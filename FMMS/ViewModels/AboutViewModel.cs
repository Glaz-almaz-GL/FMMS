using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FMMS.Managers;
using FMMS.Models;
using Huskui.Avalonia.Controls;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace FMMS.ViewModels
{
    public partial class AboutViewModel : ViewModelBase
    {
        private readonly Stopwatch _uptimeStopwatch;

        public AboutViewModel()
        {
            _uptimeStopwatch = new Stopwatch();
            _uptimeStopwatch.Start();

            // Инициализация системной информации
            UpdateSystemInfo();
        }

        // Информация о приложении
        public static string AppName => "Files Metadata Management System";
        public static string AppVersion => Assembly.GetExecutingAssembly()?.GetName().Version?.ToString() ?? "1.0.0.0";
        public static string License => "MIT License";
        public static string AppStatus => "Стабильная версия";
        public static string AppDescription => "Система для анализа, обработки и управления метаданными файлов. Позволяет находить, извлекать и сохранять информацию о файлах в удобном формате.";

        // Информация о разработчике
        public static string DeveloperName => "Глазов Михаил";
        public static string DeveloperCompany => "Независимый разработчик";
        public static string DeveloperContact => "glazalmazgl@gmail.com";
        public static string GitHubUrl => "https://github.com/Glaz-almaz-GL/FMMS"; // Предполагаемый URL для FMMS
        public static string WebsiteUrl => "https://github.com/Glaz-almaz-GL/FMMS"; // Можно использовать GitHub или другой сайт

        // Системная информация
        [ObservableProperty]
        private string _osInfo = string.Empty;

        [ObservableProperty]
        private string _architecture = string.Empty;

        [ObservableProperty]
        private string _dotNetVersion = string.Empty;

        [ObservableProperty]
        private string _clrVersion = string.Empty;

        [ObservableProperty]
        private string _availableMemory = string.Empty;

        [ObservableProperty]
        private string _totalMemory = string.Empty;

        [ObservableProperty]
        private string _uptime = string.Empty;

        [ObservableProperty]
        private string _processorCount = string.Empty;

        [ObservableProperty]
        private string _processorArchitecture = string.Empty;

        private void UpdateSystemInfo()
        {
            try
            {
                // Операционная система
                OsInfo = RuntimeInformation.OSDescription;

                // Архитектура
                Architecture = RuntimeInformation.OSArchitecture.ToString();

                // Версия .NET
                DotNetVersion = Environment.Version.ToString();
                ClrVersion = RuntimeInformation.FrameworkDescription;

                // Время работы
                TimeSpan uptime = _uptimeStopwatch.Elapsed;
                Uptime = $"{uptime.Days} дн. {uptime.Hours:D2}:{uptime.Minutes:D2}:{uptime.Seconds:D2}";

                // Количество ядер
                ProcessorCount = Environment.ProcessorCount.ToString();
                ProcessorArchitecture = RuntimeInformation.ProcessArchitecture.ToString();

                // Пример получения доступной/всей памяти (может зависеть от платформы)
                // Это пример, точная реализация может отличаться.
                // В .NET 9+ доступна GC.GetGCMemoryInfo().TotalAvailableMemoryBytes
                // или сторонние библиотеки для получения системной памяти.
                // Для простоты оставим заглушки, как в оригинале, если не реализовано.
                // AvailableMemory = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes.ToString(); // Пример
                // TotalMemory = ... // Требует дополнительной логики получения общей памяти системы
            }
            catch (Exception ex)
            {
                OsInfo = "Не удалось получить информацию";
                Architecture = "Неизвестно";
                DotNetVersion = Environment.Version.ToString();
                ClrVersion = "Неизвестно";
                AvailableMemory = "Неизвестно";
                TotalMemory = "Неизвестно";
                Uptime = "Неизвестно";
                ProcessorCount = "Неизвестно";
                ProcessorArchitecture = "Неизвестно";

                // Логируем ошибку если нужно
                Debug.WriteLine($"Ошибка получения системной информации: {ex.Message}");
            }
        }

        [RelayCommand]
        private static void OpenGitHub()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = GitHubUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                GrowlsManager.ShowErrorMsg($"Не удалось открыть ссылку: {ex.Message}");
            }
        }

        [RelayCommand]
        private static void OpenEmail()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = $"mailto:{DeveloperContact}",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                GrowlsManager.ShowErrorMsg($"Не удалось открыть почтовый клиент: {ex.Message}");
            }
        }

        [RelayCommand]
        private static void OpenWebsite()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = WebsiteUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                GrowlsManager.ShowErrorMsg($"Не удалось открыть сайт: {ex.Message}");
            }
        }

        [RelayCommand]
        private static async Task CheckForUpdates()
        {
            GrowlItem? progressGrowl = GrowlsManager.ShowProgressInfoMsg("Проверка обновлений...", "Проверка обновления");
            await UpdateManager.CheckForUpdatesAsync(progressGrowl);
        }

        [RelayCommand]
        private void RefreshSystemInfo()
        {
            UpdateSystemInfo();
            GrowlsManager.ShowInfoMsg("Информация обновлена");
        }

        [RelayCommand]
        private static void OpenDocumentation()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    // Предполагаемый URL документации для FMMS
                    FileName = "https://github.com/Glaz-almaz-GL/FMMS/wiki",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                GrowlsManager.ShowErrorMsg($"Не удалось открыть документацию: {ex.Message}");
            }
        }

        [RelayCommand]
        private static void SuggestFeature()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    // Предполагаемый URL для создания обсуждения/issue для предложений
                    FileName = "https://github.com/Glaz-almaz-GL/FMMS/issues/new?assignees=&labels=enhancement&projects=&template=feature_request.md&title=Suggestion",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                GrowlsManager.ShowErrorMsg($"Не удалось открыть страницу предложений: {ex.Message}");
            }
        }

        [RelayCommand]
        private static void ReportBug()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    // Предполагаемый URL для создания issue для ошибок
                    FileName = "https://github.com/Glaz-almaz-GL/FMMS/issues/new?assignees=&labels=bug&projects=&template=bug_report.md&title=Bug",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                GrowlsManager.ShowErrorMsg($"Не удалось открыть страницу ошибок: {ex.Message}");
            }
        }
    }
}