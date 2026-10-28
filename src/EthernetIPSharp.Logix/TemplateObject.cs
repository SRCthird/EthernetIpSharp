using System.Buffers.Binary;
using System.Text;
using SRCthird.EthernetIPSharp.Cip;

namespace SRCthird.EthernetIPSharp.Logix;

/// <summary>
/// CIP Template Object (Class 0x6C).
/// Each UDT/structure type has one instance. Provides:
/// - Get_Attribute_List (0x03): returns struct handle, member count, definition size, struct size
/// - Template Read (0x4C): returns member information (type, offset, names)
/// </summary>
public sealed class TemplateObject
{
    /// <summary>CIP class code for Template Object.</summary>
    public const uint ClassCode = 0x6C;

    /// <summary>Member type flag: the member is an array (Info = element count).</summary>
    private const ushort ArrayMemberFlag = 0x2000;

    /// <summary>The CIP class object for registration in the dispatcher.</summary>
    public CipClass CipClass { get; }

    /// <summary>
    /// Create the Template Object CIP class with Get_Attribute_List (0x03)
    /// and Template Read (0x4C) instance services.
    /// </summary>
    public TemplateObject(ITagDatabase tags)
    {
        CipClass = new CipClass(ClassCode, "Template", revision: 1);

        // Instance-level services
        CipClass.AddInstanceService(new CipServiceDefinition(
            0x4C, "Template_Read", HandleTemplateRead));
        CipClass.AddStandardInstanceServices();
    }

    /// <summary>Ensure a CIP instance exists for a template definition.</summary>
    public void EnsureInstance(TemplateDefinition template)
    {
        if (CipClass.GetInstance(template.InstanceId) != null) return;
        foreach (var member in template.Members)
        {
            if (member.NestedTemplate != null)
                EnsureInstance(member.NestedTemplate);
        }

        var inst = CipClass.CreateInstance(template.InstanceId);
        inst.UserData = template;

        const AttributeAccess get = AttributeAccess.GetSingle | AttributeAccess.GetAll;

        // Attribute 1: Structure Handle (UINT)
        inst.AddAttribute(CipAttribute.Create(1, CipDataType.Uint, get, template.StructureHandle));
        // Attribute 2: Member Count (UINT)
        inst.AddAttribute(CipAttribute.Create(2, CipDataType.Uint, get, (ushort)template.MemberCount));
        // Attribute 3: Structure Size (UINT) — controllers repeat attribute 5 here
        inst.AddAttribute(CipAttribute.Create(3, CipDataType.Uint, get, (ushort)template.StructureSize));
        // Attribute 4: Template Object Definition Size (UDINT) — 32-bit words
        inst.AddAttribute(CipAttribute.Create(4, CipDataType.Udint, get, template.DefinitionSize));
        // Attribute 5: Template Structure Size (UDINT) — bytes on wire
        inst.AddAttribute(CipAttribute.Create(5, CipDataType.Udint, get, template.StructureSize));
        // Attribute 6: string flag (UINT) — 1 for STRING-family types, 0 otherwise.
        // Clients use it to present DATA as a single string (STRING.DATA/82).
        inst.AddAttribute(CipAttribute.Create(6, CipDataType.Uint, get,
            template.IsString ? (ushort)1 : (ushort)0));
    }

    /// <summary>
    /// Template Read (0x4C on Template Object).
    /// Request: byte_offset (UDINT) + bytes_to_read (UINT)
    /// Response: template definition data (member info + names)
    ///
    /// The template definition structure contains:
    /// - For each member: type_info (UINT) + info (UINT) + offset (UDINT)
    ///   where type_info upper 16 bits = data type, lower 16 bits = array size or bit position
    /// - After all members: null-terminated template name + null-terminated member names
    /// </summary>
    private CipServiceResponse HandleTemplateRead(CipInstance instance, CipServiceRequest request)
    {
        var template = instance.UserData as TemplateDefinition;
        if (template == null)
            return CipServiceResponse.Error(request.ServiceCode, CipStatus.Error(0x05));

        if (request.Data.Length < 6)
            return CipServiceResponse.Error(request.ServiceCode, CipStatus.Error(0x13));

        var span = request.Data.Span;
        uint byteOffset = BinaryPrimitives.ReadUInt32LittleEndian(span);
        ushort bytesToRead = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(4));

        // Build the full template definition data
        var fullData = BuildTemplateDefinitionData(template);

        if (byteOffset >= (uint)fullData.Length)
            return CipServiceResponse.Error(request.ServiceCode, CipStatus.Error(0xFF, 0x2105));

        int available = fullData.Length - (int)byteOffset;
        int chunkSize = Math.Min(available, bytesToRead);
        chunkSize = Math.Min(chunkSize, 480); // Max reply size

        var responseData = fullData.AsMemory((int)byteOffset, chunkSize);
        bool moreData = (int)byteOffset + chunkSize < fullData.Length;

        if (moreData)
        {
            return new CipServiceResponse
            {
                ServiceCode = (byte)(request.ServiceCode | 0x80),
                Status = CipStatus.Error(0x06),
                Data = responseData,
            };
        }

        return CipServiceResponse.Success(request.ServiceCode, responseData);
    }

    /// <summary>
    /// Build the template definition byte array.
    /// Format per member: [type_and_info (4 bytes)] [offset (4 bytes)]
    /// Then: template_name\0 + member1_name\0 + member2_name\0 + ...
    /// </summary>
    private static byte[] BuildTemplateDefinitionData(TemplateDefinition template)
    {
        var data = new byte[TemplateDefinition.DefinitionDataLength(template.Name, template.Members)];
        int offset = 0;

        // Member info entries
        foreach (var member in template.Members)
        {
            bool isArray = member.DataType != LogixDataTypes.BOOL && member.ArraySize > 0;
            ushort type = isArray ? (ushort)(member.DataType | ArrayMemberFlag) : member.DataType;

            BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(offset), (ushort)member.ArraySize);
            BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(offset + 2), type);
            BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(offset + 4), (uint)member.Offset);
            offset += 8;
        }

        offset += Encoding.ASCII.GetBytes(template.Name, data.AsSpan(offset));
        data[offset++] = 0;

        // Member names (null-terminated)
        foreach (var member in template.Members)
        {
            offset += Encoding.ASCII.GetBytes(member.Name, data.AsSpan(offset));
            data[offset++] = 0;
        }

        return data;
    }
}
