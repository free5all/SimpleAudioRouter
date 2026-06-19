using System.Runtime.InteropServices;

namespace SimpleAudioRouter.Core.Vac;

public static class VacSetupExeResolver
{
    public static string? FindSetupExe(string directory)
    {
        if (!Directory.Exists(directory))
            return null;

        foreach (var fileName in GetPreferredSetupNames())
        {
            var match = Directory
                .EnumerateFiles(directory, fileName, SearchOption.AllDirectories)
                .FirstOrDefault();

            if (match is not null)
                return match;
        }

        return null;
    }

    public static string? ResolveFromExistingPath(string? setupPath)
    {
        if (string.IsNullOrWhiteSpace(setupPath))
            return null;

        var directory = Path.GetDirectoryName(setupPath);
        if (directory is null)
            return null;

        var resolved = FindSetupExe(directory);
        if (resolved is not null)
            return resolved;

        var parent = Directory.GetParent(directory)?.FullName;
        return parent is null ? null : FindSetupExe(parent);
    }

    private static IEnumerable<string> GetPreferredSetupNames()
    {
        return RuntimeInformation.OSArchitecture switch
        {
            Architecture.Arm64 => ["setup64a.exe", "setup64.exe", "setup.exe"],
            Architecture.X64 => ["setup64.exe", "setup.exe"],
            Architecture.X86 => ["setup.exe"],
            _ => ["setup64.exe", "setup.exe", "setup64a.exe"],
        };
    }
}
