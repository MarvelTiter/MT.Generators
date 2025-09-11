using AutoPageStateContainerGenerator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorTest.Pages;

[StateContainer]
public partial class TestPage
{
    [SaveState]
    public partial int Index { get; set; }
}
