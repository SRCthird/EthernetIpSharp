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

// Add templates
var stringTemplate = logix.Tags.AddStringType("STRING;82", 82);
var motorStatusTemplate = logix.Tags.AddTemplate(
    "MotorStatus",
    new TemplateMember("Running", LogixDataTypes.BOOL),
    new TemplateMember("Faulted", LogixDataTypes.BOOL),
    new TemplateMember("Speed", LogixDataTypes.REAL),
    new TemplateMember("CycleCount", LogixDataTypes.DINT),
    new TemplateMember("Temps", LogixDataTypes.REAL, arraySize: 4)
);

// Add program
var mainProgram = logix.AddProgram("MAINPROGRAM");
mainProgram.Tags.AddTag("DINT", LogixDataTypes.DINT, elementCount: 4);
mainProgram.Tags.AddTag("MOTOR", motorStatusTemplate);

// Add test tags
logix.Tags.AddTag("BOOL", LogixDataTypes.BOOL);
logix.Tags.AddTag("SINT", LogixDataTypes.SINT);
logix.Tags.AddTag("INT", LogixDataTypes.INT);
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

var adapter = new EipAdapter(logix, identity);
await adapter.ListenAsync(IPAddress.Parse("0.0.0.0"), 44818);

Console.WriteLine("Logix simulator running on 0.0.0.0:44818");
Console.WriteLine("Press Ctrl+C to stop.");

await Task.Delay(Timeout.Infinite, new CancellationTokenSource().Token);
