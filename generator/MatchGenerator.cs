using Microsoft.CodeAnalysis;
using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Text;

namespace SyntaxSearcher.Generators
{
    [Generator]
    public class MatcherGenerator : ISourceGenerator
    {
        private static ImmutableArray<string> IgnoreKinds = ImmutableArray.Create("Token", "Keyword", "Trivia");
        private SyntaxCollector<ClassDeclarationSyntax> _receiver;

        /// <summary>
        /// Map of the first in the chain of SyntaxKinds that use the same concrete type.
        /// <para>
        /// Used when the syntax kind name doesn't map exactly to a syntax class.
        /// </para>
        /// </summary>
        private static readonly ImmutableDictionary<string, string> _firstTypes = new Dictionary<string, string>()
        {
            {nameof(SyntaxKind.AddExpression), nameof(BinaryExpressionSyntax) },
            {nameof(SyntaxKind.AddAssignmentExpression), nameof(AssignmentExpressionSyntax) },
            {nameof(SyntaxKind.UnaryPlusExpression), nameof(PostfixUnaryExpressionSyntax) },
            {nameof(SyntaxKind.NumericLiteralExpression), nameof(LiteralExpressionSyntax) },
            {nameof(SyntaxKind.ClassConstraint), nameof(ClassOrStructConstraintSyntax) },
            {nameof(SyntaxKind.BaseConstructorInitializer), nameof(ConstructorInitializerSyntax) },
            {nameof(SyntaxKind.GetAccessorDeclaration), nameof(AccessorDeclarationSyntax) },
            {nameof(SyntaxKind.OrPattern), nameof(BinaryPatternSyntax) },
            {nameof(SyntaxKind.YieldReturnStatement), nameof(YieldStatementSyntax) },
            {nameof(SyntaxKind.AscendingOrdering), nameof(OrderByClauseSyntax) },
            {nameof(SyntaxKind.GotoCaseStatement), nameof(GotoStatementSyntax) },
            {nameof(SyntaxKind.PointerMemberAccessExpression), nameof(MemberAccessExpressionSyntax) },
            {nameof(SyntaxKind.IndexExpression), nameof(PrefixUnaryExpressionSyntax) },
            {nameof(SyntaxKind.UncheckedExpression), nameof(CheckedExpressionSyntax) }
        }.ToImmutableDictionary();

        public void Execute(GeneratorExecutionContext context)
        {
            var generated = GenerateMatcherClasses(context);

            CompletePartialClasses(context);

            GenerateParser(context, generated);
        }

        public static Dictionary<IFieldSymbol, INamedTypeSymbol> GetKindToClassMap(GeneratorExecutionContext context)
        {
            return GenerateMap(context);
        }

        public static Dictionary<INamedTypeSymbol, IFieldSymbol> GetClassToKindMap(GeneratorExecutionContext context)
        {
            var classToKind = new Dictionary<INamedTypeSymbol, IFieldSymbol>(SymbolEqualityComparer.Default);
            foreach (var kvp in GenerateMap(context))
            {
                classToKind[kvp.Value] = kvp.Key;
            }
            return classToKind;
        }

        /// <summary>
        /// Creates a mapping of <see cref="SyntaxKind"/> enumeration to the class object that
        /// it's associated with.
        /// </summary>
        /// <param name="context">generator context</param>
        public static Dictionary<IFieldSymbol, INamedTypeSymbol> GenerateMap(GeneratorExecutionContext context)
        {
            var kindToClass = new Dictionary<IFieldSymbol, INamedTypeSymbol>(SymbolEqualityComparer.Default);

            var syntaxKinds = context.Compilation.GetTypeByMetadataName(typeof(SyntaxKind).FullName)
                                                             .GetMembers()
                                                             .Where(m => !IgnoreKinds.Any(i => m.Name.Contains(i)))
                                                             .OfType<IFieldSymbol>()
                                                             .Where(f => f.HasConstantValue)
                                                             .Where(f => f.ConstantValue is ushort v && v > 0);

            string namespaceString = string.Join(".", typeof(ClassDeclarationSyntax).FullName.Split('.').Reverse().Skip(1).Reverse());

            INamedTypeSymbol lastType = null;

            foreach (IFieldSymbol kind in syntaxKinds)
            {
                var associatedType = context.Compilation.GetTypeByMetadataName($"{namespaceString}.{kind.Name}Syntax");
                if (associatedType is null && kind.Name.StartsWith("Simple"))
                    associatedType = context.Compilation.GetTypeByMetadataName($"{namespaceString}.{kind.Name.Replace("Simple", "")}Syntax");

                if (associatedType != null)
                {
                    lastType = null;
                }
                else if (associatedType is null && _firstTypes.TryGetValue(kind.Name, out var className))
                {
                    associatedType = context.Compilation.GetTypeByMetadataName($"{namespaceString}.{className}");
                    lastType = associatedType;
                }
                else if (lastType != null)
                {
                    associatedType = lastType;
                }

                kindToClass[kind] = associatedType;
            }

            return kindToClass;
        }

