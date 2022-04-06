using Microsoft.CodeAnalysis;
using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using System.Collections.Immutable;
using System.Collections.Generic;

namespace SyntaxSearcher.Generators
{
    [Generator]
    public class TreeBuilderGenerator : ISourceGenerator
    {
        public static ImmutableDictionary<string, string> TypeProperties = new Dictionary<string, string>()
        {
            {"Identifier", "Text" },
            {"Keyword" , "Text" },
            //{"Modifiers", null },
            {"Token", "ValueText" }
        }.ToImmutableDictionary();

        public void Execute(GeneratorExecutionContext context)
        {
            var entryType = context.Compilation.GetTypeByMetadataName(typeof(CSharpSyntaxWalker).FullName);

            if (entryType is null) return;

            var methods = entryType.GetAllMembers().OfType<IMethodSymbol>().Where(m => m.IsVirtual);

            StringBuilder text = new();

            text.AppendLine("using System;");
            text.AppendLine("using System.Linq;");
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

                foreach (var kvp in TypeProperties)
                {
                    var property = parameterType.GetAllMembers(kvp.Key).OfType<IPropertySymbol>().FirstOrDefault();
                    if (property != null && (kvp.Value is null || property.Type.GetAllMembers(kvp.Value).Any()))
                    {
                        if (!started)
                        {
                            text.AppendLine($"public override void {method.Name}({parameterType.Name} node)");
                            text.AppendLine($"{{");

                            started = true;
                        }
                        else
                        {
                            text.AppendLine();
                        }

                        text.AppendLine($"if (_options.{kvp.Key}s)");
                        text.AppendLine($"{{");

                        if (kvp.Value is null)
                        {
                            text.AppendLine($"if (node.{kvp.Key}.Any())");
                            text.AppendLine($"{{");
                            text.AppendLine($"    var {kvp.Key.ToLower()} = Document.CreateAttribute(\"{kvp.Key}\");");
                            text.AppendLine($"    {kvp.Key.ToLower()}.Value = string.Join(\",\", node.{kvp.Key}.Select(m => m.Kind().ToString().Replace(\"Keyword\", string.Empty).ToLower()));");
                            text.AppendLine($"    _current.Attributes.SetNamedItem({kvp.Key.ToLower()});");
                            text.AppendLine($"}}");
                        }
                        else
                        {

                            text.AppendLine($"var {kvp.Key.ToLower()} = Document.CreateAttribute(\"{kvp.Key}\");");
                            text.AppendLine($"{kvp.Key.ToLower()}.Value = node.{kvp.Key}.{kvp.Value};");
                            text.AppendLine($"_current.Attributes.SetNamedItem({kvp.Key.ToLower()});");
                        }
                        text.AppendLine($"}}");
                    }
                }

                if (started)
                {
                    text.AppendLine();
                    text.AppendLine($"base.{method.Name}(node);");
                    text.AppendLine($"}}");
                    text.AppendLine();
                }
            }




            text.AppendLine("    }");
            text.AppendLine("}");

            context.AddSource("TreeBuilder", Utilities.Normalize(text));
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