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
        private const string _githubToken = "YOUR-GITHUB-TOKEN";

        private const string _owner = "Glaz-almaz-GL";
        private const string _repo = "FMMS";

        private static readonly string _tempFolderPath = Path.GetTempPath();

        public static async Task CheckForUpdatesAsync(GrowlItem? progressGrowl = null)
        {
            try
            {
                // Обновляем прогресс
                UpdateProgress(progressGrowl, 10, "Получение информации о последнем релизе...", "Проверка обновления");

                JObject? latestRelease = await GetLatestRelease(_owner, _repo, _githubToken);

                if (latestRelease == null)
                {
                    UpdateProgress(progressGrowl, 100, "Не удалось получить информацию о релизе", "Проверка обновления", false);
                    return;
                }

                UpdateProgress(progressGrowl, 20, "Анализ информации о версии...", "Проверка обновления");

                string? latestVersion = latestRelease["tag_name"]?.ToString().Replace("v", "");

                // В методе CheckForUpdates
                if (latestRelease == null)
                {
                    GrowlsManager.ShowInfoMsg("Релизы не найдены. Убедитесь, что репозиторий содержит релизы.", "Обновление недоступно");
                    return;
                }

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
                    UpdateProgress(progressGrowl, 100, "В релизе нет файлов", "Установка обновления", false);
                    GrowlsManager.ShowErrorMsg("В релизе нет файлов.");
                    return;
                }

                Directory.CreateDirectory(_tempFolderPath);

                if (string.IsNullOrEmpty(latestVersion) || string.IsNullOrEmpty(downloadUrl))
                {
                    UpdateProgress(progressGrowl, 100, "Ошибка данных релиза", "Установка обновления", false);
                    GrowlsManager.ShowErrorMsg("Не удалось проверить обновления. Попробуйте снова.");
                    return;
                }

                if (!Uri.IsWellFormedUriString(downloadUrl, UriKind.Absolute))
                {
                    UpdateProgress(progressGrowl, 100, "Неверный URL", "Установка обновления", false);
                    GrowlsManager.ShowErrorMsg("Неверный формат downloadUrl: URL недействителен.");
                    return;
                }

                UpdateProgress(progressGrowl, 30, "Проверка наличия новой версии...", "Проверка обновления");

                if (IsNewerVersion(latestVersion))
                {
                    UpdateProgress(progressGrowl, 40, "Найдена новая версия", "Установка обновления");

                    string message = $"Доступна новая версия: {latestVersion}\nТекущая версия: {_appVersion}\nЖелаете обновить программу?";

                    bool? dialog = await DialogsManager.ShowMsgDialogAsync(message, "Доступно обновление", true, "Обновить", "Отмена");

                    if (dialog == true)
                    {
                        UpdateProgress(progressGrowl, 50, "Начало процесса обновления...", "Установка обновления");
                        await StartUpdateProcess(downloadUrl, _tempFolderPath, progressGrowl);
                    }
                    else
                    {
                        UpdateProgress(progressGrowl, 100, "Обновление отменено пользователем", "Установка обновления", false);
                    }
                }
                else
                {
                    UpdateProgress(progressGrowl, 100, $"Установлена последняя версия: {_appVersion}", "Проверка обновления", false);
                    GrowlsManager.ShowInfoMsg($"Уже установлена последняя версия: {_appVersion}", "Обновление не требуется");
                }
            }
            catch (Exception ex)
            {
                UpdateProgress(progressGrowl, 100, "Ошибка", "Проверка обновления", false);
                GrowlsManager.ShowErrorMsg(ex, "Ошибка при проверке обновлений");
            }
        }

        private static async Task StartUpdateProcess(string downloadUrl, string tempFolderPath, GrowlItem? progressGrowl = null)
        {
            try
            {
                UpdateProgress(progressGrowl, 60, "Подготовка к загрузке обновления...", "Скачивание");

                string tempSetupPath = Path.Combine(tempFolderPath, "setup.exe");

                using (HttpClient client = new())
                {
                    try
                    {
                        UpdateProgress(progressGrowl, 65, "Отправка запроса на сервер...", "Скачивание");

                        using HttpResponseMessage response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                        response.EnsureSuccessStatusCode(); // Это вызовет исключение при 404

                        long totalBytes = response.Content.Headers.ContentLength ?? -1L;
                        bool canReportProgress = totalBytes != -1;

                        UpdateProgress(progressGrowl, 70, "Начало загрузки файла...", "Скачивание");

                        await using Stream stream = await response.Content.ReadAsStreamAsync();
                        await using FileStream fileStream = new(tempSetupPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                        if (canReportProgress)
                        {
                            byte[] buffer = new byte[8192];
                            long totalBytesRead = 0;
                            int bytesRead;
                            int lastProgress = 70;

                            while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
                            {
                                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                                totalBytesRead += bytesRead;

                                if (totalBytes > 0)
                                {
                                    int currentProgress = 70 + (int)((double)totalBytesRead / totalBytes * 20);
                                    if (currentProgress > lastProgress)
                                    {
                                        lastProgress = currentProgress;
                                        UpdateProgress(progressGrowl, currentProgress, $"Загрузка... {totalBytesRead / 1024}KB / {totalBytes / 1024}KB", "Скачивание");
                                    }
                                }
                            }
                        }
                        else
                        {
                            UpdateProgress(progressGrowl, 80, "Загрузка файла...", "Скачивание");
                            await stream.CopyToAsync(fileStream);
                        }
                    }
                    catch (HttpRequestException e)
                    {
                        UpdateProgress(progressGrowl, 100, "Ошибка HTTP", "Установка", false);

                        if (e.Message.Contains("404"))
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = downloadUrl,
                                UseShellExecute = true
                            });
                            GrowlsManager.ShowErrorMsg("Файл обновления не найден. Проверьте наличие релиза на GitHub.", "Ошибка загрузки");
                        }
                        else
                        {
                            GrowlsManager.ShowErrorMsg($"Ошибка HTTP при обновлении: {e.Message}", "Ошибка загрузки");
                        }
                        return;
                    }
                    catch (Exception e)
                    {
                        UpdateProgress(progressGrowl, 100, "Критическая ошибка", "Установка", false);
                        GrowlsManager.ShowErrorMsg($"Критическая ошибка при обновлении: {e.Message}", "Ошибка загрузки");
                        return;
                    }
                }

                UpdateProgress(progressGrowl, 95, "Запуск установщика...", "Установка");

                ProcessStartInfo startInfo = new()
                {
                    FileName = tempSetupPath,
                    UseShellExecute = true,
                    Verb = "runas"
                };
                Process.Start(startInfo);

                UpdateProgress(progressGrowl, 100, "Установщик запущен. Приложение будет закрыто.", "Установка", false);

                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.Shutdown(0);
                }
            }
            catch (Exception ex)
            {
                UpdateProgress(progressGrowl, 100, "Ошибка запуска", "Установка", false);
                GrowlsManager.ShowErrorMsg(ex, "Ошибка при начале обновления");
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
            Version latest = new(latestVersion);
            return latest > _appVersion;
        }

        public static async Task<JObject?> GetLatestRelease(string owner, string repo, string GithubToken)
        {
            HttpClient client = new();

            client.DefaultRequestHeaders.UserAgent.ParseAdd("C# App");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GithubToken);
            client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");

            // Исправлен URL (убраны лишние пробелы)
            string url = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";

            try
            {
                HttpResponseMessage response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    JObject release = JObject.Parse(responseBody);

                    string? htmlUrl = release["html_url"]?.ToString();

                    if (!string.IsNullOrEmpty(htmlUrl))
                    {
                        return release;
                    }
                    else
                    {
                        GrowlsManager.ShowErrorMsg("Не удалось извлечь 'html_url' из ответа.", "Ошибка при проверке обновлений");
                    }
                }
                else
                {
                    GrowlsManager.ShowErrorMsg($"Ошибка при запросе новейшей версии: {(int)response.StatusCode} - {response.ReasonPhrase}", "Ошибка при проверке обновлений");
                }
            }
            catch (Exception ex)
            {
                GrowlsManager.ShowErrorMsg(ex);
            }

            return null;
        }
    }
}