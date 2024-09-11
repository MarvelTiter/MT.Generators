using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Generators.Shared.Builder;

internal class MethodBuilder : MethodBase<MethodBuilder>
{
    public MethodBuilder()
    {
        Modifiers = "public";
    }
    public override NodeType Type => NodeType.Method;
    public bool IsLambdaBody { get; set; }
    public bool IsAsync { get; set; }
    string Async => IsAsync ? " async " : " ";
    public string? ReturnType { get; set; } = "void";
    public string ConstructedMethodName => $"{Name}{Types}";
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

internal class Statement : Node
{
    //public override string Indent => "            ";

    public string Content { get; private set; } = string.Empty;

    public override NodeType Type => NodeType.Statement;


    public static implicit operator Statement(string content) => new Statement() { Content = content };

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

internal class SwitchStatement : Statement
{
    public SwitchStatement(string switchValue)
    {
        SwitchValue = switchValue;
    }
    public SwitchStatement()
    {

    }
    //public static SwitchStatement Default(Node parent) => new SwitchStatement() { Parent = parent };
    public static SwitchStatement Default => new SwitchStatement();

    public string? SwitchValue { get; set; }
    public List<SwitchCaseStatement> SwitchCases { get; set; } = [];
    public DefaultCaseStatement? DefaultCase { get; set; }
    public override string ToString()
    {
        return
$$"""
{{Indent}}switch ({{SwitchValue}})
{{Indent}}{
{{string.Join("\n", SwitchCases)}}
{{DefaultCase}}
{{Indent}}}
""";
    }
}

internal class SwitchCaseStatement : Statement
{
    public string? Condition { get; set; }
    public string? Action { get; set; }
    public bool IsBreak { get; set; }
    public override string ToString()
    {
        if (IsBreak)
        {
            return
$"""
{Indent}    case {Condition}:
{Indent}        {Action};
{Indent}        break;
""";
        }
        else
        {
            return
$"""
{Indent}    case {Condition}:
{Indent}        {Action};
""";
        }
    }
}

internal class DefaultCaseStatement : Statement
{
    public string? Condition { get; set; }
    public string? Action { get; set; }
    public override string ToString()
    {
        return
$"""
{Indent}    default:
{Indent}        {Action};
""";
    }
    public static implicit operator DefaultCaseStatement(string action) => new DefaultCaseStatement { Action = action };
}

internal class IfStatement : Statement
{
    //internal static IfStatement Default(Node parent) => new IfStatement() { Parent = parent };
    internal static IfStatement Default => new IfStatement();
    public string? Condition { get; set; }
    public List<Statement> IfContents { get; set; } = [];
    public override string ToString()
    {
        return
$$"""
{{Indent}}if ({{Condition}})
{{Indent}}{
{{string.Join("\n", IfContents)}}
{{Indent}}}
""";
    }
}

internal class LocalFunction : Statement
{
    //public static LocalFunction Default(Node parent) => new LocalFunction() { Parent = parent };
    public static LocalFunction Default => new LocalFunction() ;
    public string? ReturnType { get; set; }
    public string? Name { get; set; }
    public bool IsAsync { get; set; }
    string Async => IsAsync ? "async " : "";
    public List<string> Parameters { get; set; } = [];
    public List<Statement> Body { get; set; } = [];
    public override string ToString()
    {
        return
$$"""
{{Indent}}{{Async}}{{ReturnType}} {{Name}}({{string.Join(", ", Parameters)}})
{{Indent}}{
{{string.Join("\n", Body)}}
{{Indent}}}
""";
    }
}
