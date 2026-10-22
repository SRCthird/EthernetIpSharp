// Minimal Logix simulator for pycomm3 testing — run with: dotnet script tests/logix_host.cs
// Or compile as a standalone console app

using System.Net;
using EthernetIPSharp.Cip;
using EthernetIPSharp.Logix;
using EthernetIPSharp.Protocol;

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

var stringTemplate = logix.Tags.AddTemplate(
    "STRING",
    new TemplateMember("LEN", LogixDataTypes.DINT),
    new TemplateMember("DATA", LogixDataTypes.SINT, arraySize: 82)
    );

var motorStatusTemplate = logix.Tags.AddTemplate(
    "MotorStatus",
    new TemplateMember("Running", LogixDataTypes.BOOL),
    new TemplateMember("Faulted", LogixDataTypes.BOOL),
    new TemplateMember("Speed", LogixDataTypes.REAL),
    new TemplateMember("CycleCount", LogixDataTypes.DINT),
    new TemplateMember("Temps", LogixDataTypes.REAL, arraySize: 4)
);

// Add test tags
logix.Tags.AddTag("BOOL", LogixDataTypes.BOOL);
logix.Tags.AddTag("DINT", LogixDataTypes.DINT, elementCount: 4);
logix.Tags.AddTag("INT", LogixDataTypes.INT);
logix.Tags.AddTag("LINT", LogixDataTypes.LINT);
logix.Tags.AddTag("LREAL", LogixDataTypes.LREAL);
logix.Tags.AddTag("REAL", LogixDataTypes.REAL);
logix.Tags.AddTag("SINT", LogixDataTypes.SINT);
logix.Tags.AddTag("STRING", stringTemplate);
logix.Tags.AddTag("MOTOR", motorStatusTemplate);

var adapter = new EipAdapter(logix, identity);
await adapter.ListenAsync(IPAddress.Parse("0.0.0.0"), 44818);

Console.WriteLine("Logix simulator running on 0.0.0.0:44818");
Console.WriteLine("Press Ctrl+C to stop.");

await Task.Delay(Timeout.Infinite, new CancellationTokenSource().Token);
