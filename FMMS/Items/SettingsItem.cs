using System.Text.Json.Serialization;

namespace FMMS.Items
{
    public class SettingsItem
    {
        // Тема приложения
        [JsonPropertyName("theme")]
        public string Theme { get; set; } = "System"; // Значение по умолчанию

        // Формат экспорта
        [JsonPropertyName("export_file_extension")]
        public string ExportFileExtension { get; set; } = ".txt"; // Значение по умолчанию

        // Автопроверка обновлений
        [JsonPropertyName("auto_check_for_updates")]
        public bool AutoCheckForUpdates { get; set; } = true; // Значение по умолчанию

        [JsonPropertyName("column_settings")]
        public ColumnSettingsItem ColumnSettings { get; set; } = new();
    }
}
