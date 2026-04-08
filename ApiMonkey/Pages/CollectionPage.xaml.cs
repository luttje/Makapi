using ApiMonkey.Models;
using ApiMonkey.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace ApiMonkey.Pages;

public sealed partial class CollectionPage : Page
{
    internal CollectionPageViewModel ViewModel { get; } = new();

    public CollectionPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        var collectionId = e.Parameter as string;

        if (collectionId == null)
        {
            throw new ArgumentNullException(nameof(collectionId));
        }

        ViewModel.CurrentCollection = RequestStore.Instance.GetCollectionById(collectionId);
    }
}
