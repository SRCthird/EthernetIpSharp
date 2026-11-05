namespace SRCthird.EthernetIPSharp.Cip
{
    public static class CipDataTypeCast
    {
        public static string GetCipDataType(ushort cipType)
        {
            return Enum.GetName(typeof(CipDataType), cipType) ?? $"0x{cipType:X4}";
        }

        public static Type GetCSharpType(CipDataType baseType)
        {
            return baseType switch
            {
                CipDataType.Bool => typeof(bool),
                CipDataType.Sint => typeof(sbyte),
                CipDataType.Int => typeof(short),
                CipDataType.Dint => typeof(int),
                CipDataType.Lint => typeof(long),
                CipDataType.Usint => typeof(byte),
                CipDataType.Uint => typeof(ushort),
                CipDataType.Udint => typeof(uint),
                CipDataType.Ulint => typeof(ulong),
                CipDataType.Real => typeof(float),
                CipDataType.Lreal => typeof(double),
                _ => typeof(int),
            };
        }

        public static Type GetCSharpType(ushort baseType)
        {
            return baseType switch
            {
                (ushort)CipDataType.Bool => typeof(bool),
                (ushort)CipDataType.Sint => typeof(sbyte),
                (ushort)CipDataType.Int => typeof(short),
                (ushort)CipDataType.Dint => typeof(int),
                (ushort)CipDataType.Lint => typeof(long),
                (ushort)CipDataType.Usint => typeof(byte),
                (ushort)CipDataType.Uint => typeof(ushort),
                (ushort)CipDataType.Udint => typeof(uint),
                (ushort)CipDataType.Ulint => typeof(ulong),
                (ushort)CipDataType.Real => typeof(float),
                (ushort)CipDataType.Lreal => typeof(double),
                _ => typeof(int),
            };
        }

    }
}
