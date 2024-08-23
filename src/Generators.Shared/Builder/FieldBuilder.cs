using System;
using System.Collections.Generic;
using System.Text;

namespace Generators.Shared.Builder;

internal class FieldBuilder : MemberBuilder<FieldBuilder>
{
    public FieldBuilder()
    {
        Modifiers = "private readonly";
    }
    public override NodeType Type => NodeType.Field;
    public override string Indent => "        ";
    public override string ToString()
    {
        return $"""
            {Indent}{Modifiers} {MemberType} {Name};
            """;
    }
}

internal class PropertyBuilder : MemberBuilder<PropertyBuilder>
{
    public PropertyBuilder()
    {
        Modifiers = "public";
    }
    public bool CanRead { get; set; } = true;
    public bool CanWrite { get; set; } = true;
    public override NodeType Type => NodeType.Property;
    public override string Indent => "        ";
    string Getter => CanRead ? " get;" : "";
    string Setter => CanWrite ? " set;" : "";
    public bool IsLambdaBody { get; set; }
    string InitStatement => string.IsNullOrEmpty(Initialization) ? "" : $" = {Initialization};";
    public override string ToString()
    {
        if (IsLambdaBody)
        {
            return $$"""
                {{Indent}}{{Modifiers}} {{MemberType}} {{Name}} => {{Initialization}};
                """;
        }
        else
        {
            return $$"""
                {{Indent}}{{Modifiers}} {{MemberType}} {{Name}} {{{Getter}}{{Setter}} }{{InitStatement}}
                """;
        }
    }
}
