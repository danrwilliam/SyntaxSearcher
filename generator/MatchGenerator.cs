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
            {nameof(SyntaxKind.UncheckedExpression), nameof(CheckedExpressionSyntax) },
            {nameof(SyntaxKind.ObjectInitializerExpression), nameof(InitializerExpressionSyntax) }
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
                                                             .Where(f => f.HasConstantValue && f.ConstantValue is ushort v && v > 0);

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

        private void GenerateParser(GeneratorExecutionContext context, List<SyntaxClassInfo> withClasses)
        {
            var extraTypes = GetNonSyntaxClasses(context);

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
using System.Collections.Immutable;

namespace SyntaxSearch.Parser
{
    public interface IFileParser
    {
        Searcher Parse(string filename);

        Searcher ParseFromString(string xmlText);
    }

    public class SearchFileParser : IFileParser
    {
        private delegate INodeMatcher ParseNodeDelegate(XmlNode element, ParseNodeDelegate parseDelegate, INodeMatcher parent = null);

        private const string _rootTag = ""SyntaxSearchDefinition"";

        public Searcher Parse(string filename)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(filename);

            return ParseInternal(doc);
        }

        public Searcher ParseFromString(string xmlText)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlText);

            return ParseInternal(doc);
        }

        public Searcher FromBuilder(SyntaxSearch.Builder.XmlTreeBuilder builder)
        {
            return ParseInternal(builder.Document);
        }

        private Searcher ParseInternal(XmlDocument doc)
        {
            var docRoot = doc.SelectSingleNode(_rootTag);
            if (docRoot is null)
            {
                throw new ArgumentException(""Expected root node"");
            }

            var format = docRoot.Attributes[""Format""]?.Value ?? ""Tree"";

            INodeMatcher matcher;

            var matchRoot = docRoot.ChildNodes.OfType<XmlElement>().FirstOrDefault();
            if (matchRoot is null)
            {
                throw new ArgumentException(""empty document"");
            }

            switch (format)
            {
                case ""Tree"":
                    matcher = ParseTree(matchRoot);
                    break;
                case ""Explicit"":
                    matcher = ParseExplicit(matchRoot);
                    break;
                default:
                    throw new ArgumentException(""unsupported Format"");
            }
            
            return new Searcher(matcher);
        }

        protected virtual INodeMatcher GetMatcherHook(XmlNode node)
        {
            return null;
        }
        ");

            builder.AppendLine(@"
        private INodeMatcher ParseTree(XmlNode node, ParseNodeDelegate parseDelegate = null, INodeMatcher parent = null)
        {
            INodeMatcher nodeMatcher = null;
            if (node is XmlElement element)
            {
                string format = element.Attributes[""Children""]?.Value ?? ""Tree"";
                bool childrenHandled = false;

                switch (element.Name)
                {");

            foreach (var type in withClasses)
            {
                builder.AppendLine($@"case ""{type.KindName}"":
                {{
                    string captureName = element.Attributes[""Name""]?.Value;
                    string matchCapture = element.Attributes[""MatchCapture""]?.Value;
                ");

                if (type.Properties.Any() || type.Fields.Any())
                {
                    List<string> args = new();

                    foreach (var f in type.Fields)
                    {
                        builder.AppendLine($"string {f.TokenName} = element.Attributes[\"{f.TokenName}\"]?.Value;");

                        args.Add($"{f.FieldName.TrimStart('_')}: {f.TokenName}");
                    }

                    builder.AppendLine($@"
    if (format == ""Named"")
    {{
        nodeMatcher = ParseExplicit(element, ParseTree, parent);
        childrenHandled = true;
    }}
    else
    {{
        nodeMatcher = new {type.ClassName}({string.Join(", ", [.. args, "captureName: captureName", "matchCapture: matchCapture"])});
    }}");
                }
                else
                {
                    builder.AppendLine($"nodeMatcher = new {type.ClassName}(captureName, matchCapture);"); 
                }

                builder.AppendLine("break; }");
            }

            AddNonSyntaxClasses(context, builder, extraTypes);

            builder.AppendLine(@"
                default:
                    var custom = GetMatcherHook(node);
                    if (custom != null)
                        nodeMatcher = custom;
                    else
                        throw new ArgumentException($""unknown tag: {element.Name}"");
                    break;
                }

                if (nodeMatcher != null && !childrenHandled)
                {
                    nodeMatcher.Store = parent?.Store ?? new CaptureStore();
                    
                    foreach (XmlElement child in node.ChildNodes.OfType<XmlElement>()) 
                    {
                        var childMatcher = ParseTree(child, ParseTree, nodeMatcher);
                        if (childMatcher is LogicalMatcher && nodeMatcher is LogicalMatcher)
                        {
                            childMatcher.Accepts = NodeAccept.Node;
                        }
                        else
                        {
                            childMatcher.Accepts = NodeAccept.Child;
                        }

                        if (childMatcher != null)
                        {
                            nodeMatcher.Children.Add(childMatcher);
                        }
                    }
                }
            }

                return nodeMatcher;
            }
");


            builder.AppendLine(@"
        private static INodeMatcher ParseExplicit(XmlNode node, ParseNodeDelegate parseDelegate = null, INodeMatcher parent = null)
        {
            if (node is XmlElement element)
            {
                parseDelegate ??= ParseExplicit;

                switch (element.Name)
                {
        ");

            foreach (var type in withClasses)
            {
                builder.AppendLine(@$"case ""{type.KindName}"":");
                if (type.Properties.Any())
                {
                    builder.AppendLine($@"{{
                    string captureName = element.Attributes[""Name""]?.Value;
                    string matchCapture = element.Attributes[""MatchCapture""]?.Value;
                    var matcher = new SyntaxSearch.Matchers.Explicit.{type.ClassName}(captureName: captureName, matchCapture: matchCapture);
                    matcher.Store = parent?.Store ?? new CaptureStore();

                    foreach (var child in element.ChildNodes.OfType<XmlNode>()) {{");

                    builder.AppendLine("switch (child.Name) {");

                    foreach ((IPropertySymbol property, bool isList) in type.Properties)
                    {
                        builder.AppendLine($"case \"{property.Name}\":");
                        if (isList)
                        {
                            builder.AppendLine($@"
                            foreach (var listElement in child.OfType<XmlNode>()) {{
                                var listElementMatcher = parseDelegate(listElement, parseDelegate, matcher);
                                if (listElementMatcher is null)
                                    throw new ArgumentException($""unable to build matcher for {{listElement.Name}}"");
                                listElementMatcher.Store = matcher.Store;
                                matcher.Add{property.Name}(listElementMatcher);
                            }}
");
                        }
                        else
                        {
                            builder.AppendLine($@"{{
                                if (matcher.{property.Name} != null)
                                    throw new ArgumentException($""expected only 1 matcher for {property.Name}"");
                                
                                var childNode = child.ChildNodes.OfType<XmlElement>().FirstOrDefault(); 
                                INodeMatcher childMatcher = null;

                                if (childNode is null)
                                {{
                                    childMatcher = new NotNullMatcher();
                                }}
                                else
                                {{
                                    childMatcher = parseDelegate(childNode, parseDelegate, matcher);
                                }}

                                if (childMatcher is null)
                                    throw new ArgumentException($""unable to build matcher for {{child.Name}}"");
                                matcher.{property.Name} = childMatcher; 
                                matcher.{property.Name}.Store = matcher.Store;
}}");
                        }
                        builder.AppendLine("break;");
                    }


                    builder.AppendLine("default:");
                    builder.AppendLine("throw new InvalidOperationException($\"{element.Name} does not support a child of name {child.Name}\");");

                    builder.AppendLine("}");
                    builder.AppendLine("}");

                    builder.AppendLine("return matcher;");
                    builder.AppendLine("}");
                }
                else
                {
                    builder.AppendLine($@"{{
                    string captureName = element.Attributes[""Name""]?.Value;
                    string matchCapture = element.Attributes[""MatchCapture""]?.Value;
                    ");

                    List<string> args = [];

                    foreach (var f in type.Fields)
                    {
                        if (f.TypeName == "string[]")
                        {
                        }
                        else
                        {
                            builder.AppendLine($"string {f.TokenName} = element?.Attributes[\"{f.TokenName}\"]?.Value;");

                            args.Add($"{f.FieldName.Trim('_')}: {f.TokenName}");
                        }
                    }

                    builder.AppendLine($"var matcher = new {type.ClassName}({string.Join(", ", [..args, "captureName: captureName", "matchCapture: matchCapture"])});");
                    builder.AppendLine("matcher.Store = parent?.Store ?? new CaptureStore();");
                    builder.AppendLine("return matcher;");
                    builder.AppendLine("}");
                }
            }

            AddNonSyntaxClasses(context, builder, extraTypes, true);

            builder.AppendLine("}");
            builder.AppendLine("}");
            builder.AppendLine("return null;");
            builder.AppendLine("}");

            // class
            builder.AppendLine("}");

            //namespace
            builder.AppendLine("}");

            context.AddSource("SearchParser", Utilities.Normalize(builder));
        }

        private List<(INamedTypeSymbol, ClassDeclarationSyntax, string)> GetNonSyntaxClasses(GeneratorExecutionContext context)
        {
            List<(INamedTypeSymbol, ClassDeclarationSyntax, string)> types = [];

            foreach (var classDecl in _receiver.Collected)
            {
                var model = context.Compilation.GetSemanticModel(classDecl.SyntaxTree);
                var interfaceType = model.Compilation.GetTypeByMetadataName("SyntaxSearch.Matchers.INodeMatcher");

                var classType = model.GetDeclaredSymbol(classDecl);

                if (!classType.AllInterfaces.Any(intf => SymbolEqualityComparer.Default.Equals(intf, interfaceType)))
                    continue;
                if (classType.IsAbstract || classType.TypeKind != TypeKind.Class)
                    continue;

                types.Add((classType, classDecl, classType.Name.Replace("Matcher", "")));
            }

            return types;
        }

        private void AddNonSyntaxClasses(GeneratorExecutionContext context, StringBuilder builder,
            List<(INamedTypeSymbol, ClassDeclarationSyntax, string)> extraTypes, bool iterate = false)
        {
            foreach ((var classType, var classDecl, var tagName) in extraTypes)
            {
                builder.AppendLine($"case \"{tagName}\":");
                bool takesArg = classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
                {
                    if (iterate)
                    {
                        builder.AppendLine(@$"
                            {{
                                var matcher = new {classType.Name}({(takesArg ? "element" : "")});
                                matcher.Store = parent?.Store ?? new CaptureStore();
                                
                                foreach (var c in element.ChildNodes.OfType<XmlElement>())
                                {{
                                    var childMatcher = parseDelegate(c, parseDelegate, matcher);
                                    if (childMatcher != null)
                                        matcher.Children.Add(childMatcher);
                                }}
                                return matcher;
                            }}");
                    }
                    else
                    {
                        builder.AppendLine($"nodeMatcher = new {classType.Name}({(takesArg ? "element" : "")});");
                        builder.AppendLine("break;");
                    }
                }
            }
        }

        private void CompletePartialClasses(GeneratorExecutionContext context)
        {
            var baseMatcher = context.Compilation.GetTypeByMetadataName("SyntaxSearch.Matchers.BaseMatcher");

            bool isOverrideWithProperties(INamedTypeSymbol type, INamedTypeSymbol stringType)
            {
                var currentType = type.BaseType;

                while (!SymbolEqualityComparer.Default.Equals(baseMatcher, currentType))
                {
                    foreach (var f in currentType.GetMembers().OfType<IFieldSymbol>().Where(f => !f.IsStatic && !f.IsReadOnly && !f.IsAbstract))
                    {
                        if (SymbolEqualityComparer.Default.Equals(f.Type, stringType))
                            return true;
                        else if (f.Type.TypeKind == TypeKind.Enum)
                            return true;
                    }

                    currentType = currentType.BaseType;
                }

                return false;
            }

            foreach (var classDecl in _receiver.Collected)
            {
                var model = context.Compilation.GetSemanticModel(classDecl.SyntaxTree);
                var interfaceType = model.Compilation.GetTypeByMetadataName("SyntaxSearch.Matchers.INodeMatcher");

                var stringType = model.Compilation.GetSpecialType(SpecialType.System_String);
                var boolType = model.Compilation.GetSpecialType(SpecialType.System_Boolean);

                var classType = model.GetDeclaredSymbol(classDecl);

                if (!classType.AllInterfaces.Any(intf => SymbolEqualityComparer.Default.Equals(intf, interfaceType)))
                    continue;
                if (classType.IsAbstract || classType.TypeKind != TypeKind.Class)
                    continue;

                bool isOverride = isOverrideWithProperties(classType, stringType);

                StringBuilder builder = new();

                foreach (var field in classType.GetMembers()
                                               .OfType<IFieldSymbol>()
                                               .Where(f => !f.IsStatic && !f.IsReadOnly && !f.IsAbstract && (f.DeclaredAccessibility == Accessibility.Protected || f.DeclaredAccessibility == Accessibility.Private)))
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
using System.Collections.Immutable;

namespace SyntaxSearch.Matchers
{{
    public partial class {classType.Name}
    {{
        public {classType.Name}(XmlElement element){(isOverride ? " : base(element)" : "")}
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
                    else if (SymbolEqualityComparer.Default.Equals(field.Type, boolType))
                    {
                        string serializedName = field.Name.Trim('_').Trim().Replace("Kind", "");
                        serializedName = $"{char.ToUpper(serializedName[0])}{serializedName.Substring(1)}";
                        string value = $"{char.ToLower(serializedName[0])}{serializedName.Substring(1)}";

                        builder.AppendLine(@$"if (element?.Attributes[""{serializedName}""] != null) 
{{
    {field.Name} = bool.Parse(element.Attributes[""{serializedName}""].Value);
}}
");
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

                if (builder.Length == 0 && isOverride)
                {
                    builder.AppendLine(@$"
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Text;
using System.Collections.Immutable;

namespace SyntaxSearch.Matchers
{{
    public partial class {classType.Name}
    {{
        public {classType.Name}(XmlElement element){(isOverride ? " : base(element)" : "")}
        {{
");
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

        private List<SyntaxClassInfo> GenerateMatcherClasses(GeneratorExecutionContext context)
        {
            var syntaxKinds = context.Compilation.GetTypeByMetadataName(typeof(SyntaxKind).FullName)
                                                             .GetMembers()
                                                             .Where(m => !IgnoreKinds.Any(i => m.Name.Contains(i)))
                                                             .OfType<IFieldSymbol>()
                                                             .Where(f => f.HasConstantValue && f.ConstantValue is ushort v && v > 0);

            string namespaceString = string.Join(".", typeof(ClassDeclarationSyntax).FullName.Split('.').Reverse().Skip(1).Reverse());

            List<SyntaxClassInfo> newTrees = [];

            StringBuilder map = new();

            var syntaxNodeType = context.Compilation.GetTypeByMetadataName(typeof(SyntaxNode).FullName);
            var syntaxTokenList = context.Compilation.GetTypeByMetadataName(typeof(SyntaxTokenList).FullName);

            Dictionary<INamedTypeSymbol, string> typeToClassMap = new(SymbolEqualityComparer.Default);
            List<(IFieldSymbol, INamedTypeSymbol)> kinds = [];

            foreach (var entry in GetKindToClassMap(context))
            {
                var kind = entry.Key;
                var associatedType = entry.Value;

                var info = new SyntaxClassInfo
                {
                    KindName = kind.Name,
                    ClassName = $"{kind.Name}Matcher"
                };

                if (associatedType is null)
                {
                    var slim = BuildClassNoOverrides(kind, null);
                    string classTypeName = $"{kind.Name}.Matcher";

                    context.AddSource(classTypeName, Utilities.Normalize(slim));

                    newTrees.Add(info);
                }
                else
                {
                    var namedProperties = Helpers.GetNamedProperties(associatedType, syntaxNodeType);
                    // type, node access, field name
                    List<MatchField> fields = [];

                    foreach (var kvp in TreeBuilderGenerator.TokenProperties)
                    {
                        var property = associatedType.GetAllMembers(kvp.Key).OfType<IPropertySymbol>().FirstOrDefault();
                        if (property != null && property.Type.GetAllMembers(kvp.Value).FirstOrDefault() is IPropertySymbol)
                        {
                            if (SymbolEqualityComparer.Default.Equals(property.Type, syntaxTokenList))
                            {
                                fields.Add(("string[]", kvp.Key, "Value", GenerateFieldName(kvp.Key)));

                            }
                            else
                            {
                                fields.Add(("string", kvp.Key, kvp.Value, GenerateFieldName(kvp.Key)));
                            }
                        }
                    }

                    string contents;

                    info.Properties = [.. namedProperties];
                    info.Fields = [.. fields];
                    newTrees.Add(info);

                    if (fields.Any())
                    {
                        contents = BuildClass(kind, associatedType.Name, fields, namedProperties);
                    }
                    else
                    {
                        contents = BuildClassNoOverrides(kind, associatedType.Name, namedProperties);
                    }

                    context.AddSource($"{kind.Name}.Matcher", Utilities.Normalize(contents));

                }
            }

            return newTrees;
        }

        private static string GenerateFieldName(string kvp)
        {
            return $"_{char.ToLower(kvp[0])}{kvp.Substring(1)}";
        }

        private string BuildClassNoOverrides(ISymbol kind, string className = null, MatchProperty[] namedProperties = null)
        {
            StringBuilder builder = new();

            if (className != null)
            {
                builder.AppendLine(@$"
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Collections.Immutable;


namespace SyntaxSearch.Matchers
{{
    public class {kind.Name}Matcher : NodeMatcher
    {{

        public {kind.Name}Matcher(string captureName = null, string matchCapture = null) : base(
            SyntaxKind.{kind.Name},
            captureName,
            matchCapture)
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
");

                if (namedProperties.Any())
                {
                    AppendExplicitClassWithFields(kind, className, [], namedProperties, builder);
                }
            }
            else
            {
                builder.AppendLine(@$"
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Collections.Immutable;

namespace SyntaxSearch.Matchers
{{
    public class {kind.Name}Matcher : NodeMatcher
    {{

        public {kind.Name}Matcher(string captureName = null, string matchCapture = null) : base(
            SyntaxKind.{kind.Name},
            captureName,
            matchCapture)
        {{
        }}
    }}
}}
");
            }


            return builder.ToString();
        }

        private static void AppendExplicitClassWithFields(ISymbol kind,
                                                          string className,
                                                          List<MatchField> fields,
                                                          MatchProperty[] namedProperties,
                                                          StringBuilder builder)
        {
            builder.AppendLine("namespace SyntaxSearch.Matchers.Explicit {");

            builder.AppendLine($"public class {kind.Name}Matcher : ExplicitNodeMatcher {{");

            foreach (var i in fields)
            {
                builder.AppendLine($"private {i.TypeName} {i.FieldName};");
            }

            StringBuilder builderMethods = new();

            foreach ((var namedProp, bool isList) in namedProperties)
            {
                if (isList)
                {
                    builder.AppendLine($"public ImmutableArray<INodeMatcher> {namedProp.Name} {{ get;  internal set; }}");

                    builderMethods.AppendLine($@"internal void Add{namedProp.Name}(INodeMatcher matcher)
                    {{
                        if (matcher is null)
                        {{
                            throw new ArgumentNullException(nameof(matcher));
                        }}
                        {namedProp.Name} = {namedProp.Name}.Add(matcher);
                    }}");


                    builderMethods.AppendLine($@"public {kind.Name}Matcher With{namedProp.Name}(IEnumerable<INodeMatcher> matcher)
                    {{
                        if (matcher is null)
                        {{
                            throw new ArgumentNullException(nameof(matcher));
                        }}
                        
                        var copy = new {kind.Name}Matcher(this)
                        {{
                            {namedProp.Name} = [.. matcher]
                        }};
                        return copy;
                    }}");
                }
                else
                {
                    builder.AppendLine($"public INodeMatcher {namedProp.Name} {{ get; internal set; }}");

                    builderMethods.AppendLine($@"
                    public {kind.Name}Matcher With{namedProp.Name}(INodeMatcher matcher) 
                    {{ 
                        return new {kind.Name}Matcher(this)
                        {{
                            {namedProp.Name} = matcher
                        }};
                    }}");
                }
            }

            string constructorArgs = string.Join(
    ", ",
    [.. fields.Select(i => $"{i.TypeName} {i.FieldName.TrimStart('_')} = default"), "string captureName = null, string matchCapture = null"]);


            builder.AppendLine($@"
        public {kind.Name}Matcher(
            {constructorArgs}) : base(
            SyntaxKind.{kind.Name},
            captureName,
            matchCapture)
        {{");

            foreach (var i in fields)
            {
                builder.AppendLine($"{i.FieldName} = {i.FieldName.TrimStart('_')};");
            }

            builder.AppendLine("}");

            builder.AppendLine($@"
            /// <summary>
            /// Copy constructor
            /// </summary>
            protected {kind.Name}Matcher({kind.Name}Matcher copy) : base(copy)
            {{");
            foreach (var f in fields)
            {
            }
            foreach ((var namedProp, _) in namedProperties)
            {
                builder.AppendLine($"this.{namedProp.Name} = copy.{namedProp.Name};");
            }

            builder.AppendLine("}");

            builder.AppendLine(builderMethods.ToString());

            builder.AppendLine($@"
                protected override bool IsNodeMatch(SyntaxNode node) {{
                    if (!base.IsNodeMatch(node))
                        return false;

            var obj = ({className})node;");

            foreach (var field in fields)
            {
                builder.AppendLine(@$"
            if (!string.IsNullOrWhiteSpace({field.FieldName}) && !obj.{field.TokenName}.{field.TokenValue}.Equals({field.FieldName}))
                return false;");
            }

            builder.AppendLine("return true;}");

            builder.AppendLine(@$"
                protected override bool DoChildNodesMatch(SyntaxNode node)
                {{
                    var castNode = ({className})node;
");

            foreach ((var namedProp, bool isList) in namedProperties)
            {
                string localName = $"{namedProp.Name.ToLower()}Node";

                if (isList)
                {
                    builder.AppendLine(@$"
    if (!{namedProp.Name}.IsEmpty)
    {{
        var {localName} = castNode.{namedProp.Name};
        if ({localName} == default)
            return false;

        if ({localName}.Count != {namedProp.Name}.Length)
            return false;

        for (int i = 0; i < {namedProp.Name}.Length; i++)
        {{
            if (!{namedProp.Name}[i].IsMatch({localName}[i]))
                return false;
        }}
    }}
");

                }
                else
                {
                    builder.AppendLine(@$"
    if ({namedProp.Name} != null)
    {{
        var {localName} = castNode.{namedProp.Name};
        if ({localName} == default)
            return false;

        if (!{namedProp.Name}.IsMatch({localName}))
            return false;
    }}
");
                }
            }

            builder.AppendLine("return true;");
            builder.AppendLine("}");

            builder.AppendLine(@"
    }
}
");
        }

        private string BuildClass(ISymbol kind, string className, List<MatchField> fields, MatchProperty[] namedProperties)
        {
            StringBuilder b = new();
            b.AppendLine();
            string newLine = b.ToString();

            string constructorArgs = string.Join(
                ", ",
                [.. fields.Select(i => $"{i.TypeName} {i.FieldName.TrimStart('_')} = default"), "string captureName = null, string matchCapture = null"]);

            StringBuilder builder = new(@$"
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Collections.Immutable;

namespace SyntaxSearch.Matchers
{{
    public class {kind.Name}Matcher : NodeMatcher
    {{
{string.Join(newLine, fields.Select(i => $"        private {i.TypeName} {i.FieldName};"))}

        public {kind.Name}Matcher({constructorArgs}) : base(
            SyntaxKind.{kind.Name},
            captureName,
            matchCapture)
        {{
");

            foreach (var i in fields)
            {
                builder.AppendLine($"{i.FieldName} = {i.FieldName.TrimStart('_')};");
            }

            builder.AppendLine($@"
        }}

        public {kind.Name}Matcher({className} node) : base(SyntaxKind.{kind.Name}, null, null)
        {{
            if (!node.IsKind(SyntaxKind.{kind.Name}))
            {{
                throw new ArgumentException($""expected {{nameof(node)}} to be of kind {kind.Name}, but got {{node.Kind().ToString()}}"");
            }}

{string.Join(newLine, fields.Select(i => $"            {i.FieldName} = node.{i.TokenName}.{i.TokenValue};"))}            
        }}

        protected override bool IsNodeMatch(SyntaxNode node)
        {{
            if (!base.IsNodeMatch(node))
                return false;

            var obj = ({className})node;
");

            foreach (var f in fields)
            {
                if (f.TypeName == "string")
                {
                    builder.AppendLine(MakeComparision(f));
                }
            }

            builder.AppendLine(@"
            return true;
        }
    }
}
");

            if (namedProperties.Any())
            {
                AppendExplicitClassWithFields(kind, className, fields, namedProperties, builder);
            }

            return Utilities.Normalize(builder);
        }

        private static string MakeComparision(MatchField field)
        {
            return @$"
            if (!string.IsNullOrWhiteSpace({field.FieldName}) && !obj.{field.TokenName}.{field.TokenValue}.Equals({field.FieldName}))
                return false;";
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            _receiver = new SyntaxCollector<ClassDeclarationSyntax>();
            context.RegisterForSyntaxNotifications(() => _receiver);
        }
    }

    internal class SyntaxClassInfo
    {
        public string KindName { get; set; }
        public string ClassName { get; set; }

        public SyntaxKind Kind { get; set;  }

        public IReadOnlyList<MatchProperty> Properties { get; internal set; } = [];

        public IReadOnlyList<MatchField> Fields { get; internal set; } = [];

        public SyntaxClassInfo(string kind, string className)
        {
            KindName = kind;
            ClassName = className;
        }

        public SyntaxClassInfo()
        {
        }
    }

    internal record struct MatchProperty(IPropertySymbol Property, bool IsList)
    {
        public static implicit operator (IPropertySymbol, bool)(MatchProperty value)
        {
            return (value.Property, value.IsList);
        }

        public static implicit operator MatchProperty((IPropertySymbol, bool) value)
        {
            return new MatchProperty(value.Item1, value.Item2);
        }
    }

    internal record struct MatchField(string TypeName, string TokenName, string TokenValue, string FieldName)
    {
        public static implicit operator (string, string, string, string)(MatchField value)
        {
            return (value.TypeName, value.TokenName, value.TokenValue, value.FieldName);
        }

        public static implicit operator MatchField((string, string, string, string) value)
        {
            return new MatchField(value.Item1, value.Item2, value.Item3, value.Item4);
        }
    }
}