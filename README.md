# EthernetIPSharp

EthernetIPSharp is a C# library for building EtherNet/IP devices and clients with .NET 8. This fork puts a particular focus on Logix tag servers: define tags in a `TagDatabase`, expose Logix tag services through `LogixDispatcher`, and host them over EtherNet/IP with `EipAdapter`. You can also build standard I/O adapters and scanners, connect to Logix controllers, and explore CIP Safety examples.

The repository includes runnable samples so you can try a local connection before connecting to PLC hardware.

## What you can build

- **A Logix tag server:** define a tag database, serve tags through `LogixDispatcher` and `EipAdapter`, and react to changes in tag values.
- **A Logix tag client:** read and write tags on a Logix controller or a server built with this library.
- **An adapter (target):** expose input, output, and configuration assemblies to a PLC or scanner.
- **A scanner (originator):** open a cyclic I/O connection and exchange data with an adapter.
- **CIP Safety examples:** explore safety adapter and scanner connections. These examples are for development and testing; the library is not a substitute for safety engineering, validation, or certification.

The library is divided into projects, so you can use the pieces that fit your application. For example, the CIP project contains protocol types, while the Device project combines an adapter with I/O assemblies.

## Requirements

- .NET 8 SDK
- For a local demo: two available terminals and the network ports used by the samples
- For hardware testing: an EtherNet/IP-compatible PLC or device and a network configuration that can reach it

EtherNet/IP commonly uses TCP port `44818` for session and explicit messaging, and UDP port `2222` for cyclic I/O. Make sure these ports are available and allowed by your firewall when running the samples.

## Get started

### 1. Build the repository

From a terminal, clone the repository and build the solution:

```sh
git clone https://github.com/SRCthird/EthernetIpSharp.git
cd EthernetIpSharp
dotnet build EthernetIPSharp.slnx
```

### 2. Start the Logix tag server

Run the dedicated Logix server sample:

```sh
dotnet run --project samples/LogixHost
```

`LogixHost` starts an `EipAdapter` on TCP port `44818` and serves tags through a `LogixDispatcher`. It includes controller-scoped atomic tags, a Logix `STRING` template, nested UDT examples, and program-scoped tags under `MAINPROGRAM`. Keep this terminal running while you connect a client. The sample binds to `0.0.0.0`; use a firewall and a trusted network when exposing it beyond your own computer.

The full setup is in [`samples/LogixHost/Program.cs`](samples/LogixHost/Program.cs). The sample project targets .NET 8 and references the Logix, Protocol, and CIP library projects directly.

### 3. Read and write a tag

In a second application or terminal, use `TagClient` to connect to the local host. The unqualified `DINT` tag below is a controller-scoped scalar created by `LogixHost`.

```csharp
using EthernetIPSharp.Logix;

await using var client = new TagClient("127.0.0.1");
await client.ConnectAsync();

int value = await client.ReadAsync<int>("DINT");
await client.WriteAsync("DINT", value + 1);
```

A successful client write updates the tag held by the server. The tag's change events are a convenient place to connect that write to application behavior.

### 4. Try standard I/O samples

For a separate adapter/scanner demo, run these commands in two terminals. The samples use assembly I/O rather than the Logix tag-server path.

```sh
# Terminal 1
dotnet run --project samples/StandardAdapterSample

# Terminal 2
dotnet run --project samples/SampleStandardScanner -- --target=127.0.0.1
```

Press `Ctrl+C` to stop a sample. If a port is already in use or a firewall blocks traffic, the connection may not start. For hardware tests, use the target device's reachable IP address and matching assembly settings.

## Pick a sample

There are seven runnable sample applications in `samples/`:

| Sample | What it demonstrates |
|---|---|
| [`LogixHost`](samples/LogixHost/) | A Logix tag server built with `LogixDispatcher` and `EipAdapter`; includes atomic tags, UDTs, a Logix `STRING` type, and controller- and program-scoped tags |
| [`SampleTag`](samples/SampleTag/) | A `TagClient` example that reads and writes a structured Logix tag |
| [`StandardAdapterSample`](samples/StandardAdapterSample/) | A standard, non-safety adapter for Generic Ethernet Module-style I/O |
| [`SampleStandardScanner`](samples/SampleStandardScanner/) | A scanner that opens a cyclic Class 1 I/O connection |
| [`CipEchoServer`](samples/CipEchoServer/) | A catch-all CIP server for inspecting and replying to requests |
| [`SafetyAdapterSample`](samples/SafetyAdapterSample/) | A CIP Safety adapter example |
| [`SampleSafetyScanner`](samples/SampleSafetyScanner/) | A CIP Safety scanner example |

