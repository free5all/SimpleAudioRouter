using System.Runtime.InteropServices;

namespace SimpleAudioRouter.Core.Native;

internal enum ERole
{
    Console = 0,
    Multimedia = 1,
    Communications = 2,
}

[ComImport]
[Guid("f8679669-850a-41cf-9c72-430f290290c8")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IPolicyConfig
{
    [PreserveSig]
    int Unused1();

    [PreserveSig]
    int Unused2();

    [PreserveSig]
    int Unused3();

    [PreserveSig]
    int Unused4();

    [PreserveSig]
    int Unused5();

    [PreserveSig]
    int Unused6();

    [PreserveSig]
    int Unused7();

    [PreserveSig]
    int Unused8();

    [PreserveSig]
    int Unused9();

    [PreserveSig]
    int Unused10();

    [PreserveSig]
    int SetDefaultEndpoint([MarshalAs(UnmanagedType.LPWStr)] string deviceId, int role);
}

[ComImport]
[Guid("870af99c-171d-4f9e-af0d-e63df40c2bc9")]
internal class PolicyConfigClient;

internal static class PolicyConfigInterop
{
    public static void SetDefaultEndpoint(string deviceId, ERole role)
    {
        var policy = (IPolicyConfig)new PolicyConfigClient();
        var hr = policy.SetDefaultEndpoint(deviceId, (int)role);
        Marshal.ThrowExceptionForHR(hr);
    }
}
