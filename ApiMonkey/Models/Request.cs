using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ApiMonkey.Models;

internal class Request : INotifyPropertyChanged
{
    public string Id { get; private set; }
    public RequestCollection? Collection { get; internal set; }

    private string? _name;
    public string? Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged();
            }
        }
    }

    public Request(RequestCollection? collection = null)
    {
        Id = Guid.NewGuid().ToString();
        Name = "Unnamed Request";
        Collection = collection;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}