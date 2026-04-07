using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ApiMonkey.Models;

internal class RequestCollection : INotifyPropertyChanged
{
    public string Id { get; private set; }
    public List<Request> Requests { get; private set; } = [];

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

    public RequestCollection()
    {
        Id = Guid.NewGuid().ToString();
        Name = "Unnamed Collection";
    }


    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
