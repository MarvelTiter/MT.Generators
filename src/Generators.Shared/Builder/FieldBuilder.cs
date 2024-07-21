using System;
using System.Collections.Generic;
using System.Text;

namespace Generators.Shared.Builder
{
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
        public override NodeType Type => NodeType.Property;
        public override string Indent => "        ";
        public override string ToString()
        {
            return $$"""
                {{Indent}}{{Modifiers}} {{MemberType}} {{Name}} { get; set; }
                """;
        }
    }
}