Run a sample with `dotnet run --project samples/<sample-name>`. For example:

```sh
dotnet run --project samples/LogixHost
```

The [samples guide](samples/README.md) covers the adapter, scanner, and safety examples. `LogixHost` is the server-side Logix example; its complete source is [`samples/LogixHost/Program.cs`](samples/LogixHost/Program.cs).

`SampleTag` is a client example for a structured tag. Its default tag name and member layout do not match `LogixHost`'s `MOTOR` UDT, so change the target tag and structure-writing code before pairing those two samples. For a quick local read/write against `LogixHost`, use the simple `TagClient` example in the getting-started section.

## Common concepts

### Adapter and scanner

An **adapter**, also called a target, provides assemblies and waits for a scanner to connect. A **scanner**, also called an originator, opens the connection and exchanges I/O data. The standard adapter and scanner samples are a good place to start because they can be run together locally.

For a Studio 5000 **Generic Ethernet Module** configuration matching the standard adapter sample, use Data - DINT format with input assembly `100` (125 DINTs), output assembly `102` (124 DINTs), and configuration assembly `105` (10 bytes). Keep the assembly instances and sizes consistent on both sides.

### Logix tag server: `LogixHost`, database, and change handlers

[`samples/LogixHost`](samples/LogixHost/) is the project's runnable LogixController server which emulates a ControlLogix 5500, not the assembly-based `StandardAdapterSample`. Start it with:

```sh
dotnet run --project samples/LogixHost
```

It builds a `LogixDispatcher`, populates its tag model, then passes that dispatcher to an `EipAdapter` listening on TCP port `44818`. This server path is for Logix explicit tag services; it does not require the UDP Class 1 I/O transport used by the standard adapter/scanner samples.

#### How `LogixHost` defines its tags

The sample creates the controller identity and a dispatcher backed by a `TagDatabase`:

```csharp
using System.Net;
using EthernetIPSharp.Cip;
using EthernetIPSharp.Logix;
using EthernetIPSharp.Protocol;

var identity = new IdentityInfo
{
    VendorId = 1,
    DeviceType = 0x0E, // Programmable Logic Controller
    ProductCode = 55,
    MajorRevision = 32,
    MinorRevision = 11,
    SerialNumber = 0xDEAD,
    ProductName = "EthernetIPSharp Logix",
};

var logix = new LogixDispatcher(new TagDatabase(), identity);
```

Tags and templates are added to `logix.Tags` for controller scope. `LogixHost` defines the Logix `STRING` layout, then uses templates to model nested structures and arrays:

```csharp
var stringTemplate = logix.Tags.AddStringType("STRING;82", 82);

var heaterTemplate = logix.Tags.AddTemplate(
    "Heater",
    new TemplateMember("Heating", LogixDataTypes.BOOL),
    new TemplateMember("Setpoint", LogixDataTypes.REAL),
    new TemplateMember("Temps", LogixDataTypes.REAL, arraySize: 3),
    new TemplateMember("Avg", LogixDataTypes.REAL),
    new TemplateMember("Codes", stringTemplate, arraySize: 3));

var motorStatusTemplate = logix.Tags.AddTemplate(
    "MotorStatus",
    new TemplateMember("Running", LogixDataTypes.BOOL),
    new TemplateMember("Faulted", LogixDataTypes.BOOL),
    new TemplateMember("Speed", LogixDataTypes.REAL),
    new TemplateMember("CycleCount", LogixDataTypes.DINT),
    new TemplateMember("heaters", heaterTemplate, arraySize: 4),
    new TemplateMember("Asset", stringTemplate));

var mainProgram = logix.AddProgram("MAINPROGRAM");
mainProgram.Tags.AddTag("DINT", LogixDataTypes.DINT, elementCount: 4);
mainProgram.Tags.AddTag("MOTOR", motorStatusTemplate);

// Controller-scoped tags use logix.Tags.
logix.Tags.AddTag("DINT", LogixDataTypes.DINT);
logix.Tags.AddTag("STRING", stringTemplate);
```

The complete sample also registers scalar tags for the other supported atomic types and hosts them with `EipAdapter`:

```csharp
var adapter = new EipAdapter(logix, identity);
await adapter.ListenAsync(IPAddress.Parse("0.0.0.0"), 44818);
```

`0.0.0.0` binds to all local IPv4 interfaces. For local-only testing, bind to `IPAddress.Loopback`; for a networked test, use the host's assigned address and restrict access with your firewall. See [`Program.cs`](samples/LogixHost/Program.cs) for the full runnable setup.

