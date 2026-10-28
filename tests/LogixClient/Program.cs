using System.Buffers.Binary;
using SRCthird.EthernetIPSharp.Logix;

const string Host = "127.0.0.1";
const int Port = 44818;

const string ProgramDint = "Program:MAINPROGRAM.DINT";
const string ProgramMotor = "Program:MAINPROGRAM.MOTOR";

await using var client = new TagClient(Host, Port);

Console.WriteLine($"Connecting to {Host}:{Port}...");
await client.ConnectAsync();

Console.WriteLine("Connected.");
Console.WriteLine("Browsing tags...");

var browse = await client.BrowseTagsAsync();

foreach (var tag in browse.UserTags.OrderBy(t => t.Name))
{
    Console.WriteLine($"  {tag}");
}

Console.WriteLine();

var expectedControllerTags = new[]
{
    "BOOL",
    "SINT",
    "INT",
    "DINT",
    "LINT",
    "USINT",
    "UINT",
    "UDINT",
    "ULINT",
    "REAL",
    "LREAL",
    "DWORD",
    "STRING",
};

foreach (var tagName in expectedControllerTags)
{
    RequireTag(browse, tagName);
}

RequireTag(browse, ProgramDint);
RequireTag(browse, ProgramMotor);

Console.WriteLine("Testing controller-scope atomic tags...");
Console.WriteLine();

await TestAtomicAsync(client, "BOOL", false, true);
await TestAtomicAsync(client, "SINT", (sbyte)-11, (sbyte)37);
await TestAtomicAsync(client, "INT", (short)-2222, (short)12345);
await TestAtomicAsync(client, "DINT", -333333, 1234567);
await TestAtomicAsync(client, "LINT", -4444444444L, 9876543210L);

await TestAtomicAsync(client, "USINT", (byte)11, (byte)201);
await TestAtomicAsync(client, "UINT", (ushort)2222, (ushort)54321);
await TestAtomicAsync(client, "UDINT", 333333U, 3456789012U);
await TestAtomicAsync(client, "ULINT", 4444444444UL, 123456789012345UL);

await TestAtomicAsync(client, "REAL", 12.5f, 98.25f);
await TestAtomicAsync(client, "LREAL", 25.25d, 1234.5678d);

await TestDwordAsync(client, "DWORD", 0xA5A55A5A);

Console.WriteLine();
Console.WriteLine("Testing STRING tag...");
Console.WriteLine();

await TestStringAsync(
    client,
    browse,
    "STRING",
    "LOCAL-STRING");

Console.WriteLine();
Console.WriteLine("Testing program-scope DINT array...");
Console.WriteLine();

await TestDintArrayAsync(
    client,
    ProgramDint,
    new[] { 10, 20, 30, 40 });

Console.WriteLine();
Console.WriteLine("Testing MOTOR structure...");
Console.WriteLine();

await TestMotorStructureAsync(
    client,
    browse,
    ProgramMotor);

Console.WriteLine();
Console.WriteLine("All tag read/write tests passed.");

