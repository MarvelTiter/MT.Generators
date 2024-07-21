using Microsoft.CodeAnalysis;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Generators.Shared.Builder
{
    internal enum NodeType
    {
        Unit,
        NameSpace,
        Class,
        Interface,
        Record,
        Constructor,
        Field,
        Property,
        Method,
    }
    internal enum MemberType
    {
        Field,
        Property,
        Method,
        Constructor,
    }
    internal class ClassBuilder : MemberBuilder<ClassBuilder>
    {
        public ClassBuilder()
        {
            Modifiers = "public partial";
        }
        public override NodeType Type => NodeType.Class;
        public override string Indent => "    ";
        public string? BaseType { get; set; }
        public IList<string> Interfaces { get; } = [];
        private string BaseTypeList
        {
            get
            {
                if (BaseType == null && !Interfaces.Any())
                {
                    return "";
                }
                string?[] all = [BaseType, .. Interfaces];
                return $": {string.Join(", ", all.Where(b => !string.IsNullOrEmpty(b)))}";
            }
        }

        private IEnumerable<string> RenderMembers()
        {
            foreach (var m in Members.Where(m => m.Type == NodeType.Field))
            {
                yield return $"{m}";
            }
            foreach (var m in Members.Where(m => m.Type == NodeType.Constructor))
            {
                yield return $"{m}";
            }
            foreach (var m in Members.Where(m => m.Type == NodeType.Property))
            {
                yield return $"{m}";
            }
            foreach (var m in Members.Where(m => m.Type == NodeType.Method))
            {
                yield return $"{m}";
            }
        }

        public override string ToString()
        {
            return
$$"""
{{AttributeList}}
{{Indent}}/// <inheritdoc/>
{{Indent}}{{Modifiers}} class {{Name}} {{BaseTypeList}}
{{Indent}}{
{{string.Join("\n\n", RenderMembers())}}
{{Indent}}}
""";
        }
    }
}
