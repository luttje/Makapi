using Makapi.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
namespace Makapi.Pages;

public sealed partial class CollectionPage : Page
{
  internal CollectionPageViewModel ViewModel { get; } = new();
  private readonly RequestStore _requestStore;

  public CollectionPage()
  {
    _requestStore = App.Services.GetRequiredService<RequestStore>();

    InitializeComponent();
  }

  protected override void OnNavigatedTo(NavigationEventArgs e)
  {
    base.OnNavigatedTo(e);

    if (e.Parameter is not string collectionId)
    {
      throw new ArgumentNullException(nameof(collectionId));
    }

    ViewModel.CurrentCollection = _requestStore.GetCollectionById(collectionId);
  }
}
