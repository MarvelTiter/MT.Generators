using AutoPageStateContainerGenerator;

namespace Blazor.Test.Client.Pages;

[StateContainer(Lifetime = ServiceLifetime.Singleton)]
public partial class Counter : CounterParent
{
    //[SaveState]
    //private string? name1;

    //[SaveState]
    //private int index;

    //[SaveState]
    //private List<string> values = [];
    [SaveState]
    public partial string? Name1 { get; set; }

    [SaveState]
    public partial int Index { get; set; }

    [SaveState(Init = "[]")]
    public partial List<string> Values { get; set; }

    public string? Name2 { get; set; }

}
