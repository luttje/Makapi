using ApiMonkey.Models;
using ApiMonkey.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiMonkey.MenuItemActions;

internal class CollectionCreator(XamlRoot xamlRoot, RequestStore requestStore, SettingsManager settingsManager) : IMenuItemAction
{
    // Show a dialog to ask for the path to where the collection should be created, then create the collection and save it to disk
    async void IMenuItemAction.Execute()
    {
        var path = await ShowDialogFor();

        if (path == null)
            return;

        requestStore.CreateCollection(path);
    }

    private async Task<string?> ShowDialogFor()
    {
        var dialog = new ContentDialog
        {
            Title = "Create Collection",
            PrimaryButtonText = "Create",
            CloseButtonText = "Cancel",
            XamlRoot = xamlRoot,
            Width = 512,
        };

        dialog.Content = CreateBrowseField(dialog);

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            return dialog.Tag as string;
        }

        return null;
    }

    private StackPanel CreateBrowseField(ContentDialog dialog)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
        };

        var label = new TextBlock
        {
            Text = "Choose the folder where your collection is saved. A good place is a subfolder inside your version controlled project.",
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 5),
        };

        panel.Children.Add(label);

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };

        panel.Children.Add(grid);

        var defaultPath = settingsManager.GetNewCollectionPath();
        dialog.Tag = defaultPath;

        var textBox = new TextBox
        {
            Text = defaultPath,
            Margin = new Thickness(0, 0, 8, 0)
        };
        textBox.TextChanged += (sender, args) =>
        {
            dialog.Tag = textBox.Text;
        };

        grid.Children.Add(textBox);
        Grid.SetColumn(textBox, 0);

        var button = new Button
        {
            Content = "Browse",
        };

        button.Click += async (sender, args) =>
        {
            var picker = new FolderPicker(button.XamlRoot.ContentIslandEnvironment.AppWindowId)
            {
                CommitButtonText = "Pick Folder",
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                ViewMode = PickerViewMode.List
            };

            var folder = await picker.PickSingleFolderAsync();

            if (folder != null)
                textBox.Text = folder.Path;
        };

        grid.Children.Add(button);
        Grid.SetColumn(button, 1);

        return panel;
    }
}