#### Tag Database storage

`TagDatabase` is the default in-memory implementation of `ITagDatabase`. It stores tags by name and Symbol Object instance ID, and stores the UDT templates needed to describe structured tags. Use `AddTag` for atomic values and arrays, `AddTemplate` for UDTs, and `AddStringType` for a Logix `STRING`-family structure. In this model, a Logix `STRING` is a template with a `LEN` DINT and a `DATA` SINT array, not an atomic `LogixDataTypes` value. The default database does not persist values after the process exits; `LogixDispatcher` can accept another `ITagDatabase` implementation when an application needs different storage.

There are two tag scopes in the host:

- **Controller scope:** `logix.Tags.AddTag(...)`
- **Program scope:** `mainProgram.Tags.AddTag(...)`, where `mainProgram` comes from `logix.AddProgram("MAINPROGRAM")`

Program-scoped symbols use the `PROGRAM:MAINPROGRAM.TagName` form. Program tags live in their own database, so subscribe to `mainProgram.Tags` to observe their changes; `logix.Tags` events cover controller-scoped tags.

#### Make application logic react to writes

`LogixHost` demonstrates serving tags and UDTs. To attach behavior, subscribe to `Tag.ValueChanged` for one tag, or to `AnyTagChanged` on the database for all its tags. Both application writes and incoming CIP tag writes use the tag's write methods and raise these notifications.

```csharp
var rate = logix.Tags.AddTag("Rate", LogixDataTypes.DINT);
var highRate = logix.Tags.AddTag("HighRate", LogixDataTypes.BOOL);

rate.Write(0, 0);
highRate.Write(0, false);

rate.ValueChanged += (tag, change) =>
{
    int currentRate = tag.Read<int>();
    highRate.Write(0, currentRate > 1000);

    Console.WriteLine(
        $"{tag.Name} = {currentRate}; changed {change.ByteLength} byte(s) " +
        $"at offset {change.ByteOffset}.");
};
```

`TagChangeInfo` reports the byte offset and length written. Treat these events as write notifications: a write can raise an event even if it writes the same value. Callbacks may run on a TCP client-handling thread, so keep them short and move blocking or long-running work to an application-owned queue or background task.

To watch every controller-scoped tag instead, subscribe to `logix.Tags.AnyTagChanged`. For program-scoped tags, subscribe to `mainProgram.Tags.AnyTagChanged`.

#### Test the server with a client

`TagClient` can connect to `LogixHost` or to a Logix controller. `LogixHost` creates a controller-scoped `DINT`, so it can be read and written with a short client program:

```csharp
using EthernetIPSharp.Logix;

await using var client = new TagClient("127.0.0.1");
await client.ConnectAsync();

int value = await client.ReadAsync<int>("DINT");
await client.WriteAsync("DINT", value + 1);
```

#### Avoiding circular reasoning

`TagClient` is written in the same scope as the `LogixHost` which means they are perfect for eachother. To avoid circular reasoning when developing your own controller I would suggest using external software like **PTC Kepware**, **pycomm3**, or **Node-RED**.

### CIP Safety

The safety samples show how to configure a safety adapter or open a safety connection to a device. Safety connections require matching device identity and safety configuration values. Start with the sample comments and the [samples guide](samples/README.md); the required values depend on the target device and its configuration.

## Repository contents

- `src/` — library projects for CIP, EtherNet/IP transport, connections, devices, safety, and Logix tags
- `samples/` — runnable examples for adapters, scanners, tag access, and CIP Safety
- `tests/` — automated tests for protocol handling, connections, tag services, and safety-related encoding
- `packaging/` — NuGet packaging project

## Build and test

Build the full solution:

```sh
dotnet build EthernetIPSharp.slnx
```

Run the test suite:

```sh
dotnet test EthernetIPSharp.slnx
```

To run tests for one project, provide its path, for example:

```sh
dotnet test tests/EthernetIPSharp.Safety.Tests
```

## Current limitations

- Assembly data and tag values are held in memory; they are not saved between runs.
- Some CIP Safety reset and configuration-apply services have not been extensively tested with hardware.
- Very short I/O intervals depend on the host operating system, network adapter, and system load. Test timing on the hardware and network you plan to use.
- Safety ownership and commissioning workflows are not fully implemented. Review the current code and tests before relying on those behaviors.

## License

EthernetIPSharp is licensed under the [Apache License 2.0](LICENSE).

