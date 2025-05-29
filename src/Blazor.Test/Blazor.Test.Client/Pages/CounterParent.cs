using AutoPageStateContainerGenerator;
using Microsoft.AspNetCore.Components;

namespace Blazor.Test.Client.Pages
{
    //[StateContainer]
    public partial class CounterParent : ComponentBase
    {
        [SaveState]
        public virtual string? Type { get; set; } = "CounterParent";

        public void CheckType()
        {
            Console.WriteLine(Type);
        }
    }
}
