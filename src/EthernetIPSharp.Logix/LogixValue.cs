namespace EthernetIPSharp.Logix;

internal sealed class LogixValue
{
    private readonly Tag _root;
    private readonly int _baseOffset;
    private readonly bool _isBool;
    private readonly int _boolBit;

    public int RootElementIndex { get; }
    public int MemberOffset { get; }
    public ushort TagType { get; }
    public int ElementSize { get; }
    public int ElementCount { get; }
    public int DataSize => ElementSize * ElementCount;

    public bool IsArray => ElementCount > 1;

    public LogixValue(Tag tag)
    {
        _root = tag;
        RootElementIndex = 0;
        MemberOffset = 0;
        TagType = tag.TagType;
        ElementSize = tag.ElementSize;
        ElementCount = tag.ElementCount;
    }

    public LogixValue(Tag root, TemplateMemberInfo member, int rootElementIndex, int memberElementIndex = 0)
    {
        int memberCount = member.DataType == LogixDataTypes.BOOL ? 1 : Math.Max(1, member.ArraySize);
        if ((uint)memberElementIndex >= (uint)memberCount)
            throw new ArgumentOutOfRangeException(nameof(memberElementIndex));

        _root = root;
        if ((uint)rootElementIndex >= (uint)root.ElementCount)
            throw new ArgumentOutOfRangeException(nameof(rootElementIndex));
        RootElementIndex = rootElementIndex;
        MemberOffset = checked(member.Offset + memberElementIndex * LogixDataTypes.GetElementSize(member.DataType));
        _baseOffset = checked(rootElementIndex * root.ElementSize + MemberOffset);
        _isBool = member.DataType == LogixDataTypes.BOOL;
        _boolBit = member.ArraySize;
        TagType = member.DataType;
        ElementSize = LogixDataTypes.GetElementSize(member.DataType);
        if (ElementSize <= 0)
            throw new ArgumentException("UDT member is not an atomic Logix value.", nameof(member));
        ElementCount = _isBool ? 1 : Math.Max(1, member.ArraySize);
    }

    public ReadOnlySpan<byte> GetData(int byteOffset, int length)
    {
        if ((uint)byteOffset > (uint)DataSize || length < 0 || byteOffset + length > DataSize)
            throw new ArgumentOutOfRangeException(nameof(byteOffset));

        if (!_isBool)
            return _root.GetData(_baseOffset + byteOffset, length);

        var result = new byte[length];
        if (length != 0)
            result[0] = (byte)((_root.GetData(_baseOffset, 1)[0] >> _boolBit) & 1);
        return result;
    }

    public void SetData(ReadOnlySpan<byte> source, int byteOffset = 0)
    {
        if ((uint)byteOffset > (uint)DataSize || byteOffset + source.Length > DataSize)
            throw new ArgumentOutOfRangeException(nameof(byteOffset));

        if (!_isBool)
        {
            _root.SetData(source, _baseOffset + byteOffset);
            return;
        }

        if (source.Length == 0 || byteOffset != 0)
            throw new ArgumentException("A BOOL member requires one byte at offset zero.", nameof(source));

        byte host = _root.GetData(_baseOffset, 1)[0];
        byte mask = (byte)(1 << _boolBit);
        host = source[0] == 0 ? (byte)(host & ~mask) : (byte)(host | mask);
        _root.SetData(new[] { host }, _baseOffset);
    }
}