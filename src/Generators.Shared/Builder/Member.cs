
namespace Generators.Shared.Builder;

internal abstract class Node
{
    public abstract NodeType Type { get; }
    public virtual string Indent => "";
    //public abstract new string ToString();
}
