using AutoPageStateContainerGenerator;

namespace Blazor.Test.Client.Pages;

public interface ICounter
{
    List<string> Values { get; set; }
    string? Name1 { get; set; }
    string? Type { get; set; }
}

[StateContainer( Name = "CounterData", Implements = typeof(ICounter))]
public partial class Counter : CounterParent
{
    [SaveState]
    public partial string? Name1 { get; set; }

    [SaveState]
    public partial int Index { get; set; }

    [SaveState(Init = "[]")]
    public partial List<string> Values { get; set; }

    public string? Name2 { get; set; }

}
