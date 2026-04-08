using Microsoft.UI.Xaml.Data;
using System;

namespace Makapi;

public class SendButtonTextConverter : IValueConverter
{
  public object Convert(object value, Type targetType, object parameter, string language)
  {
    if (value is bool isSending)
    {
      return isSending ? "Sending..." : "Send";
    }
    return "Send";
  }

  public object ConvertBack(object value, Type targetType, object parameter, string language)
  {
    throw new NotImplementedException();
  }
}
