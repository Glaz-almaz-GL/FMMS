using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using FMMS.Models;
using Huskui.Avalonia.Controls;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;

namespace FMMS.Managers
{
    public static class UpdateManager
    {
        private static readonly Version _appVersion = Assembly.GetExecutingAssembly()?.GetName().Version ?? new Version("1.0.0.0");

        private const string _owner = "Glaz-almaz-GL";
        private const string _repo = "FMMS";

        private static readonly string _tempFolderPath = Path.GetTempPath();

        private const string CheckingForUpdatesTitle = "Проверка обновления";
        private const string InstallingUpdateTitle = "Установка обновления";
        private const string DownloadingUpdateTitle = "Скачивание";
        private const string SetupFileName = "setup.exe";
        private const int BufferSize = 8192;
        private const int ProgressDownloadStart = 70;
        private const int ProgressDownloadEnd = 90;
        private const int ProgressDownloadRange = ProgressDownloadEnd - ProgressDownloadStart;
        private const string DownloadFailedTitle = "Ошибка загрузки";
        private const string CheckingUpdateFailedTitle = "Ошибка при проверке обновлений";

        public static async Task CheckForUpdatesAsync(GrowlItem? progressGrowl = null)
        {
            try
            {
                UpdateProgress(progressGrowl, 10, "Получение информации о последнем релизе...", CheckingForUpdatesTitle);

                JObject? latestRelease = await GetLatestRelease(_owner, _repo);

                if (latestRelease == null)
                {
                    UpdateProgress(progressGrowl, 100, "Не удалось получить информацию о релизе", CheckingForUpdatesTitle, false);
                    return;
                }

                UpdateProgress(progressGrowl, 20, "Анализ информации о версии...", CheckingForUpdatesTitle);

                string? latestVersion = latestRelease["tag_name"]?.ToString().Replace("v", "");

                // Проверяем, есть ли ассеты
                JArray? assets = latestRelease["assets"] as JArray;
                if (assets?.Count == 0)
                {
                    GrowlsManager.ShowInfoMsg("В релизе нет файлов для загрузки.", "Обновление недоступно");
                    return;
                }

                string? downloadUrl;

                if (assets?.Count > 0)
                {
                    JToken firstAsset = assets[0];
                    downloadUrl = firstAsset["browser_download_url"]?.ToString();
                }
                else
                {
                    UpdateProgress(progressGrowl, 100, "В релизе нет файлов", InstallingUpdateTitle, false);
                    GrowlsManager.ShowErrorMsg("В релизе нет файлов.");
                    return;
                }

                Directory.CreateDirectory(_tempFolderPath);

                if (string.IsNullOrEmpty(latestVersion) || string.IsNullOrEmpty(downloadUrl))
                {
                    UpdateProgress(progressGrowl, 100, "Ошибка данных релиза", InstallingUpdateTitle, false);
                    GrowlsManager.ShowErrorMsg("Не удалось проверить обновления. Попробуйте снова.");
                    return;
                }

                if (!Uri.IsWellFormedUriString(downloadUrl, UriKind.Absolute))
                {
                    UpdateProgress(progressGrowl, 100, "Неверный URL", InstallingUpdateTitle, false);
                    GrowlsManager.ShowErrorMsg("Неверный формат downloadUrl: URL недействителен.");
                    return;
                }

                UpdateProgress(progressGrowl, 30, "Проверка наличия новой версии...", CheckingForUpdatesTitle);

                if (IsNewerVersion(latestVersion))
                {
                    UpdateProgress(progressGrowl, 40, "Найдена новая версия", InstallingUpdateTitle);

                    string message = $"Доступна новая версия: {latestVersion}\nТекущая версия: {_appVersion}\nЖелаете обновить программу?";

                    bool? dialog = await DialogsManager.ShowMsgDialogAsync(message, "Доступно обновление", true, "Обновить", "Отмена");

                    if (dialog == true)
                    {
                        UpdateProgress(progressGrowl, 50, "Начало процесса обновления...", InstallingUpdateTitle);
                        await StartUpdateProcess(downloadUrl, _tempFolderPath, progressGrowl);
                    }
                    else
                    {
                        UpdateProgress(progressGrowl, 100, "Обновление отменено пользователем", InstallingUpdateTitle, false);
                    }
                }
                else
                {
                    UpdateProgress(progressGrowl, 100, $"Установлена последняя версия: {_appVersion}", CheckingForUpdatesTitle, false);
                    GrowlsManager.ShowInfoMsg($"Уже установлена последняя версия: {_appVersion}", "Обновление не требуется");
                }
            }
            catch (Exception ex)
            {
                UpdateProgress(progressGrowl, 100, "Ошибка", CheckingForUpdatesTitle, false);
                GrowlsManager.ShowErrorMsg(ex, CheckingUpdateFailedTitle);
            }
        }

