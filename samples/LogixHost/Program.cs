using System.Net;
using System.Text;

using SRCthird.EthernetIPSharp.Cip;
using SRCthird.EthernetIPSharp.Logix;
using SRCthird.EthernetIPSharp.Protocol;

var identity = new IdentityInfo
{
    VendorId = 1,
    DeviceType = 0x0E,
    ProductCode = 55,
    MajorRevision = 32,
    MinorRevision = 11,
    SerialNumber = 0xDEAD,
    ProductName = "EthernetIPSharp Logix",
};

var logix = new LogixDispatcher(new TagDatabase(), identity);

// Add templates
var stringTemplate = logix.Tags.AddStringType("STRING;82", 82);
var heaterTemplate = logix.Tags.AddTemplate(
    "Heater",
    new TemplateMember("Heating", LogixDataTypes.BOOL),
    new TemplateMember("Setpoint", LogixDataTypes.REAL),
    new TemplateMember("Temps", LogixDataTypes.REAL, arraySize: 3),
    new TemplateMember("Avg", LogixDataTypes.REAL),
    new TemplateMember("Codes", stringTemplate, arraySize: 3)
);

var motorStatusTemplate = logix.Tags.AddTemplate(
    "MotorStatus",
    new TemplateMember("Running", LogixDataTypes.BOOL),
    new TemplateMember("Faulted", LogixDataTypes.BOOL),
    new TemplateMember("Speed", LogixDataTypes.REAL),
    new TemplateMember("CycleCount", LogixDataTypes.DINT),
    new TemplateMember("heaters", heaterTemplate, arraySize: 4, disableWrite: true),
    new TemplateMember("Asset", stringTemplate)
);

// Add program
var mainProgram = logix.AddProgram("MAINPROGRAM");
mainProgram.Tags.AddTag("DINT", LogixDataTypes.DINT, elementCount: 4, disableWrite: true);
mainProgram.Tags.AddTag("MOTOR", motorStatusTemplate);

// Add test tags
logix.Tags.AddTag("BOOL", LogixDataTypes.BOOL, disableWrite: true);
logix.Tags.AddTag("SINT", LogixDataTypes.SINT, disableRead: true);
logix.Tags.AddTag("INT", LogixDataTypes.INT, disableRead: true, disableWrite: true);
logix.Tags.AddTag("DINT", LogixDataTypes.DINT);
logix.Tags.AddTag("LINT", LogixDataTypes.LINT);
logix.Tags.AddTag("USINT", LogixDataTypes.USINT);
logix.Tags.AddTag("UINT", LogixDataTypes.UINT);
logix.Tags.AddTag("UDINT", LogixDataTypes.UDINT);
logix.Tags.AddTag("ULINT", LogixDataTypes.ULINT);
logix.Tags.AddTag("REAL", LogixDataTypes.REAL);
logix.Tags.AddTag("LREAL", LogixDataTypes.LREAL);
logix.Tags.AddTag("DWORD", LogixDataTypes.DWORD);
logix.Tags.AddTag("STRING", stringTemplate);

logix.Tags.AnyTagChanged += Tags_AnyTagChanged;
mainProgram.Tags.AnyTagChanged += Tags_AnyTagChanged;

static object ReadValue(Tag tag, Type cipType, int byteOffset)
{
    if (cipType == typeof(bool)) return tag.Read<bool>(byteOffset);
    if (cipType == typeof(sbyte)) return tag.Read<sbyte>(byteOffset);
    if (cipType == typeof(short)) return tag.Read<short>(byteOffset);
    if (cipType == typeof(int)) return tag.Read<int>(byteOffset);
    if (cipType == typeof(long)) return tag.Read<long>(byteOffset);
    if (cipType == typeof(byte)) return tag.Read<byte>(byteOffset);
    if (cipType == typeof(ushort)) return tag.Read<ushort>(byteOffset);
    if (cipType == typeof(uint)) return tag.Read<uint>(byteOffset);
    if (cipType == typeof(ulong)) return tag.Read<ulong>(byteOffset);
    if (cipType == typeof(float)) return tag.Read<float>(byteOffset);
    if (cipType == typeof(double)) return tag.Read<double>(byteOffset);
    return tag.Read<int>(byteOffset);
}

void Tags_AnyTagChanged(Tag tag, TagChangeInfo change)
{
    string cipDataType = CipDataTypeCast.GetCipDataType(tag.TagType);
    string cipMemberDataType = change.MemberDataType is not null
        ? CipDataTypeCast.GetCipDataType((ushort)change.MemberDataType)
        : cipDataType;

    Type cipType = change.MemberDataType is not null
        ? CipDataTypeCast.GetCSharpType((ushort)change.MemberDataType)
        : CipDataTypeCast.GetCSharpType(tag.TagType);

    var data = new List<object?>();
    int elemSize = change.MemberDataType is ushort memberType
        ? CipDataTypeCast.GetFixedSize(memberType)
        : tag.ElementSize;

    if (change.FullPath.EndsWith(".DATA", StringComparison.OrdinalIgnoreCase) &&
        change.MemberDataType == (ushort)CipDataType.Sint)
    {
        var bytes = tag.GetData(change.ByteOffset, change.ByteLength);
        data.Add(Encoding.ASCII.GetString(bytes).TrimEnd('\0'));
    }
    else
    {
        int endOffset = checked(change.ByteOffset + change.ByteLength);

        for (int byteOffset = change.ByteOffset; byteOffset < endOffset; byteOffset += elemSize)
            data.Add(ReadValue(tag, cipType, byteOffset));
    }

    string location = change.FullPath;

    if (change.MemberDataType is null && tag.ElementCount > 1)
    {
        int firstIndex = change.ByteOffset / tag.ElementSize;
        int lastIndex = firstIndex + data.Count - 1;

        location += data.Count == 1
            ? $"[{firstIndex}]"
            : $"[{firstIndex}..{lastIndex}]";
    }

    string values = data.Count == 1 && data[0] is string text
    ? System.Text.Json.JsonSerializer.Serialize(text)
    : $"[{string.Join(", ", data.Select(
        value => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)
                        ?? "null"))}]";

    Console.WriteLine($"{location}({cipMemberDataType}) = {values}");
}

var adapter = new EipAdapter(logix, identity);
await adapter.ListenAsync(IPAddress.Parse("0.0.0.0"), 44818);

Console.WriteLine("Logix simulator running on 0.0.0.0:44818");
Console.WriteLine("Press Ctrl+C to stop.");

await Task.Delay(Timeout.Infinite, new CancellationTokenSource().Token);
