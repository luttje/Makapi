using Makapi.Messages;
using Makapi.Models;
using Makapi.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Makapi.Pages;

[ObservableObject]
public partial class SettingsPageViewModel
{
    private readonly SettingsManager _settingsManager;
    private readonly IMessenger _messenger;

    [ObservableProperty]
    public partial XamlRoot? XamlRoot { get; set; }

    public ObservableCollection<RequestRootEntry> RequestRoots { get; }

    public SettingsPageViewModel()
    {
        _settingsManager = App.Services.GetRequiredService<SettingsManager>();
        _messenger = App.Services.GetRequiredService<IMessenger>();

        RequestRoots = new ObservableCollection<RequestRootEntry>(
            _settingsManager.Settings.RequestRoots
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
        _settingsManager.Settings.RequestRoots = RequestRoots
            .Select(r => r.Container.Value!)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct()
            .ToList();

        _settingsManager.Save();
        _messenger.Send(new SettingsChangedMessage());
    }
}