        private void GenerateParser(GeneratorExecutionContext context, List<(string, string)> withClasses)
        {
            StringBuilder builder = new();

            builder.AppendLine(
@"
using Microsoft.CodeAnalysis;
using SyntaxSearch.Matchers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace SyntaxSearch.Parser
{
    public static class SearchFileParser
    {
        private const string _rootTag = ""SyntaxSearchDefinition"";

        public static Searcher Parse(string filename)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(filename);

            return ParseInternal(doc);
        }

        public static Searcher ParseFromString(string xmlText)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlText);

            return ParseInternal(doc);
        }

        private static Searcher ParseInternal(XmlDocument doc)
        {
            var matchRoot = doc.SelectSingleNode(_rootTag).ChildNodes.OfType<XmlElement>().FirstOrDefault();
            if (matchRoot is null)
            {
                throw new ArgumentException(""document does not contain any matcher nodes"");
            }

            var format = matchRoot.Attributes[""Format""]?.Value ?? ""Tree"";

            var match = ParseTree(matchRoot);

            return new Searcher(match);
        }

        private static INodeMatcher ParseTree(XmlNode node, INodeMatcher parent = null)
        {
            INodeMatcher matcher = GetMatcher(node);
        
            if (matcher != null)
            {
                matcher.Store = parent?.Store ?? new CaptureStore();

                foreach (XmlElement child in node.ChildNodes.OfType<XmlElement>())
                {
                    var childMatcher = ParseTree(child, matcher);
                    if (childMatcher is LogicalMatcher logical && parent is LogicalMatcher)
                    {
                        childMatcher.Accepts = NodeAccept.Node;
                    }
                    else
                    {
                        childMatcher.Accepts = NodeAccept.Child;
                    }

                    if (childMatcher != null)
                    {
                        matcher.Children.Add(childMatcher);
                    }
                }
            }
            
            return matcher;
        }

        private static INodeMatcher GetMatcher(XmlNode node)
        {
            if (node is XmlElement element)
            {
                switch (element.Name)
                {");

            foreach ((var tagName, var className) in withClasses)
            {
                builder.AppendLine($"case \"{tagName}\":");
                builder.AppendLine($" return new {className}(element);");
            }

            AddNonSyntaxClasses(context, builder);

            builder.AppendLine("default:");
            builder.AppendLine(" throw new ArgumentException($\"unknown tag: {element.Name}\");");

            builder.AppendLine("}");
            builder.AppendLine("}");
            builder.AppendLine("return null;");
            builder.AppendLine("}");

//            builder.AppendLine(@"
//        private static INodeMatcher ParseExplicit(XmlNode element, INodeMatcher parent = null)
//        {

//");
//            foreach ((var tagName, var className) in withClasses)
//            {
//                builder.AppendLine($"case \"{tagName}\":");
//                builder.AppendLine("// explicit");
//                builder.AppendLine($" return new {className}(element);");
//            }

//            AddNonSyntaxClasses(context, builder);

//            builder.AppendLine("}");

            // class
            builder.AppendLine("}");

            //namespace
            builder.AppendLine("}");

            context.AddSource("SearchParser", Utilities.Normalize(builder));
        }

        private void AddNonSyntaxClasses(GeneratorExecutionContext context, StringBuilder builder)
        {
            foreach (var classDecl in _receiver.Collected)
            {
                var model = context.Compilation.GetSemanticModel(classDecl.SyntaxTree);
                var interfaceType = model.Compilation.GetTypeByMetadataName("SyntaxSearch.Matchers.INodeMatcher");

                var classType = model.GetDeclaredSymbol(classDecl);

                if (!classType.AllInterfaces.Any(intf => SymbolEqualityComparer.Default.Equals(intf, interfaceType)))
                    continue;
                if (classType.IsAbstract || classType.TypeKind != TypeKind.Class)
                    continue;

                string tagName = classType.Name.Replace("Matcher", "");

                builder.AppendLine($"case \"{tagName}\":");
                if (classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                {
                    builder.AppendLine($"return new {classType.Name}(element);");
                }
                else
                {
                    builder.AppendLine($"return new {classType.Name}();");
                }
            }
        }

        private void CompletePartialClasses(GeneratorExecutionContext context)
        {
            foreach (var classDecl in _receiver.Collected)
            {
                var model = context.Compilation.GetSemanticModel(classDecl.SyntaxTree);
                var interfaceType = model.Compilation.GetTypeByMetadataName("SyntaxSearch.Matchers.INodeMatcher");

                var stringType = model.Compilation.GetSpecialType(SpecialType.System_String);

                var classType = model.GetDeclaredSymbol(classDecl);

                if (!classType.AllInterfaces.Any(intf => SymbolEqualityComparer.Default.Equals(intf, interfaceType)))
                    continue;
                if (classType.IsAbstract || classType.TypeKind != TypeKind.Class)
                    continue;

                StringBuilder builder = new();

                foreach (var field in classType.GetMembers().OfType<IFieldSymbol>().Where(f => !f.IsStatic && !f.IsReadOnly && !f.IsAbstract))
                {
                    if (builder.Length == 0)
                    {
                        builder.AppendLine(@$"
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Text;

namespace SyntaxSearch.Matchers
{{
    public partial class {classType.Name}
    {{
        public {classType.Name}(XmlElement element)
        {{
");
                    }

                    if (SymbolEqualityComparer.Default.Equals(field.Type, stringType))
                    {
                        string serializedName = field.Name.Trim('_').Trim().Replace("Kind", "");
                        serializedName = $"{char.ToUpper(serializedName[0])}{serializedName.Substring(1)}";
                        string value = $"{char.ToLower(serializedName[0])}{serializedName.Substring(1)}";

                        builder.AppendLine($"{field.Name} = element?.Attributes[\"{serializedName}\"]?.Value;");
                    }
                    else if (field.Type.TypeKind == TypeKind.Enum)
                    {
                        string serializedName = field.Name.Trim('_').Trim().Replace("Kind", "");
                        serializedName = $"{char.ToUpper(serializedName[0])}{serializedName.Substring(1)}";
                        string value = $"{char.ToLower(serializedName[0])}{serializedName.Substring(1)}";

                        builder.AppendLine($@"
if (element?.Attributes[""{serializedName}""]?.Value is string {value})
{{
    {field.Name} = Enum.GetValues(typeof({field.Type.ToDisplayString()})).Cast<{field.Type.ToDisplayString()}>().First(k => k.ToString() == {value});
}}
");
                    }
                }

                if (builder.Length > 0)
                {
                    builder.AppendLine("}");
                    builder.AppendLine("}");
                    builder.AppendLine("}");
                    context.AddSource($"{classType.Name}.Ctor", Utilities.Normalize(builder));
                }
            }
        }

        private List<(string, string)> GenerateMatcherClasses(GeneratorExecutionContext context)
        {
            var syntaxKinds = context.Compilation.GetTypeByMetadataName(typeof(SyntaxKind).FullName)
                                                             .GetMembers()
                                                             .Where(m => !IgnoreKinds.Any(i => m.Name.Contains(i)))
                                                             .OfType<IFieldSymbol>()
                                                             .Where(f => f.HasConstantValue)
                                                             .Where(f => f.ConstantValue is ushort v && v > 0);

            string namespaceString = string.Join(".", typeof(ClassDeclarationSyntax).FullName.Split('.').Reverse().Skip(1).Reverse());

            List<(string, string)> newTrees = new();

            StringBuilder map = new StringBuilder();

            var syntaxNodeType = context.Compilation.GetTypeByMetadataName(typeof(SyntaxNode).FullName);

            Dictionary<INamedTypeSymbol, string> typeToClassMap = new(SymbolEqualityComparer.Default);
            List<(IFieldSymbol, INamedTypeSymbol)> kinds = new();

            foreach (KeyValuePair<IFieldSymbol, INamedTypeSymbol> entry in GetKindToClassMap(context))
            {
                var kind = entry.Key;
                var associatedType = entry.Value;

                if (associatedType is null)
                {
                    var slim = BuildClassNoOverrides(kind, null);
                    context.AddSource($"{kind.Name}.Matcher", Utilities.Normalize(slim));
                    newTrees.Add((kind.Name, $"{kind.Name}Matcher"));

                    map.AppendLine($"// {kind} -> ");
                }
                else
                {
                    var namedProperties = associatedType.GetMembers().OfType<IPropertySymbol>().Where(f => f.Type.IsSubclassOf(syntaxNodeType)).ToArray();

                    // type, node access, field name
                    List<(string, string, string, string)> fields = new();

                    foreach (var kvp in TreeBuilderGenerator.TokenProperties)
                    {
                        var property = associatedType.GetAllMembers(kvp.Key).OfType<IPropertySymbol>().FirstOrDefault();
                        if (property != null && kvp.Value != null && property.Type.GetAllMembers(kvp.Value).FirstOrDefault() is IPropertySymbol accessType)
                        {
                            fields.Add(("string", kvp.Key, kvp.Value, $"_{char.ToLower(kvp.Key[0])}{kvp.Key.Substring(1)}"));
                        }
                    }

                    string contents;

                    if (fields.Any())
                    {
                        contents = BuildClass(kind, associatedType.Name, fields, namedProperties);
                    }
                    else
                    {
                        contents = BuildClassNoOverrides(kind, associatedType.Name, namedProperties);
                    }

                    context.AddSource($"{kind.Name}.Matcher", Utilities.Normalize(contents));
                    newTrees.Add((kind.Name, $"{kind.Name}Matcher"));
                }
            }

            context.AddSource($"A.KindTypeMap", map.ToString());

            return newTrees;
        }

        private string BuildClassNoOverrides(ISymbol kind, string className = null, IPropertySymbol[] namedProperties = null)
        {
            string contents;

            if (className != null)
            {
                contents = @$"
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;


namespace SyntaxSearch.Matchers
{{
    public class {kind.Name}Matcher : NodeMatcher
    {{

        public {kind.Name}Matcher(XmlElement element) : base(
            SyntaxKind.{kind.Name},
            element?.Attributes[""Name""]?.Value,
            element?.Attributes[""Match""]?.Value)
        {{
        }}

        public {kind.Name}Matcher({className} node) : base(SyntaxKind.{kind.Name}, null, null)
        {{
            if (!node.IsKind(SyntaxKind.{kind.Name}))
            {{
                throw new ArgumentException($""expected {{nameof(node)}} to be of kind {kind.Name}, but got {{node.Kind().ToString()}}"");
            }}
        }}
    }}
}}
";

            }
            else
            {
                contents = @$"
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;


namespace SyntaxSearch.Matchers
{{
    public class {kind.Name}Matcher : NodeMatcher
    {{

        public {kind.Name}Matcher(XmlElement element) : base(
            SyntaxKind.{kind.Name},
            element?.Attributes[""Name""]?.Value,
            element?.Attributes[""Match""]?.Value)
        {{
        }}
    }}
}}
";
            }


            return contents;
        }

        private string BuildClass(ISymbol kind, string className, List<(string, string, string, string)> fields, IPropertySymbol[] namedProperties)
        {
            static string makeComparision((string, string, string, string) field)
            {
                string ifStatement =
@$"
            if (!string.IsNullOrWhiteSpace({field.Item4}) && !obj.{field.Item2}.{field.Item3}.Equals({field.Item4}))
                return false;
";

                return ifStatement;
            }

            string contents = @$"
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace SyntaxSearch.Matchers
{{
    public class {kind.Name}Matcher : NodeMatcher
    {{
{String.Join(Environment.NewLine, fields.Select(i => $"        private {i.Item1} {i.Item4};"))}

        public {kind.Name}Matcher(XmlElement element) : base(
            SyntaxKind.{kind.Name},
            element?.Attributes[""Name""]?.Value,
            element?.Attributes[""Match""]?.Value)
        {{
{String.Join(Environment.NewLine, fields.Select(i => $"            {i.Item4} = element?.Attributes[\"{i.Item2}\"]?.Value;"))}
        }}

        public {kind.Name}Matcher({className} node) : base(SyntaxKind.{kind.Name}, null, null)
        {{
            if (!node.IsKind(SyntaxKind.{kind.Name}))
            {{
                throw new ArgumentException($""expected {{nameof(node)}} to be of kind {kind.Name}, but got {{node.Kind().ToString()}}"");
            }}

{String.Join(Environment.NewLine, fields.Select(i => $"            {i.Item4} = node.{i.Item2}.{i.Item3};"))}            
        }}

        protected override bool IsNodeMatch(SyntaxNode node)
        {{
            if (!base.IsNodeMatch(node))
                return false;

            var obj = ({className})node;

{string.Join(String.Empty, fields.Select(makeComparision))}
            
            return true;
        }}
    }}
}}
";

            return contents;
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            _receiver = new SyntaxCollector<ClassDeclarationSyntax>();
            context.RegisterForSyntaxNotifications(() => _receiver);
        }
    }

    /// <summary>
    /// Collects all nodes of type <typeparamref name="TNode"/> for later
    /// processing in generator
    /// </summary>
    /// <typeparam name="TNode"><see cref="SyntaxNode"/> type to collect</typeparam>
    public class SyntaxCollector<TNode> : ISyntaxReceiver where TNode : SyntaxNode
    {
        /// <summary>
        /// Collected syntax nodes
        /// </summary>
        public List<TNode> Collected { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is TNode nodeType)
            {
                Collected.Add(nodeType);
            }
        }
    }
}