        /// <summary>
        /// Загружает файл обновления и запускает установку.
        /// </summary>
        /// <param name="downloadUrl">URL-адрес файла обновления.</param>
        /// <param name="tempFolderPath">Путь к временной папке для сохранения файла.</param>
        /// <param name="progressGrowl">Элемент для отображения прогресса (может быть null).</param>
        /// <returns></returns>
        private static async Task StartUpdateProcess(string downloadUrl, string tempFolderPath, GrowlItem? progressGrowl = null)
        {
            string tempFilePath = Path.Combine(tempFolderPath, SetupFileName);

            try
            {
                UpdateProgress(progressGrowl, 60, "Подготовка к загрузке обновления...", DownloadingUpdateTitle);

                bool downloadSuccessful = await DownloadFileAsync(downloadUrl, tempFilePath, progressGrowl);

                if (!downloadSuccessful)
                {
                    return; // Ошибка уже обработана в DownloadFileAsync
                }

                UpdateProgress(progressGrowl, 95, "Запуск установщика...", InstallingUpdateTitle);

                if (!TryStartInstaller(tempFilePath))
                {
                    UpdateProgress(progressGrowl, 100, "Ошибка запуска установщика", InstallingUpdateTitle, false);
                    return; // Ошибка уже показана
                }

                UpdateProgress(progressGrowl, 100, "Установщик запущен. Приложение будет закрыто.", InstallingUpdateTitle, false);

                // Закрытие приложения после запуска установщика
                CloseApplication();
            }
            catch (Exception ex)
            {
                UpdateProgress(progressGrowl, 100, "Ошибка во время подготовки к обновлению", InstallingUpdateTitle, false);
                GrowlsManager.ShowErrorMsg(ex, "Ошибка при подготовке к обновлению");
            }
        }

        /// <summary>
        /// Асинхронно загружает файл по указанному URL с отслеживанием прогресса.
        /// </summary>
        /// <param name="url">URL-адрес файла для загрузки.</param>
        /// <param name="filePath">Локальный путь для сохранения файла.</param>
        /// <param name="progressGrowl">Элемент для отображения прогресса (может быть null).</param>
        /// <returns>True, если загрузка прошла успешно, иначе False.</returns>
        private static async Task<bool> DownloadFileAsync(string url, string filePath, GrowlItem? progressGrowl)
        {
            try
            {
                UpdateProgress(progressGrowl, 65, "Отправка запроса на сервер...", DownloadingUpdateTitle);

                using var httpClient = new HttpClient();
                using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                long totalBytes = response.Content.Headers.ContentLength ?? -1L;
                bool canReportProgress = totalBytes > 0;

                UpdateProgress(progressGrowl, 70, "Начало загрузки файла...", DownloadingUpdateTitle);

                var downloadTask = canReportProgress
                    ? DownloadWithProgressAsync(response, filePath, totalBytes, progressGrowl)
                    : DownloadWithoutProgressAsync(response, filePath);

                await downloadTask;
                return true;
            }
            catch (HttpRequestException httpEx)
            {
                HandleHttpError(httpEx, url, progressGrowl);
                return false;
            }
            catch (Exception generalEx)
            {
                UpdateProgress(progressGrowl, 100, "Критическая ошибка", InstallingUpdateTitle, false);
                GrowlsManager.ShowErrorMsg($"Критическая ошибка при загрузке: {generalEx.Message}", DownloadFailedTitle);
                return false;
            }
        }

        /// <summary>
        /// Загружает файл с отслеживанием прогресса.
        /// </summary>
        private static async Task DownloadWithProgressAsync(HttpResponseMessage response, string filePath, long totalBytes, GrowlItem? progressGrowl)
        {
            var buffer = new byte[BufferSize];
            long totalBytesRead = 0;
            int lastProgressValue = ProgressDownloadStart;

            await using var stream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, useAsync: true);

            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                totalBytesRead += bytesRead;

