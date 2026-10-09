using System.Buffers;
using System.Buffers.Binary;
using EthernetIPSharp.Cip;

namespace EthernetIPSharp.Logix;

internal static class MemberTagServices
{
    private const int MaxReplyData = 480;

    public static CipServiceResponse Read(
        LogixValue value, byte serviceCode, ReadOnlyMemory<byte> data, int elementOffset)
    {
        if (data.Length < 2)
            return CipServiceResponse.Error(serviceCode, CipStatus.Error(0x13));

        ushort elementCount = BinaryPrimitives.ReadUInt16LittleEndian(data.Span);
        int byteOffset = checked(elementOffset * value.ElementSize);
        int bytesToRead = checked(elementCount * value.ElementSize);
        if (elementOffset < 0 || byteOffset + bytesToRead > value.DataSize)
            return CipServiceResponse.Error(serviceCode, CipStatus.Error(0xFF, 0x2105));

        int responseLength = 2 + bytesToRead;
        if (responseLength > MaxReplyData)
        {
            int fit = MaxReplyData - 2;
            return BuildReadResponse(value, serviceCode, byteOffset, fit, true);
        }

        return BuildReadResponse(value, serviceCode, byteOffset, bytesToRead, false);
    }

    public static CipServiceResponse Write(
        LogixValue value, byte serviceCode, ReadOnlyMemory<byte> data, int elementOffset)
    {
        if (data.Length < 4)
            return CipServiceResponse.Error(serviceCode, CipStatus.Error(0x13));

        var span = data.Span;
        ushort type = BinaryPrimitives.ReadUInt16LittleEndian(span);
        ushort elementCount = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(2));
        if (type != value.TagType)
            return CipServiceResponse.Error(serviceCode, CipStatus.Error(0xFF, 0x2107));

        int byteOffset = checked(elementOffset * value.ElementSize);
        int bytesToWrite = checked(elementCount * value.ElementSize);
        if (elementOffset < 0 || data.Length < 4 + bytesToWrite)
            return CipServiceResponse.Error(serviceCode, CipStatus.Error(0x13));
        if (byteOffset + bytesToWrite > value.DataSize)
            return CipServiceResponse.Error(serviceCode, CipStatus.Error(0xFF, 0x2105));

        value.SetData(span.Slice(4, bytesToWrite), byteOffset);
        return CipServiceResponse.Success(serviceCode);
    }

    public static CipServiceResponse ReadFragmented(
        LogixValue value, byte serviceCode, ReadOnlyMemory<byte> data, int elementOffset)
    {
        if (data.Length < 6 || elementOffset < 0)
            return CipServiceResponse.Error(serviceCode, CipStatus.Error(0x13));

        var span = data.Span;
        ushort elementCount = BinaryPrimitives.ReadUInt16LittleEndian(span);
        uint fragmentOffset = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(2));
        if (elementCount == 0)
            elementCount = 1;

        int elementBase = checked(elementOffset * value.ElementSize);
        int totalBytes = checked(elementCount * value.ElementSize);
        if (elementBase < 0 || elementBase + totalBytes > value.DataSize ||
            fragmentOffset >= (uint)totalBytes)
            return CipServiceResponse.Error(serviceCode, CipStatus.Error(0xFF, 0x2105));

        int remaining = totalBytes - (int)fragmentOffset;
        int chunk = Math.Min(remaining, MaxReplyData - 2);
        int byteOffset = elementBase + (int)fragmentOffset;
        bool more = (int)fragmentOffset + chunk < totalBytes;
        return BuildReadResponse(value, serviceCode, byteOffset, chunk, more);
    }

    public static CipServiceResponse WriteFragmented(
        LogixValue value, byte serviceCode, ReadOnlyMemory<byte> data, int elementOffset)
    {
        if (data.Length < 8 || elementOffset < 0)
            return CipServiceResponse.Error(serviceCode, CipStatus.Error(0x13));

        var span = data.Span;
        ushort type = BinaryPrimitives.ReadUInt16LittleEndian(span);
        ushort elementCount = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(2));
        uint fragmentOffset = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(4));
        if (type != value.TagType)
            return CipServiceResponse.Error(serviceCode, CipStatus.Error(0xFF, 0x2107));
        if (elementCount == 0)
            elementCount = 1;

        int elementBase = checked(elementOffset * value.ElementSize);
        int totalBytes = checked(elementCount * value.ElementSize);
        int writeLength = data.Length - 8;
        if (fragmentOffset + (uint)writeLength > (uint)totalBytes)
            return CipServiceResponse.Error(serviceCode, CipStatus.Error(0xFF, 0x2104));

        int byteOffset = elementBase + (int)fragmentOffset;
        if (byteOffset < 0 || byteOffset + writeLength > value.DataSize)
            return CipServiceResponse.Error(serviceCode, CipStatus.Error(0xFF, 0x2105));

        value.SetData(span.Slice(8, writeLength), byteOffset);
        return CipServiceResponse.Success(serviceCode);
    }

    private static CipServiceResponse BuildReadResponse(
        LogixValue value, byte serviceCode, int byteOffset, int dataLength, bool more)
    {
        int responseLength = 2 + dataLength;
        var rented = ArrayPool<byte>.Shared.Rent(responseLength);
        try
        {
            BinaryPrimitives.WriteUInt16LittleEndian(rented, value.TagType);
            value.GetData(byteOffset, dataLength).CopyTo(rented.AsSpan(2));
            var result = rented.AsSpan(0, responseLength).ToArray();
            if (more)
            {
                return new CipServiceResponse
                {
                    ServiceCode = (byte)(serviceCode | 0x80),
                    Status = CipStatus.Error(0x06),
                    Data = result,
                };
            }
            return CipServiceResponse.Success(serviceCode, result);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    public static CipServiceResponse ReadModifyWrite(
    LogixValue value, byte serviceCode, ReadOnlyMemory<byte> data)
    {
        if (data.Length < 2)
            return CipServiceResponse.Error(serviceCode, CipStatus.Error(0x13));

        var span = data.Span;
        ushort maskSize = BinaryPrimitives.ReadUInt16LittleEndian(span);
        if (maskSize != 1 && maskSize != 2 && maskSize != 4 && maskSize != 8 && maskSize != 12)
            return CipServiceResponse.Error(serviceCode, CipStatus.Error(0x03));
        if (data.Length < 2 + maskSize * 2 || maskSize > value.DataSize)
            return CipServiceResponse.Error(serviceCode, CipStatus.Error(0x13));

        var orMask = span.Slice(2, maskSize);
        var andMask = span.Slice(2 + maskSize, maskSize);
        var current = value.GetData(0, maskSize).ToArray();
        for (int i = 0; i < maskSize; i++)
            current[i] = (byte)((current[i] | orMask[i]) & andMask[i]);
        value.SetData(current);
        return CipServiceResponse.Success(serviceCode);
    }
}