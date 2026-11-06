namespace SRCthird.EthernetIPSharp.Cip
{
    public static class CipDataTypeCast
    {
        public static string GetCipDataType(ushort cipType)
        {
            return Enum.GetName(typeof(CipDataType), cipType) ?? $"0x{cipType:X4}";
        }

        public static string GetCSharpString(CipDataType baseType)
        {
            return baseType switch
            {
                CipDataType.Bool => "bool",
                CipDataType.Sint => "sbyte",
                CipDataType.Int => "short",
                CipDataType.Dint => "int",
                CipDataType.Lint => "long",
                CipDataType.Usint => "byte",
                CipDataType.Uint => "ushort",
                CipDataType.Udint => "uint",
                CipDataType.Ulint => "ulong",
                CipDataType.Real => "float",
                CipDataType.Lreal => "double",
                _ => "int",
            };
        }

        public static string GetCSharpString(ushort baseType)
        {
            return GetCSharpString((CipDataType)baseType);
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
            return GetCSharpType((CipDataType)baseType);
        }

        /// <summary>Returns the fixed size in bytes for a CIP data type, or -1 for variable-length types (strings).</summary>
        public static int GetFixedSize(CipDataType baseType)
        { 
            return baseType switch
            {
                CipDataType.Bool => 1,
                CipDataType.Sint => 1,
                CipDataType.Int => 2,
                CipDataType.Dint => 4,
                CipDataType.Lint => 8,
                CipDataType.Usint => 1,
                CipDataType.Uint => 2,
                CipDataType.Udint => 4,
                CipDataType.Ulint => 8,
                CipDataType.Real => 4,
                CipDataType.Lreal => 8,
                CipDataType.Byte => 1,
                CipDataType.Word => 2,
                CipDataType.Dword => 4,
                CipDataType.Lword => 8,
                CipDataType.ShortString => 81,
                _ => -1,
            };
        }

        /// <summary>Returns the fixed size in bytes for a CIP data type, or -1 for variable-length types (strings).</summary>
        public static int GetFixedSize(ushort baseType)
        {
            return GetFixedSize((CipDataType)baseType);
        }
    }
}
