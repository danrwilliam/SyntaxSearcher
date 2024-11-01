﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using System.Text;
using System.Collections.Immutable;
using System.Collections.Generic;

namespace SyntaxSearcher.Generators
{
    [Generator]
    public class TreeBuilderGenerator : ISourceGenerator
    {
        public static ImmutableDictionary<string, string> TokenProperties = new Dictionary<string, string>()
        {
            {"Identifier", "Text" },
            {"Keyword" , "Text" },
            {"Token", "ValueText" }
        }.ToImmutableDictionary();

        private static readonly ImmutableHashSet<string> _supportAutoCompare = ImmutableArray.Create(
            nameof(IdentifierNameSyntax),
            nameof(MemberAccessExpressionSyntax),
            nameof(GenericNameSyntax)
        ).ToImmutableHashSet();

        public void Execute(GeneratorExecutionContext context)
        {
            var entryType = context.Compilation.GetTypeByMetadataName(typeof(CSharpSyntaxWalker).FullName);

            if (entryType is null) return;

            var methods = entryType.GetAllMembers().OfType<IMethodSymbol>().Where(m => m.IsVirtual).ToArray();

            var syntaxNodeType = context.Compilation.GetTypeByMetadataName(typeof(SyntaxNode).FullName);
            var syntaxTokenType = context.Compilation.GetTypeByMetadataName(typeof(SyntaxToken).FullName);

            StringBuilder text = new();

            text.AppendLine("using System;");
            text.AppendLine("using System.Linq;");
            text.AppendLine("using System.Collections.Generic;");
            text.AppendLine("using System.Xml;");
            text.AppendLine("using Microsoft.CodeAnalysis;");
            text.AppendLine("using Microsoft.CodeAnalysis.CSharp;");
            text.AppendLine("using Microsoft.CodeAnalysis.CSharp.Syntax;");
            text.AppendLine();
            text.AppendLine("namespace SyntaxSearch.Builder");
            text.AppendLine("{");
            text.AppendLine("    internal partial class TreeWalker");
            text.AppendLine("    {");

            foreach (var method in methods)
            {
                if (!method.Parameters.Any())
                    continue;

                var parameterType = method.Parameters[0].Type;
                bool started = false;
                var namedProperties = Helpers.GetNamedProperties(parameterType, syntaxNodeType, syntaxTokenType);

                static void createHeaderIfNeeded(StringBuilder builder, IMethodSymbol m, ITypeSymbol type, ref bool start)
                {
                    if (!start)
                    {
                        builder.AppendLine($"public override void {m.Name}({type.Name} node)");
                        builder.AppendLine("{");

                        start = true;
                    }
                    else
                    {
                        builder.AppendLine();
                    }
                }

                if (namedProperties.Any())
                {
                    createHeaderIfNeeded(text, method, parameterType, ref started);


                    text.AppendLine(@"
if (_options.NamedChildren)
{
");

                    foreach ((var property, var isList) in namedProperties)
                    {
                        if (isList)
                        {
                            text.AppendLine($@"

if (node.{property.Name} != default)
{{
    var parent = _current;
    var n = Document.CreateElement(""{property.Name}"");
    parent.AppendChild(n);

    _current = n;

    foreach (var listNode in node.{property.Name})
    {{
        Visit(listNode);
    }}

    _current = parent;
}}
");
                        }
                        else
                        {
                            bool isToken = property.Type.Name == nameof(SyntaxToken);
                            string visit = isToken ? "" : $"Visit(node.{property.Name});";

                            text.AppendLine($@"

if (node.{property.Name} != null)
{{
    var parent = _current;
    var n = Document.CreateElement(""{property.Name}"");
    parent.AppendChild(n);

    _current = n;

    {visit}

    _current = parent;
}}
");
                        }
                    }

                    text.AppendLine(@"
    return;
}");
                }

                if (_supportAutoCompare.Contains(parameterType.Name))
                {
                    createHeaderIfNeeded(text, method, parameterType, ref started);
                    text.AppendLine(@"
bool handled = false;

if (_options.AutomaticCapture)
{
    if (TryGetCaptured(node, out string captureKey))
    {
        var element = Document.CreateElement(""MatchCapture"");
        var name = Document.CreateAttribute(""Name"");
        name.Value = captureKey;
        element.Attributes.SetNamedItem(name);

        var parent = _current.ParentNode;

        parent.RemoveChild(_current);
        parent.AppendChild(element);

        handled = true;

        return;
    }
    else
    {
        _staged.Add((node, _current));
    }
}

if (!handled) 
{
");
                }

                foreach (var kvp in TokenProperties)
                {
                    var property = parameterType.GetAllMembers(kvp.Key).OfType<IPropertySymbol>().FirstOrDefault();
                    if (property != null && (kvp.Value is null || property.Type.GetAllMembers(kvp.Value).Any()))
                    {
                        createHeaderIfNeeded(text, method, parameterType, ref started);

                        text.AppendLine($"if (_options.{kvp.Key}s)");
                        text.AppendLine("{");

                        if (kvp.Value is null)
                        {
                            text.AppendLine($"if (node.{kvp.Key}.Any())");
                            text.AppendLine("{");
                            text.AppendLine($"    var {kvp.Key.ToLower()} = Document.CreateAttribute(\"{kvp.Key}\");");
                            text.AppendLine($"    {kvp.Key.ToLower()}.Value = string.Join(\",\", node.{kvp.Key}.Select(m => m.Kind().ToString().Replace(\"Keyword\", string.Empty).ToLower()));");
                            text.AppendLine($"    _current.Attributes.SetNamedItem({kvp.Key.ToLower()});");
                            text.AppendLine("}");
                        }
                        else
                        {

                            text.AppendLine($"var {kvp.Key.ToLower()} = Document.CreateAttribute(\"{kvp.Key}\");");
                            text.AppendLine($"{kvp.Key.ToLower()}.Value = node.{kvp.Key}.{kvp.Value};");
                            text.AppendLine($"_current.Attributes.SetNamedItem({kvp.Key.ToLower()});");
                        }
                        text.AppendLine("}");
                    }
                }

                if (_supportAutoCompare.Contains(parameterType.Name))
                {
                    text.AppendLine("}");
                }


                if (started)
                {
                    text.AppendLine();
                    text.AppendLine($"base.{method.Name}(node);");
                    text.AppendLine("}");
                    text.AppendLine();
                }
            }




            text.AppendLine("    }");
            text.AppendLine("}");

            context.AddSource("TreeBuilder.g.cs", Utilities.Normalize(text));
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }
    }

    public static class Extensions
    {
        public static IEnumerable<ISymbol> GetAllMembers(this ITypeSymbol type)
        {
            if (type is null)
                yield break;

            foreach (var member in type.GetMembers())
            {
                yield return member;
            }

            foreach (var member in type.BaseType.GetAllMembers())
            {
                yield return member;
            }
        }

        public static IEnumerable<ISymbol> GetAllMembers(this ITypeSymbol type, string name)
        {
            if (type is null)
                yield break;

            foreach (var member in type.GetMembers(name))
            {
                yield return member;
            }

            foreach (var member in type.BaseType.GetAllMembers(name))
            {
                yield return member;
            }
        }
    }
}