static TagInfo RequireTag(TagBrowseResult browse, string name)
{
    var tag = browse.Tags.FirstOrDefault(t =>
        t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    if (tag is null)
    {
        throw new InvalidOperationException(
            $"Expected tag '{name}' was not returned by BrowseTagsAsync().");
    }

    return tag;
}

static async Task TestAtomicAsync<T>(
    TagClient client,
    string tagName,
    T initialTestValue,
    T finalTestValue)
    where T : unmanaged
{
    var before = await client.ReadAsync<T>(tagName);

    await client.WriteAsync(tagName, initialTestValue);
    var afterFirstWrite = await client.ReadAsync<T>(tagName);

    if (!EqualityComparer<T>.Default.Equals(afterFirstWrite, initialTestValue))
    {
        throw new InvalidOperationException(
            $"{tagName}: first write failed. " +
            $"Expected {initialTestValue}, received {afterFirstWrite}.");
    }

    await client.WriteAsync(tagName, finalTestValue);
    var afterSecondWrite = await client.ReadAsync<T>(tagName);

    if (!EqualityComparer<T>.Default.Equals(afterSecondWrite, finalTestValue))
    {
        throw new InvalidOperationException(
            $"{tagName}: second write failed. " +
            $"Expected {finalTestValue}, received {afterSecondWrite}.");
    }

    Console.WriteLine(
        $"PASS {tagName,-6} " +
        $"before={before}, after={afterSecondWrite}");
}

static async Task TestDwordAsync(
    TagClient client,
    string tagName,
    uint expectedValue)
{
    var before = await ReadDwordAsync(client, tagName);

    var payload = new byte[sizeof(uint)];
    BinaryPrimitives.WriteUInt32LittleEndian(payload, expectedValue);

    await client.WriteRawAsync(
        tagName,
        LogixDataTypes.DWORD,
        elementCount: 1,
        payload);

    var after = await ReadDwordAsync(client, tagName);

    if (after != expectedValue)
    {
        throw new InvalidOperationException(
            $"{tagName}: expected 0x{expectedValue:X8}, " +
            $"received 0x{after:X8}.");
    }

    Console.WriteLine(
        $"PASS {tagName,-6} " +
        $"before=0x{before:X8}, after=0x{after:X8}");
}

static async Task<uint> ReadDwordAsync(
    TagClient client,
    string tagName)
{
    var raw = await client.ReadTagRawAsync(tagName, elementCount: 1);

    if (raw.Length < 2 + sizeof(uint))
    {
        throw new InvalidOperationException(
            $"{tagName}: response was too short for a DWORD.");
    }

    var tagType = BinaryPrimitives.ReadUInt16LittleEndian(raw);
    if (tagType != LogixDataTypes.DWORD)
    {
        throw new InvalidOperationException(
            $"{tagName}: expected DWORD type 0x{LogixDataTypes.DWORD:X4}, " +
            $"received 0x{tagType:X4}.");
    }

    return BinaryPrimitives.ReadUInt32LittleEndian(
        raw.AsSpan(2, sizeof(uint)));
}

static async Task TestStringAsync(
    TagClient client,
    TagBrowseResult browse,
    string tagName,
    string expectedValue)
{
    var tag = RequireTag(browse, tagName);

    if (!tag.IsStruct || tag.Template is null)
    {
        throw new InvalidOperationException(
            $"{tagName}: expected a resolved STRING structure template.");
    }

    var before = await client.ReadStringAsync(tagName);

    await client.WriteStringAsync(
        tagName,
        expectedValue,
        tag.Template.StructureHandle);

    var after = await client.ReadStringAsync(tagName);

    if (!after.Equals(expectedValue, StringComparison.Ordinal))
    {
        throw new InvalidOperationException(
            $"{tagName}: expected '{expectedValue}', received '{after}'.");
    }

    Console.WriteLine(
        $"PASS {tagName,-6} " +
        $"before='{before}', after='{after}'");
}

static async Task TestDintArrayAsync(
    TagClient client,
    string tagName,
    int[] expectedValues)
{
    var beforeRaw = await client.ReadTagRawAsync(
        tagName,
        elementCount: checked((ushort)expectedValues.Length));

    var payload = new byte[expectedValues.Length * sizeof(int)];

    for (int i = 0; i < expectedValues.Length; i++)
    {
        BinaryPrimitives.WriteInt32LittleEndian(
            payload.AsSpan(i * sizeof(int), sizeof(int)),
            expectedValues[i]);
    }

    await client.WriteRawAsync(
        tagName,
        LogixDataTypes.DINT,
        checked((ushort)expectedValues.Length),
        payload);

    var afterRaw = await client.ReadTagRawAsync(
        tagName,
        elementCount: checked((ushort)expectedValues.Length));

    VerifyRawPayload(
        tagName,
        afterRaw,
        LogixDataTypes.DINT,
        payload);

    Console.WriteLine(
        $"PASS {tagName,-32} " +
        $"elements=[{string.Join(", ", expectedValues)}] " +
        $"beforeBytes={beforeRaw.Length - 2}");
}

static async Task TestMotorStructureAsync(
    TagClient client,
    TagBrowseResult browse,
    string tagName)
{
    var tag = RequireTag(browse, tagName);

    var template = tag.Template
        ?? throw new InvalidOperationException(
            $"{tagName}: no structure template was resolved.");

    var before = await client.ReadStructAsync(tagName, template);

    var writer = new StructureValue(
        template,
        before.RawData.ToArray());

    writer.SetBool("Running", !before.GetBool("Running"));
    writer.SetBool("Faulted", !before.GetBool("Faulted"));
    writer.Set("Speed", 1450.5f);
    writer.Set("CycleCount", 7);
    writer.SetString("Asset", "MOTOR-LOCAL");

    await client.WriteStructValueAsync(tagName, writer);

    var after = await client.ReadStructAsync(tagName, template);

    if (after.GetBool("Running") != !before.GetBool("Running"))
    {
        throw new InvalidOperationException(
            $"{tagName}.Running: write/read-back failed.");
    }

    if (after.GetBool("Faulted") != !before.GetBool("Faulted"))
    {
        throw new InvalidOperationException(
            $"{tagName}.Faulted: write/read-back failed.");
    }

    if (after.Get<float>("Speed") != 1450.5f)
    {
        throw new InvalidOperationException(
            $"{tagName}.Speed: write/read-back failed.");
    }

    if (after.Get<int>("CycleCount") != 7)
    {
        throw new InvalidOperationException(
            $"{tagName}.CycleCount: write/read-back failed.");
    }

    if (!after.GetString("Asset").Equals(
            "MOTOR-LOCAL",
            StringComparison.Ordinal))
    {
        throw new InvalidOperationException(
            $"{tagName}.Asset: write/read-back failed.");
    }

    Console.WriteLine(
        $"PASS {tagName,-32} " +
        $"Running={after.GetBool("Running")}, " +
        $"Faulted={after.GetBool("Faulted")}, " +
        $"Speed={after.Get<float>("Speed")}, " +
        $"CycleCount={after.Get<int>("CycleCount")}, " +
        $"Asset='{after.GetString("Asset")}'");

    Console.WriteLine(
        $"     Nested members covered by the structure write: " +
        $"heaters[4], Temps[3], Codes[3], and nested STRING values.");
}

static void VerifyRawPayload(
    string tagName,
    byte[] raw,
    ushort expectedTagType,
    byte[] expectedPayload)
{
    if (raw.Length < 2 + expectedPayload.Length)
    {
        throw new InvalidOperationException(
            $"{tagName}: response was shorter than expected.");
    }

    var actualTagType =
        BinaryPrimitives.ReadUInt16LittleEndian(raw);

    if (actualTagType != expectedTagType)
    {
        throw new InvalidOperationException(
            $"{tagName}: expected type 0x{expectedTagType:X4}, " +
            $"received 0x{actualTagType:X4}.");
    }

    if (!raw.AsSpan(2, expectedPayload.Length)
            .SequenceEqual(expectedPayload))
    {
        throw new InvalidOperationException(
            $"{tagName}: payload did not match the written value.");
    }
}
