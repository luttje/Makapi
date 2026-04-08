using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ApiMonkey.Models;

internal class ChangingValueContainer : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
     
    private string? _value;

    public ChangingValueContainer(string value)
    {
        _value = value;
    }

    public string? Value
    {
        get => _value;
        set
        {
            if (_value != value)
            {
                _value = value;
                OnPropertyChanged();
            }
        }
    }
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
