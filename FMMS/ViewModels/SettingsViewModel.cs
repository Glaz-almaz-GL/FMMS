// ViewModels/SettingsViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FMMS.Items;
using FMMS.Managers;
using FMMS.Models;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace FMMS.ViewModels
{
    public partial class SettingsViewModel : ViewModelBase // Предполагается базовый класс ViewModelBase
    {
        // Объект, хранящий настройки
        [ObservableProperty]
        private SettingsItem _settings;

        // Коллекция для ComboBox выбора темы
        public ObservableCollection<string> ThemeOptions { get; } = ["System", "Light", "Dark"];
        // Коллекция для ComboBox выбора формата экспорта
        public ObservableCollection<string> ExportFormats { get; } = [".txt", ".xlsx"];

        // Конструктор или метод инициализации
        public SettingsViewModel()
        {
            // Загружаем настройки при создании ViewModel, используя SettingsManager
            Settings = SettingsManager.CurrentSettings;
        }

        // Команда для сохранения настроек
        [RelayCommand]
        private void SaveSettings()
        {
            try
            {
                // 1. Применить тему в приложении
                SettingsManager.ApplyTheme(Settings.Theme);

                // 2. Сохранить объект Settings в файл JSON, используя SettingsManager
                SettingsManager.SaveSettings(Settings);

                // Можно добавить уведомление об успешном сохранении (например, InfoBar или Growl)
                System.Diagnostics.Debug.WriteLine("Settings saved successfully via SettingsManager.");
            }
            catch (Exception ex)
            {
                GrowlsManager.ShowErrorMsg(ex);
                Debug.WriteLine($"Failed to save settings: {ex.Message}");
                // Обработка ошибки (например, показать InfoBar с ошибкой)
            }
        }
    }
}