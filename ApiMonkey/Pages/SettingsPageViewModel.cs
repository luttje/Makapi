using ApiMonkey.Models;
using ApiMonkey.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ApiMonkey.Pages;

[ObservableObject]
public partial class SettingsPageViewModel
{
    [ObservableProperty]
    public partial XamlRoot? XamlRoot { get; set; }

    public ObservableCollection<RequestRootEntry> RequestRoots { get; }

    public SettingsPageViewModel()
    {
        RequestRoots = new ObservableCollection<RequestRootEntry>(
            SettingsManager.Settings.RequestRoots
                .Select(r => MakeEntry(r))
        );

        RequestRoots.CollectionChanged += (_, _) => Persist();
    }

    private RequestRootEntry MakeEntry(string path)
    {
        var container = new ChangingValueContainer(path);
        container.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ChangingValueContainer.Value))
                Persist();
        };
        return new RequestRootEntry(container, this);
    }

    [RelayCommand]
    private void AddPath()
    {
        RequestRoots.Add(MakeEntry(""));
    }

    [RelayCommand]
    private void RemovePath(RequestRootEntry? entry)
    {
        if (entry is not null)
            RequestRoots.Remove(entry);
    }

    [RelayCommand]
    private async Task BrowseAsync(RequestRootEntry? entry)
    {
        if (entry is null || XamlRoot is null)
            return;

        var picker = new FolderPicker(
            XamlRoot.ContentIslandEnvironment.AppWindowId);

        picker.CommitButtonText = "Pick Folder";
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.ViewMode = PickerViewMode.List;

        var folder = await picker.PickSingleFolderAsync();
        entry.Container.Value = folder?.Path ?? "";
    }

    private ChangingValueContainer Observe(ChangingValueContainer container)
    {
        container.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ChangingValueContainer.Value))
                Persist();
        };
        return container;
    }

    private void Persist()
    {
        SettingsManager.Settings.RequestRoots = RequestRoots
            .Select(r => r.Container.Value!)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct()
            .ToList();

        SettingsManager.Save();
        MainWindow.Current.RefreshMenuItems();
    }
}