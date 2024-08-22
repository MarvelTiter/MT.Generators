using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Generators.Shared.Builder
{
    internal class MethodBuilder : MethodBase<MethodBuilder>
    {
        public MethodBuilder()
        {
            Modifiers = "public";
        }
        public override NodeType Type => NodeType.Method;
        public bool IsAsync { get; set; }
        public bool IsLambdaBody { get; set; }
        string Async => IsAsync ? " async " : " ";
        public string? ReturnType { get; set; } = "void";
        
        public override string ToString()
        {
            if (IsLambdaBody)
                return
$$"""
{{AttributeList}}
{{Indent}}{{Modifiers}}{{Async}}{{ReturnType}} {{Name}}{{Types}}({{string.Join(", ", Parameters)}}){{TypeConstraints}}
{{Indent}}  => {{Body.FirstOrDefault()?.ToString().Trim()}}
""";
            else
                return
$$"""
{{AttributeList}}
{{Indent}}{{Modifiers}}{{Async}}{{ReturnType}} {{Name}}{{Types}}({{string.Join(", ", Parameters)}}){{TypeConstraints}}
{{Indent}}{
{{string.Join("\n", Body)}}
{{Indent}}}
""";
        }
    }

    internal class ConstructorBuilder : MethodBase<ConstructorBuilder>
    {
        public ConstructorBuilder()
        {
            Modifiers = "public";
        }
        public override NodeType Type => NodeType.Constructor;
        public override string ToString()
        {
            return
$$"""
{{Indent}}{{Modifiers}} {{Name}}({{string.Join(", ", Parameters)}})
{{Indent}}{
{{string.Join("\n", Body)}}
{{Indent}}}
""";
        }
    }

    internal class Statement
    {
        public string Indent => "            ";

        public string Content { get; }

        public Statement(string content)
        {
            Content = content;
        }
        public static implicit operator Statement(string content) => new Statement(content);

        public override string ToString()
        {
            //if (!Content.Trim().EndsWith(";"))
            //{
            //    return $"{Indent}{Content};";
            //}
            return $"{Indent}{Content}{AttachSemicolon()}";
        }

        private string AttachSemicolon()
        {
            if (Content.Trim().EndsWith(";"))
                return string.Empty;
            if (Content.StartsWith("if"))
                return string.Empty;
            if (Content.StartsWith("{"))
                return string.Empty;
            if (Content.EndsWith("}"))
                return string.Empty;
            return ";";

        }
    }
}