                int currentProgress = ProgressDownloadStart + (int)((double)totalBytesRead / totalBytes * ProgressDownloadRange);
                if (currentProgress > lastProgressValue)
                {
                    lastProgressValue = currentProgress;
                    UpdateProgress(progressGrowl, currentProgress,
                        $"Загрузка... {totalBytesRead / 1024:N0} KB / {totalBytes / 1024:N0} KB", DownloadingUpdateTitle);
                }
            }
        }

        /// <summary>
        /// Загружает файл без отслеживания прогресса.
        /// </summary>
        private static async Task DownloadWithoutProgressAsync(HttpResponseMessage response, string filePath)
        {
            await using var stream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, useAsync: true);
            await stream.CopyToAsync(fileStream);
        }

        /// <summary>
        /// Обрабатывает ошибки HTTP при загрузке.
        /// </summary>
        private static void HandleHttpError(HttpRequestException e, string downloadUrl, GrowlItem? progressGrowl)
        {
            UpdateProgress(progressGrowl, 100, "Ошибка HTTP", InstallingUpdateTitle, false);

            if (e.StatusCode == System.Net.HttpStatusCode.NotFound) // 404
            {
                // Попытка открыть URL в браузере как резервный вариант
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = downloadUrl,
                        UseShellExecute = true
                    });
                }
                catch (Exception processEx)
                {
                    GrowlsManager.ShowErrorMsg($"Не удалось открыть URL в браузере: {processEx.Message}", DownloadFailedTitle);
                }

                GrowlsManager.ShowErrorMsg("Файл обновления не найден. Проверьте наличие релиза на GitHub.", DownloadFailedTitle);
            }
            else
            {
                GrowlsManager.ShowErrorMsg($"Ошибка HTTP при загрузке: {e.StatusCode} - {e.Message}", DownloadFailedTitle);
            }
        }

        /// <summary>
        /// Пытается запустить установщик с правами администратора.
        /// </summary>
        /// <param name="installerPath">Путь к файлу установщика.</param>
        /// <returns>True, если запуск был успешной попыткой, иначе False.</returns>
        private static bool TryStartInstaller(string installerPath)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = installerPath,
                    UseShellExecute = true,
                    Verb = "runas" // Запуск от имени администратора
                };
                Process.Start(startInfo);
                return true;
            }
            catch (Exception ex)
            {
                GrowlsManager.ShowErrorMsg($"Не удалось запустить установщик: {ex.Message}", "Ошибка установки");
                return false;
            }
        }

        /// <summary>
        /// Закрывает текущее приложение.
        /// </summary>
        private static void CloseApplication()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown(0);
            }
        }

        private static void UpdateProgress(GrowlItem? growlItem, double progress, string message, string title, bool showProgress = true)
        {
            if (growlItem != null)
            {
                growlItem.Title = title;
                growlItem.Content = message;
                growlItem.Progress = progress;
                growlItem.IsProgressBarVisible = showProgress;
                growlItem.UpdateLayout();
            }
        }

        private static bool IsNewerVersion(string latestVersion)
        {
            if (!Version.TryParse(latestVersion, out var latest))
            {
                // Логирование или обработка неверного формата версии
                return false;
            }
            return latest > _appVersion;
        }

        public static async Task<JObject?> GetLatestRelease(string owner, string repo)
        {
            using var handler = new HttpClientHandler();
            using var client = new HttpClient(handler);

            client.DefaultRequestHeaders.UserAgent.ParseAdd("FMMS-App");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");

            string url = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";

            try
            {
                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    var release = JObject.Parse(responseBody);

                    string? htmlUrl = release["html_url"]?.ToString();
                    string? tagName = release["tag_name"]?.ToString();

                    if (!string.IsNullOrEmpty(htmlUrl) || !string.IsNullOrEmpty(tagName))
                    {
                        return release;
                    }
                    else
                    {
                        GrowlsManager.ShowErrorMsg("Ответ от GitHub API не содержит ожидаемых данных (html_url или tag_name).", CheckingUpdateFailedTitle);
                    }
                }
                else
                {
                    string errorMessage = $"Ошибка при запросе к GitHub API: {(int)response.StatusCode} - {response.ReasonPhrase}";
                    if ((int)response.StatusCode == 403)
                    {
                        errorMessage += "\nВозможно, превышено ограничение на количество запросов (Rate Limit).";
                    }
                    GrowlsManager.ShowErrorMsg(errorMessage, CheckingUpdateFailedTitle);
                }
            }
            catch (HttpRequestException httpEx)
            {
                GrowlsManager.ShowErrorMsg($"Сетевая ошибка при обращении к GitHub: {httpEx.Message}", CheckingUpdateFailedTitle);
            }
            catch (Newtonsoft.Json.JsonReaderException jsonEx)
            {
                GrowlsManager.ShowErrorMsg($"Ошибка при обработке ответа от GitHub (некорректный JSON): {jsonEx.Message}", CheckingUpdateFailedTitle);
            }
            catch (Exception ex)
            {
                GrowlsManager.ShowErrorMsg($"Непредвиденная ошибка при получении информации о релизе: {ex.Message}", CheckingUpdateFailedTitle);
            }

            return null;
        }
    }
}