using System.Text.Json;

public class FileCleanupSettingsService
{
    private readonly string _settingsPath = Path.Combine(Directory.GetCurrentDirectory(), "filecleanupsettings.json");

    public FileCleanupSettings GetSettings()
    {
        if (!File.Exists(_settingsPath))
        {
            var defaultSettings = new FileCleanupSettings();
            SaveSettings(defaultSettings);
            return defaultSettings;
        }
        var json = File.ReadAllText(_settingsPath);
        return JsonSerializer.Deserialize<FileCleanupSettings>(json) ?? new FileCleanupSettings();
    }

    public void SaveSettings(FileCleanupSettings settings)
    {
        var json = JsonSerializer.Serialize(settings);
        File.WriteAllText(_settingsPath, json);
    }
}