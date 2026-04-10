using Makapi.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Makapi.MenuItemActions;

internal class FileOpener(XamlRoot xamlRoot, Func<string?, string?, Task> openCallback) : IMenuItemAction
{
    async void IMenuItemAction.Execute()
    {
        var picker = new FileOpenPicker(xamlRoot.ContentIslandEnvironment.AppWindowId);
        picker.FileTypeFilter.Add(".json");
        picker.CommitButtonText = "Open";

        var file = await picker.PickSingleFileAsync();
        if (file == null)
            return;

        var filePath = file.Path;

        bool isCollectionFile = filePath.EndsWith(
            $".{RequestCollection.EXTENSION}", StringComparison.OrdinalIgnoreCase);
        bool isRequestFile = filePath.EndsWith(
            $".{Request.EXTENSION}", StringComparison.OrdinalIgnoreCase);

        if (!isCollectionFile && !isRequestFile)
        {
            var errorDialog = new ContentDialog
            {
                Title = "Unrecognized File",
                Content = $"Please select a file ending in '.{Request.EXTENSION}' or '.{RequestCollection.EXTENSION}'.",
                CloseButtonText = "OK",
                XamlRoot = xamlRoot
            };
            await errorDialog.ShowAsync();
            return;
        }

        if (isCollectionFile)
        {
            var collectionDirectory = Path.GetDirectoryName(filePath)!;
            await openCallback(null, collectionDirectory);
            return;
        }

        // Request file – check whether its folder contains a collection index
        var requestDirectory = Path.GetDirectoryName(filePath)!;
        var collectionFiles = Directory.GetFiles(requestDirectory, $"*.{RequestCollection.EXTENSION}");

        if (collectionFiles.Length > 0)
        {
            var collectionName = Path.GetFileName(requestDirectory);
            var dialog = new ContentDialog
            {
                Title = "Request is part of a collection",
                Content = $"This request belongs to the \"{collectionName}\" collection. Open the entire collection instead?",
                PrimaryButtonText = "Open Collection",
                SecondaryButtonText = "Open Request Only",
                CloseButtonText = "Cancel",
                XamlRoot = xamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.None)
                return;

            if (result == ContentDialogResult.Primary)
            {
                await openCallback(null, requestDirectory);
                return;
            }
        }

        await openCallback(filePath, null);
    }
}
