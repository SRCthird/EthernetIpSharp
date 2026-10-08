using System.Buffers.Binary;
using EthernetIPSharp.Cip;

namespace EthernetIPSharp.Logix;

/// <summary>
/// Rockwell Controller Object (class 0xAC).
///
/// The ControlLogix Ethernet driver reads attributes 1 and 3 before and after a
/// project upload and compares the values to decide whether the controller
/// project shifted mid-upload. The values are opaque to the driver — only their
/// width and their stability matter. Both are therefore projections of
/// ITagDatabase.StructureVersion, which changes only when the tag structure does.
/// </summary>
public sealed class ControllerObject
{
    public const uint ClassCode = 0xAC;

    private readonly ITagDatabase _tags;
    private readonly CipAttribute _changeNumber;
    private readonly CipAttribute _changeSignature;

    public CipClass CipClass { get; }

    public ControllerObject(ITagDatabase tags)
    {
        _tags = tags;

        CipClass = new CipClass(ClassCode, "Controller", revision: 1);
        CipClass.AddStandardInstanceServices();

        var inst = CipClass.CreateInstance(1);

        // Attribute 1: project change number (UDINT).
        _changeNumber = CipAttribute.Create(1, CipDataType.Udint,
            AttributeAccess.GetSingle | AttributeAccess.GetAll, 0u);
        inst.AddAttribute(_changeNumber);

        // Attribute 3: project change signature (UDINT).
        _changeSignature = CipAttribute.Create(3, CipDataType.Udint,
            AttributeAccess.GetSingle | AttributeAccess.GetAll, 0u);
        inst.AddAttribute(_changeSignature);

        Refresh();
        tags.TagAdded += _ => Refresh();
        tags.TemplateAdded += _ => Refresh();
    }

    private void Refresh()
    {
        uint version = _tags.StructureVersion;

        Span<byte> buf = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(buf, version);
        _changeNumber.SetData(buf);

        BinaryPrimitives.WriteUInt32LittleEndian(buf, version ^ 0xA5A5_0000u);
        _changeSignature.SetData(buf);
    }
}
