using YimyWalk.Core.Settings;

namespace YimyWalk.App.Settings;

/// <summary>
/// Owns the user settings file and its recovery copy. The app layer owns the
/// filesystem and atomic replacement; the document itself remains platform
/// neutral in Core.
/// </summary>
internal sealed class SettingsStore
{
    private const string ApplicationDirectoryName = "YimyWalk";
    private const string SettingsFileName = "settings.json";
    private const string BackupFileName = "settings.json.bak";
    private const string TemporaryFileName = "settings.json.tmp";

    public SettingsStore(string? rootDirectory = null)
    {
        RootDirectory = Path.GetFullPath(rootDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            ApplicationDirectoryName));
        SettingsPath = Path.Combine(RootDirectory, SettingsFileName);
        BackupPath = Path.Combine(RootDirectory, BackupFileName);
        TemporaryPath = Path.Combine(RootDirectory, TemporaryFileName);
    }

    public string RootDirectory { get; }
    public string SettingsPath { get; }
    public string BackupPath { get; }
    public string TemporaryPath { get; }

    public SettingsLoadResult Load()
    {
        var diagnostics = new List<string>();

        if (TryRead(SettingsPath, out var settings, out var primaryError))
            return new SettingsLoadResult(settings, UsedDefaults: false, UsedBackup: false, null);
        if (!string.IsNullOrWhiteSpace(primaryError)) diagnostics.Add($"主设置文件：{primaryError}");

        if (TryRead(BackupPath, out settings, out var backupError))
            return new SettingsLoadResult(settings, UsedDefaults: false, UsedBackup: true,
                string.Join("；", diagnostics));
        if (!string.IsNullOrWhiteSpace(backupError)) diagnostics.Add($"备份设置文件：{backupError}");

        var diagnostic = diagnostics.Count == 0
            ? "设置文件不存在，已使用默认值"
            : string.Join("；", diagnostics);
        return new SettingsLoadResult(SettingsDocument.Defaults, UsedDefaults: true, UsedBackup: false, diagnostic);
    }

    public void Save(SettingsDocument settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var bytes = SettingsDocument.Serialize(settings);
        Directory.CreateDirectory(RootDirectory);

        try
        {
            // A fixed temp name keeps the replacement in the same directory
            // and therefore on the same volume as the destination.
            using (var stream = new FileStream(
                TemporaryPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                options: FileOptions.WriteThrough))
            {
                stream.Write(bytes);
                stream.Flush(flushToDisk: true);
            }

            if (File.Exists(SettingsPath))
            {
                try
                {
                    // File.Replace atomically swaps the destination and keeps
                    // the previous valid document as the recovery copy.
                    File.Replace(TemporaryPath, SettingsPath, BackupPath, ignoreMetadataErrors: true);
                }
                catch (PlatformNotSupportedException)
                {
                    ReplaceWithCopyFallback();
                }
                catch (IOException)
                {
                    ReplaceWithCopyFallback();
                }
            }
            else
            {
                File.Move(TemporaryPath, SettingsPath, overwrite: true);
            }
        }
        finally
        {
            TryDelete(TemporaryPath);
        }
    }

    private void ReplaceWithCopyFallback()
    {
        File.Copy(SettingsPath, BackupPath, overwrite: true);
        File.Move(TemporaryPath, SettingsPath, overwrite: true);
    }

    private static bool TryRead(string path, out SettingsDocument settings, out string error)
    {
        settings = SettingsDocument.Defaults;
        error = string.Empty;
        if (!File.Exists(path))
        {
            error = "文件不存在";
            return false;
        }

        try
        {
            var bytes = File.ReadAllBytes(path);
            if (SettingsDocument.TryParse(bytes, out settings, out error)) return true;
            return false;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            error = ex.Message;
            settings = SettingsDocument.Defaults;
            return false;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (IOException)
        {
            // A stale temp file is harmless if the process is terminating;
            // the next save will overwrite it.
        }
        catch (UnauthorizedAccessException)
        {
            // Preserve the original save exception when cleanup is denied.
        }
    }
}

internal sealed record SettingsLoadResult(
    SettingsDocument Settings,
    bool UsedDefaults,
    bool UsedBackup,
    string? Diagnostic);
