using AutoPageStateContainerGenerator;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Blazor.Test.Client.Pages;

[StateContainer]
public partial class WeatherBase<T1, T2>
    where T1 : struct
{
    [SaveState]
    public partial T1 Value1 { get; set; }

    [SaveState]
    public partial T2 Value2 { get; set; }
}
