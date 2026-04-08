using ApiMonkey.Models;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.IO;

namespace ApiMonkey.Pages;

internal partial class CollectionPageViewModel
{
    internal RequestCollection CurrentCollection { get; set; }

    [RelayCommand]
    private void OpenCollectionFolder()
    {
        var path = CurrentCollection.Path;

        // Evaluate the path, as it may have been changed by Windows (e.g: by scoping the app to a specific folder)
        path = Path.GetFullPath(path);

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true,
        });
    }
}
