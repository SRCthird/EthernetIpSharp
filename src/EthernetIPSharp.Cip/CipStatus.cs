namespace EthernetIPSharp.Cip;

/// <summary>
/// CIP service response status — general status byte plus optional additional status words.
/// Encoded in the MR response after the reply service code and reserved byte.
/// </summary>
public readonly struct CipStatus
{
    /// <summary>General status code (0 = success).</summary>
    public byte GeneralStatus { get; init; }

    /// <summary>Optional additional status words providing more detail about the error.</summary>
    public ushort[] AdditionalStatus { get; init; }

    /// <summary>True if GeneralStatus is 0 (success).</summary>
    public bool IsSuccess => GeneralStatus == 0;

    /// <summary>Pre-built success status.</summary>
    public static readonly CipStatus Success = new() { GeneralStatus = 0, AdditionalStatus = [] };

    /// <summary>Create an error status with optional additional status words.</summary>
    public static CipStatus Error(byte general, params ushort[] additional) =>
        new() { GeneralStatus = general, AdditionalStatus = additional };

    // --- Common CIP general status codes ---

    /// <summary>0x00 — service completed successfully.</summary>
    public const byte SuccessStatus = 0x00;

    /// <summary>0x00FF - General Error</summary>
    public readonly struct GeneralError()
    {
        private static CipStatus Additional(ushort additionalStatus) =>
            new() { GeneralStatus = 0x00FF, AdditionalStatus = [additionalStatus] };

        public static CipStatus Default  => new() { GeneralStatus = 0x00FF };
        public static CipStatus AddressOutOfRange     => Additional(0x2104);
        public static CipStatus BeyondEndOfDataObject => Additional(0x2105);
        public static CipStatus DataInUse             => Additional(0x2106);
        public static CipStatus DatatypeUnsupported   => Additional(0x2107);
    }

    /// <summary>0x001F - Failure during connection</summary>
    public readonly struct FailureOnConnection()
    {
        private static CipStatus Additional(ushort additionalStatus) =>
            new() { GeneralStatus = 0x00F1, AdditionalStatus = [additionalStatus] };

        public static CipStatus Default => new() { GeneralStatus = 0x00F1 };
        public static CipStatus Timeout => Additional(0x0203);
    }

    /// <summary>0x01 - connection failure</summary>
    public readonly struct ConnectionFailure()
    {
        private static CipStatus Additional(ushort additionalStatus) =>
            new() { GeneralStatus = 0x01, AdditionalStatus = [additionalStatus] };

        public static CipStatus Default =>          new() { GeneralStatus = 0x01 };
        public static CipStatus ConnectionInUse               => Additional(0x0100);
        public static CipStatus TransportNotSupported         => Additional(0x0103);
        public static CipStatus OwnershipConflict             => Additional(0x0106);
        public static CipStatus ConnectionNotFound            => Additional(0x0107);
        public static CipStatus InvalidConnectionType         => Additional(0x0108);
        public static CipStatus InvalidConnectionSize         => Additional(0x0109);
        public static CipStatus ModuleNotConfigured           => Additional(0x0110);
        public static CipStatus EPRNotSupported               => Additional(0x0111);
        public static CipStatus WrongModule                   => Additional(0x0114);
        public static CipStatus WrongDeviceType               => Additional(0x0115);
        public static CipStatus WrongRevision                 => Additional(0x0116);
        public static CipStatus InvalidConfigurationFormat    => Additional(0x0118);
        public static CipStatus ApplicationOutOfConnections   => Additional(0x011A);
        public static CipStatus ConnectionTimeout             => Additional(0x0203);
        public static CipStatus UnconnectedMessageTimeout     => Additional(0x0204);
        public static CipStatus UnconnectedSendParameterError => Additional(0x0205);
        public static CipStatus MessageTooLarge               => Additional(0x0206);
        public static CipStatus NoBufferMemory                => Additional(0x0301);
        public static CipStatus BandwidthNotAvailable         => Additional(0x0302);
        public static CipStatus NoScreenersAvailable          => Additional(0x0303);
        public static CipStatus SignatureMatch                => Additional(0x0305);
        public static CipStatus PortNotAvailable              => Additional(0x0311);
        public static CipStatus LinkAddressNotAvailable       => Additional(0x0312);
        public static CipStatus InvalidSegmentType            => Additional(0x0315);
        public static CipStatus ConnectionNotScheduled        => Additional(0x0317);
        public static CipStatus LinkAddressToSelfIsInvalid    => Additional(0x0318);
    }

    /// <summary>0x02 - Insufficient resources </summary>
    public const byte InsufficientResources = 0x02;
    /// <summary>0x03 - Value invalid </summary>
    public const byte ValueInvalid = 0x03;
    /// <summary>
    /// 0x04 — IOI could not be deciphered or the tag does not exist.
    /// This may also occur when security permissions deny client access.
    /// </summary>
    public const byte IOINotDecipheredOrTagNotFound = 0x04;
    /// <summary>0x05 — unknown destination.</summary>
    public const byte UnknownDestination = 0x05;
    /// <summary>0x06 — requested data would not fit in the response packet.</summary>
    public const byte DataDoesNotFitInResponsePacket = 0x06;
    /// <summary>0x07 — loss of connection.</summary>
    public const byte LossOfConnection = 0x07;
    /// <summary>0x08 — unsupported service.</summary>
    public const byte UnsupportedService = 0x08;
    /// <summary>0x09 — error in data segment or invalid attribute value.</summary>
    public const byte ErrorInDataSegmentOrInvalidAttributeValue = 0x09;
    /// <summary>0x0A — attribute list error.</summary>
    public const byte AttributeListError = 0x0A;
    /// <summary>0x0B — state already exists.</summary>
    public const byte StateAlreadyExists = 0x0B;
    /// <summary>0x0C — object model conflict.</summary>
    public const byte ObjectModelConflict = 0x0C;
    /// <summary>0x0D — object already exists.</summary>
    public const byte ObjectAlreadyExists = 0x0D;
    /// <summary>0x0E — attribute is read-only.</summary>
    public const byte AttributeNotSettable = 0x0E;
    /// <summary>0x0F — permission denied.</summary>
    public const byte PermissionDenied = 0x0F;
    /// <summary>0x10 — device state conflict.</summary>
    public const byte DeviceStateConflict = 0x10;
    /// <summary>0x11 — reply does not fit.</summary>
    public const byte ReplyDoesNotFit = 0x11;
    /// <summary>0x12 — fragment primitive.</summary>
    public const byte FragmentPrimitive = 0x12;
    /// <summary>0x13 — insufficient command data or parameters.</summary>
    public const byte InsufficientCommandData = 0x13;
    /// <summary>0x14 — attribute not supported.</summary>
    public const byte AttributeNotSupported = 0x14;
    /// <summary>0x15 — too much data in the request.</summary>
    public const byte TooMuchData = 0x15;
    /// <summary>0x16 — object (instance) does not exist.</summary>
    public const byte ObjectDoesNotExist = 0x16;
    /// <summary>0x1A — bridge request too large.</summary>
    public const byte BridgeRequestTooLarge = 0x1A;
    /// <summary>0x1B — bridge response too large.</summary>
    public const byte BridgeResponseTooLarge = 0x1B;
    /// <summary>0x1C — attribute list shortage.</summary>
    public const byte AttributeListShortage = 0x1C;
    /// <summary>0x1D — invalid attribute list.</summary>
    public const byte InvalidAttributeList = 0x1D;
    /// <summary>0x1E — embedded service error.</summary>
    public const byte EmbeddedServiceError = 0x1E;
    /// <summary>0x1F — failure during connection.</summary>
    public const byte FailureDuringConnection = 0x1F;
    /// <summary>0x20 — invalid parameter.</summary>
    public const byte InvalidParameter = 0x20;
    /// <summary>0x22 — invalid reply received.</summary>
    public const byte InvalidReplyReceived = 0x22;
    /// <summary>0x25 — key segment error.</summary>
    public const byte KeySegmentError = 0x25;
    /// <summary>0x26 — number of IOI words does not match the IOI word count.</summary>
    public const byte IOIWordCountMismatch = 0x26;
    /// <summary>0x27 — unexpected attribute in list.</summary>
    public const byte UnexpectedAttributeInList = 0x27;

    /// <summary>
    /// Encode to wire format: general_status(1) + additional_status_size(1) + additional_status(N*2).
    /// Returns the number of bytes written.
    /// </summary>
    public int Encode(Span<byte> dst)
    {
        int offset = 0;
        dst[offset++] = GeneralStatus;
        var additional = AdditionalStatus ?? [];
        dst[offset++] = (byte)additional.Length;
        foreach (var s in additional)
        {
            CipDataSerializer.WriteUint(dst.Slice(offset), s);
            offset += 2;
        }
        return offset;
    }
}
