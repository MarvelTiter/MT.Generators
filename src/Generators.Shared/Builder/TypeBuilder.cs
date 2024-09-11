using Generators.Models;
using System.Collections.Generic;
using System.Linq;

namespace Generators.Shared.Builder;

internal abstract class TypeBuilder : Node
{
    public List<Node> Members { get; } = [];
}
internal abstract class TypeBuilder<T> : TypeBuilder where T : TypeBuilder, new()
{
    //public static T Default(Node? parent) => new T() { Parent = parent };
    public static T Default => new T();
}

internal abstract class MemberBuilder : TypeBuilder
{
    public string? Name { get; set; }
    public string? Modifiers { get; set; }
    public string? MemberType { get; set; }
    public string? Initialization { get; set; }
    public IList<string> Attributes { get; } = [];
    public bool IsGeneric { get; set; }
    public IList<TypeParameterInfo> TypeArguments { get; set; } = [];
    protected string AttributeList => string.Join("\n", Attributes.Select(a => $"{Indent}[{a}]"));
    protected string Types
    {
        get
        {
            if (TypeArguments.Count == 0) return "";
            return $"<{string.Join(", ", TypeArguments.Select(ta => ta.Name))}>";
        }
    }
    protected string TypeConstraints
    {
        get
        {
            if (TypeArguments.Sum(t => t.Constraints.Length) == 0) return "";
            return $"\n{string.Join("\n", TypeArguments.Where(ta => ta.Constraints.Length > 0).Select(ta => $"{Indent}    where {ta.Name} : {string.Join(", ", ta.Constraints)}"))}";
        }
    }
}

internal abstract class MemberBuilder<T> : MemberBuilder where T : MemberBuilder, new()
{
    //public static T Default(Node parent) => new T() { Parent = parent };
    public static T Default => new T();
}

internal abstract class MethodBase : MemberBuilder
{
    public IList<string> Parameters { get; set; } = [];
    public IList<Statement> Body { get; set; } = [];
}
internal abstract class MethodBase<T> : MethodBase where T : MethodBase, new()
{
    //public static T Default(Node parent) => new T() { Parent = parent };
    public static T Default => new T();
    //public override string Indent => "        ";
}
