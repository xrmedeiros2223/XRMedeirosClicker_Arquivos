using System.IO;
using System.Text.Json;

namespace AutoClickerIA.Config;

public static class ConfigManager
{
    private static readonly string ConfigPath =
        Path.Combine(AppContext.BaseDirectory, "config.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(ConfigPath))
                return new AppSettings();

            string json = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            string json = JsonSerializer.Serialize(
                settings,
                new JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(ConfigPath, json);
        }
        catch
        {
            // Mantém o programa funcionando mesmo se a configuração não puder ser salva.
        }
    }
}
