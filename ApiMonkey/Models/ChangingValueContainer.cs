using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ApiMonkey.Models;

[ObservableObject]
internal partial class ChangingValueContainer
{
    [ObservableProperty]
    public partial string? Value { get; set; }

    public ChangingValueContainer(string value)
    {
        Value = value;
    }
}
