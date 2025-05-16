using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace Vdk.Services
{
    public class VdkConfig
    {
        public string TenantId { get; set; } = string.Empty;
        public string Env { get; set; } = "VDK";
    }

    public static class ConfigManager
    {
        private static readonly string ConfigFileName = ".vdkconfig.json";
        private static string ConfigPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);

        public static VdkConfig? LoadConfig()
        {
            if (!File.Exists(ConfigPath)) return null;
            try
            {
                var json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<VdkConfig>(json);
            }
            catch
            {
                return null;
            }
        }

        public static void SaveConfig(VdkConfig config)
        {
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }

        public static VdkConfig EnsureConfig(Func<string?> promptTenantId, Action openBrowser)
        {
            var config = LoadConfig();
            if (config != null && !string.IsNullOrWhiteSpace(config.TenantId))
                return config;

            while (true)
            {
                Console.WriteLine("Enter your Tenant GUID (or leave blank to create an account):");
                var input = promptTenantId();
                if (string.IsNullOrWhiteSpace(input))
                {
                    Console.WriteLine("Opening browser to create an account...");
                    openBrowser();
                    continue;
                }
                // Validate GUID
                if (Guid.TryParse(input, out _))
                {
                    config = new VdkConfig { TenantId = input.Trim() };
                    SaveConfig(config);
                    return config;
                }
                Console.WriteLine("Invalid GUID. Please try again.");
            }
        }
    }
}
