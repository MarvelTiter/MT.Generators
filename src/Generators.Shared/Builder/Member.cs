
using System;

namespace Generators.Shared.Builder;

internal abstract class Node
{
    const string IndentUnit = "    ";
    private Node? parent;

    public abstract NodeType Type { get; }
    public string Indent => new(' ', 4 * Level);
    public virtual int Level => (Parent?.Level ?? -1) + 1;
    public Node? Parent
    {
        get
        {
            return parent;
        }
        set
        {
            parent = value;
        }
    }
    //public abstract new string ToString();
}
