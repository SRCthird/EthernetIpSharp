using SRCthird.EthernetIPSharp.Cip;
using System.Buffers.Binary;
using System.Text;


namespace SRCthird.EthernetIPSharp.Logix;

/// <summary>
/// Rockwell Program Object (class 0x68).
///
/// The ControlLogix Ethernet driver enumerates programs with service 0x4B on
/// the class (instance 0) during a project upload.
/// </summary> 
public sealed class ProgramObject
{
    public const uint ClassCode = 0x68;
    public const byte EnumeratePrograms = 0x4B;

    private readonly Dictionary<string, ProgramDefinition> _programs =
        new(StringComparer.OrdinalIgnoreCase);
    private uint _nextProgramNumber;

    private const uint ProgramInstanceBase = 0x70000000;

    public CipClass CipClass { get; }

    public IEnumerable<ProgramDefinition> AllPrograms => _programs.Values;

    public event Action<ProgramDefinition>? ProgramAdded;

    public ProgramObject()
    {
        CipClass = new CipClass(ClassCode, "Program", revision: 1);
        CipClass.AddStandardInstanceServices();
        CipClass.AddClassService(new CipServiceDefinition(
            EnumeratePrograms,
            "Enumerate_Programs",
            HandleEnumeratePrograms));
        CipClass.AddInstanceService(new CipServiceDefinition(
            EnumeratePrograms,
            "Enumerate_Programs",
            HandleEnumeratePrograms));
    }

    /// <summary>Create a named program. Names are case-insensitive and unique.</summary>
    public ProgramDefinition AddProgram(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.IndexOfAny([ ':', '.' ]) >= 0)
            throw new ArgumentException("Program names cannot contain ':' or '.'.", nameof(name));
        if (_programs.ContainsKey(name))
            throw new InvalidOperationException($"Program '{name}' already exists.");

        var programNumber = checked(++_nextProgramNumber);
        var program = new ProgramDefinition(
            instanceId: ProgramInstanceBase + programNumber,
            name: name,
            firstTagInstanceId: checked(programNumber * 0x10000u),
            firstTemplateId: checked(0x100 + (int)programNumber * 0x100));

        _programs.Add(name, program);

        var instance = CipClass.CreateInstance(program.InstanceId);
        instance.UserData = program;
        instance.AddAttribute(new CipAttribute(
            1,
            CipDataType.String,
            AttributeAccess.GetSingle | AttributeAccess.GetAll,
            EncodeString(name)));

        ProgramAdded?.Invoke(program);
        return program;
    }

    public ProgramDefinition? FindByName(string name) =>
        _programs.TryGetValue(name, out var program) ? program : null;

    private static byte[] EncodeString(string value)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        var data = new byte[2 + bytes.Length];
        BinaryPrimitives.WriteUInt16LittleEndian(data, checked((ushort)bytes.Length));
        bytes.CopyTo(data, 2);
        return data;
    }

    private CipServiceResponse HandleEnumeratePrograms(
        CipInstance instance,
        CipServiceRequest request)
    {
        uint startInstance = request.Path.InstanceId ?? 0;
        var programs = _programs.Values
            .Where(p => p.InstanceId > startInstance)
            .OrderBy(p => p.InstanceId);

        var response = new List<byte>();
        foreach (var program in programs)
        {
            var id = new byte[4];
            BinaryPrimitives.WriteUInt32LittleEndian(id, program.InstanceId);
            response.AddRange(id);
            response.AddRange(EncodeString(program.Name));
        }

        return CipServiceResponse.Success(request.ServiceCode, response.ToArray());
    }
}

public sealed class ProgramDefinition
{
    internal ProgramDefinition(
        uint instanceId,
        string name,
        uint firstTagInstanceId,
        int firstTemplateId)
    {
        InstanceId = instanceId;
        Name = name;
        Tags = new TagDatabase(firstTagInstanceId, firstTemplateId);
    }

    /// <summary>Program Object instance ID.</summary>
    public uint InstanceId { get; }

    /// <summary>Program name used in a Logix symbolic path.</summary>
    public string Name { get; }

    /// <summary>Tags that belong to this program.</summary>
    public TagDatabase Tags { get; }
}
