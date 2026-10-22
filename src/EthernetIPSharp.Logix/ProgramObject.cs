﻿using EthernetIPSharp.Cip;

namespace EthernetIPSharp.Logix;

/// <summary>
/// Rockwell Program Object (class 0x68).
///
/// The ControlLogix Ethernet driver enumerates programs with service 0x4B on
/// the class (instance 0) during a project upload.
///
/// TODO: Make functional Program Scope - This server holds every tag at controller 
/// scope, so it has no programs: the enumeration succeeds with an empty list.
/// </summary> 
public sealed class ProgramObject
{
    public const uint ClassCode = 0x68;
    public const byte EnumeratePrograms = 0x4B;

    public CipClass CipClass { get; }

    public ProgramObject()
    {
        CipClass = new CipClass(ClassCode, "Program", revision: 1);
        CipClass.AddClassService(new CipServiceDefinition(
            EnumeratePrograms, "Enumerate_Programs", HandleEnumeratePrograms));
    }

    private static CipServiceResponse HandleEnumeratePrograms(
        CipInstance instance, CipServiceRequest request)
        => CipServiceResponse.Success(request.ServiceCode, ReadOnlyMemory<byte>.Empty);
}
