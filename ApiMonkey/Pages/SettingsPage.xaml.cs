using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using ApiMonkey.Services;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using ApiMonkey.Models;
using Microsoft.Windows.Storage.Pickers;

namespace ApiMonkey.Pages;

public sealed partial class SettingsPage : Page
{
    private readonly ObservableCollection<ChangingValueContainer> _requestRoots;

    public SettingsPage()
    {
        InitializeComponent();

        _requestRoots = new ObservableCollection<ChangingValueContainer>(
            SettingsManager.Settings.RequestRoots.Select(
                r => ListenForChanges(new ChangingValueContainer(r))
            )
        );
        _requestRoots.CollectionChanged += RequestRoots_CollectionChanged;

        RequestRootsListView.ItemsSource = _requestRoots;
    }

    private ChangingValueContainer ListenForChanges(ChangingValueContainer container)
    {
        container.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(ChangingValueContainer.Value))
            {
                UpdateRoots();
            }
        };

        return container;
    }

    private void RequestRoots_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateRoots();
    }

    private void UpdateRoots()
    { 
        SettingsManager.Settings.RequestRoots = _requestRoots.Select(r => r.Value!)
            .ToList();
        SettingsManager.Save();
        MainWindow.Current.RefreshMenuItems();
    }

    private void AddPathButton_Click(object sender, RoutedEventArgs e)
    {
        _requestRoots.Add(
            ListenForChanges(new ChangingValueContainer(""))
        );
    }

    private async void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var container = button?.DataContext as ChangingValueContainer;

        if (container == null || button == null)
            return;

        // disable the button to avoid double-clicking
        button.IsEnabled = false;

        // Clear previous returned folder name
        container.Value = "";

        var picker = new FolderPicker(button.XamlRoot.ContentIslandEnvironment.AppWindowId);

        picker.CommitButtonText = "Pick Folder";
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.ViewMode = PickerViewMode.List;

        // Show the picker dialog window
        var folder = await picker.PickSingleFolderAsync();
        container.Value = folder != null
            ? folder.Path
            : "";

        button.IsEnabled = true;
    }

    private void RemovePathButton_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var container = button?.DataContext as ChangingValueContainer;

        if (container != null)
        {
            _requestRoots.Remove(container);
        }
    }
}
