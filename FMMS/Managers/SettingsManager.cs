using Avalonia;
using Avalonia.Styling;
using FMMS.Items;
using FMMS.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace FMMS.Managers
{
    public static class SettingsManager
    {
        private static readonly string _settingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FMMS", // Подкаталог для вашего приложения
            "settings.json" // Имя файла настроек
        );

        public static SettingsItem CurrentSettings { get; private set; } = new SettingsItem(); // Инициализируем по умолчанию

        private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };


        // Метод для загрузки настроек из файла
        public static void LoadSettings()
        {
            SettingsItem? loadedSettings = null;

            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    string jsonString = File.ReadAllText(_settingsFilePath);
                    loadedSettings = JsonSerializer.Deserialize<SettingsItem>(jsonString);

                    if (loadedSettings != null)
                    {
                        Debug.WriteLine($"Settings loaded from: {_settingsFilePath}");
                        // Применяем тему из загруженных настроек
                        ApplyTheme(loadedSettings.Theme);
                    }
                    else
                    {
                        Debug.WriteLine("Settings file was empty or invalid JSON structure.");
                    }
                }
                else
                {
                    Debug.WriteLine("Settings file not found, using defaults.");
                }
            }
            catch (JsonException jex)
            {
                GrowlsManager.ShowErrorMsg(jex);
                Debug.WriteLine($"JSON Error loading settings: {jex.Message}");
            }
            catch (Exception ex) // Ловим общие исключения (например, IOException при чтении)
            {
                GrowlsManager.ShowErrorMsg(ex);
                Debug.WriteLine($"General Error loading settings: {ex.Message}");
            }

            // Устанавливаем CurrentSettings: либо загруженный, либо новый по умолчанию
            CurrentSettings = loadedSettings ?? new SettingsItem();
        }


        // Метод для сохранения настроек в файл
        public static void SaveSettings(SettingsItem settings)
        {
            try
            {
                string jsonString = JsonSerializer.Serialize(settings, _jsonSerializerOptions);
                string? folderPath = Path.GetDirectoryName(_settingsFilePath);

                if (!string.IsNullOrWhiteSpace(folderPath) && !Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                File.WriteAllText(_settingsFilePath, jsonString);

                Debug.WriteLine($"Settings saved to: {_settingsFilePath}");
            }
            catch (Exception ex)
            {
                GrowlsManager.ShowErrorMsg(ex);
                Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        public static async Task SaveSettingsAsync(SettingsItem? settings = null)
        {
            try
            {
                string? jsonString = settings != null
                    ? JsonSerializer.Serialize(settings, _jsonSerializerOptions)
                    : JsonSerializer.Serialize(CurrentSettings, _jsonSerializerOptions);
                string? folderPath = Path.GetDirectoryName(_settingsFilePath);

                if (!string.IsNullOrWhiteSpace(folderPath) && !Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                await File.WriteAllTextAsync(_settingsFilePath, jsonString);

                Debug.WriteLine($"Settings saved to: {_settingsFilePath}");
            }
            catch (Exception ex)
            {
                GrowlsManager.ShowErrorMsg(ex);
                Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        // Метод для применения темы на уровне приложения
        public static void ApplyTheme(string themeName)
        {
            Application? app = Application.Current;
            app?.RequestedThemeVariant = themeName switch
            {
                "Dark" => ThemeVariant.Dark,
                "Light" => ThemeVariant.Light,
                "System" or _ => ThemeVariant.Default // Используем системную или светлую по умолчанию
            };
        }

        // Метод для получения пути к файлу настроек (например, для отладки или информации)
        public static string GetSettingsFilePath()
        {
            return _settingsFilePath;
        }
    }
}