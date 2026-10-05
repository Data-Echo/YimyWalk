using YimyWalk.CharacterTools;

return CharacterValidatorCli.Run(args);

public static class CharacterValidatorCli
{
    public static int Run(string[] args)
    {
        if (args.Length == 0 || args.Contains("--help", StringComparer.OrdinalIgnoreCase))
        {
            PrintUsage();
            return args.Length == 0 ? 2 : 0;
        }

        string? directory = null;
        string? previewPath = null;
        string? referencePath = null;
        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--preview":
                    if (++index >= args.Length) return UsageError("--preview requires an output path");
                    previewPath = args[index];
                    break;
                case "--reference":
                    if (++index >= args.Length) return UsageError("--reference requires an output path");
                    referencePath = args[index];
                    break;
                default:
                    if (args[index].StartsWith("-", StringComparison.Ordinal))
                        return UsageError($"unknown option '{args[index]}'");
                    if (directory is not null)
                        return UsageError("only one character directory may be supplied");
                    directory = args[index];
                    break;
            }
        }

        try
        {
            if (directory is null)
                return UsageError("a character directory is required");

            if (referencePath is not null)
            {
                CharacterPreviewWriter.EnsureOutputDoesNotReplaceRoleFile(directory, referencePath);
                CharacterPreviewWriter.WriteReference(referencePath);
                Console.WriteLine($"REFERENCE {Path.GetFullPath(referencePath)}");
            }

            using var report = CharacterValidator.Validate(directory);
            foreach (var diagnostic in report.Diagnostics) Console.WriteLine(diagnostic);
            if (!report.IsValid)
            {
                if (previewPath is not null && File.Exists(Path.GetFullPath(previewPath)))
                    Console.Error.WriteLine("PREVIEW NOT WRITTEN (existing output was left untouched)");
                return 1;
            }
            if (previewPath is not null)
            {
                CharacterPreviewWriter.EnsureOutputDoesNotReplaceRoleFile(directory, previewPath);
                CharacterPreviewWriter.WritePreview(report.Definition!, previewPath);
                Console.WriteLine($"PREVIEW {Path.GetFullPath(previewPath)}");
            }
            return 0;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            Console.Error.WriteLine($"ERROR output failed: {ex.Message}");
            return 1;
        }
    }

    private static int UsageError(string message)
    {
        Console.Error.WriteLine($"ERROR {message}");
        PrintUsage();
        return 2;
    }

    private static void PrintUsage() => Console.Error.WriteLine("Usage: CharacterValidator <character-directory> [--preview <png>] [--reference <png>]");
}
