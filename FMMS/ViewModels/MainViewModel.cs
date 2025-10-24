using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using FMMS.Items;
using FMMS.Models;
using FMMS.Views;
using Huskui.Avalonia.Controls;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace FMMS.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private NavigationItem? _currentNavigationItem;

    [ObservableProperty]
    private Page? _currentPage;

    public ObservableCollection<NavigationItem> NavigationItems { get; }

    public MainViewModel()
    {
        // Создаем страницы заранее
        HomePage homePage = new();
        SettingsPage settingsPage = new();
        AboutPage aboutPage = new();

        NavigationItems =
        [
                new NavigationItem()
                {
                    Title = "Главная",
                    Description = "Основная страница приложения",
                    Icon = Symbol.Home,
                    IsNew = false,
                    IsUpdated = false,
                    NavigationPage = homePage
                },
                new NavigationItem()
                {
                    Title = "Настройки",
                    Description = "Параметры приложения",
                    Icon = Symbol.Settings,
                    IsNew = false,
                    IsUpdated = false,
                    NavigationPage = settingsPage
                },
                new NavigationItem()
                {
                    Title = "О программе",
                    Description = "Информация о приложении",
                    Icon = Symbol.Info,
                    IsNew = false,
                    IsUpdated = false,
                    NavigationPage = aboutPage
                }
        ];

        CurrentNavigationItem = NavigationItems[0];
    }

    partial void OnCurrentNavigationItemChanged(NavigationItem? value)
    {
        // Когда CurrentNavigationItem изменяется, автоматически обновляем CurrentPage
        CurrentPage = value?.NavigationPage;
    }

    [RelayCommand]
    private static void OpenGitHub()
    {
        OpenUrl("https://github.com/Glaz-almaz-GL");
    }

    [RelayCommand]
    private static void OpenEmail()
    {
        OpenUrl("mailto:glazalmazgl@gmail.com");
    }

    private static void OpenUrl(string url)
    {
        try
        {
            ProcessStartInfo psi = new()
            {
                FileName = url,
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            GrowlsManager.ShowErrorMsg(ex, "Не удалось открыть ссылку");
        }
    }
}
