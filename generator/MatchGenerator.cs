using Microsoft.CodeAnalysis;
using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace SyntaxSearcher.Generators
{
    [Generator]
    public class MatcherGenerator : ISourceGenerator
    {
        private static ImmutableArray<string> IgnoreKinds = ImmutableArray.Create("Keyword", "Trivia");
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
            {nameof(SyntaxKind.ObjectInitializerExpression), nameof(InitializerExpressionSyntax) },
            {nameof(SyntaxKind.ComplexElementInitializerExpression), nameof(InitializerExpressionSyntax) },
            {nameof(SyntaxKind.WithInitializerExpression), nameof(InitializerExpressionSyntax) },
        }.ToImmutableDictionary();

        public void Execute(GeneratorExecutionContext context)
        {
            var generated = GenerateMatcherClasses(context);

            {
                var injected = context.Compilation.AddSyntaxTrees(
                    generated.Select(g => SyntaxFactory.ParseSyntaxTree(SourceText.From(g.Item2), context.ParseOptions, $"{g.Item1.ClassName}.g.cs")));
                CompletePartialClasses(context, injected);
            }

            CreateIsTokenMethods(context);

            GenerateParser(context, [.. generated.Select(g => g.Item1)]);
        }

        private void CreateIsTokenMethods(GeneratorExecutionContext context)
        {
            StringBuilder b = new();
            b.AppendLine($@"
using SyntaxSearch.Matchers;
using Microsoft.CodeAnalysis.CSharp;

namespace SyntaxSearch.Framework
{{
    public static partial class Is
    {{
");

            var syntaxKind = context.Compilation.GetTypeByMetadataName(typeof(SyntaxKind).FullName);

            foreach (var kind in syntaxKind.GetMembers().OfType<IFieldSymbol>().Where(s => s.Name.EndsWith("Token")))
            {
                if (kind.Name == nameof(SyntaxKind.NumericLiteralToken))
                {
                    SpecialType[] types =
                    [
                        SpecialType.System_Double,
                        SpecialType.System_Single,
                        SpecialType.System_UInt16,
                        SpecialType.System_UInt32,
                        SpecialType.System_UInt64,
                        SpecialType.System_Int16,
                        SpecialType.System_Int32,
                        SpecialType.System_Int64,
                    ];

                    b.AppendLine($"public static TokenMatcher {kind.Name} => TokenMatcher.Default.WithKind(SyntaxKind.{kind.Name});");
                    foreach (var numeric in types)
                    {
                        var t = context.Compilation.GetSpecialType(numeric);
                        b.AppendLine($"public static TokenMatcher Number({t.ToDisplayString()} value) => Is.{kind.Name}.WithValue(value);");
                    }
                }
                else if (kind.Name == nameof(SyntaxKind.StringLiteralToken))
                {
                    b.AppendLine($"public static TokenMatcher {kind.Name}(string text) => TokenMatcher.Default.WithKind(SyntaxKind.{kind.Name}).WithValue(text);");
                }
                else if (kind.Name == nameof(SyntaxKind.IdentifierToken))
                {
                    b.AppendLine($"public static TokenMatcher {kind.Name}(string text) => TokenMatcher.Default.WithKind(SyntaxKind.{kind.Name}).WithText(text);");
                }
                else
                {
                    b.AppendLine($"public static TokenMatcher {kind.Name} => TokenMatcher.Default.WithKind(SyntaxKind.{kind.Name});");
                }
            }


            b.AppendLine("} }");

            context.AddSource("Is.Token.cs", Utilities.Normalize(b));
        }

        public static (Dictionary<IFieldSymbol, INamedTypeSymbol>, HashSet<INamedTypeSymbol>) GetKindToClassMap(GeneratorExecutionContext context)
        {
            return GenerateMap(context);
        }
        public static Dictionary<INamedTypeSymbol, IFieldSymbol> GetClassToKindMap(GeneratorExecutionContext context)
        {
            var classToKind = new Dictionary<INamedTypeSymbol, IFieldSymbol>(SymbolEqualityComparer.Default);
            foreach (var kvp in GenerateMap(context).Item1)
            {
                if (kvp.Value is not null)
                {
                    classToKind[kvp.Value] = kvp.Key;
                }
            }
            return classToKind;
        }

        /// <summary>
        /// Creates a mapping of <see cref="SyntaxKind"/> enumeration to the class object that
        /// it's associated with.
        /// </summary>
        /// <param name="context">generator context</param>
        public static (Dictionary<IFieldSymbol, INamedTypeSymbol>, HashSet<INamedTypeSymbol>) GenerateMap(GeneratorExecutionContext context)
        {
            var kindToClass = new Dictionary<IFieldSymbol, INamedTypeSymbol>(SymbolEqualityComparer.Default);

            var syntaxKinds = context.Compilation.GetTypeByMetadataName(typeof(SyntaxKind).FullName)
                                                             .GetMembers()
                                                             .Where(m => !IgnoreKinds.Any(i => m.Name.Contains(i)))
                                                             .OfType<IFieldSymbol>()
                                                             .Where(f => f.HasConstantValue && f.ConstantValue is ushort v && v > 0)
                                                             .ToArray();

            string namespaceString = string.Join(".", typeof(ClassDeclarationSyntax).FullName.Split('.').Reverse().Skip(1).Reverse());

            INamedTypeSymbol lastType = null;

            foreach (IFieldSymbol kind in syntaxKinds)
            {
                if (kind.Name is nameof(SyntaxKind.None) or nameof(SyntaxKind.List))
                    continue;

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

            HashSet<INamedTypeSymbol> abstractTypes = new(
                kindToClass
                    .Values
                    .SelectMany(b => b.BaseTypes())
                    .Distinct<ITypeSymbol>(SymbolEqualityComparer.Default)
                    .OfType<INamedTypeSymbol>()
                    .Where(t => t.IsAbstract && !t.Name.StartsWith("Base")),
                SymbolEqualityComparer.Default);

            return (kindToClass, abstractTypes);
        }

        private void GenerateParser(GeneratorExecutionContext context, List<SyntaxClassInfo> withClasses)
        {
            var extraTypes = GetNonSyntaxClasses(context);

            StringBuilder builder = new();

            builder.AppendLine(
@"
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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

            AddNonSyntaxClasses(builder, extraTypes);

            builder.AppendLine(@"
                default:
                    var custom = GetMatcherHook(node);
                    if (custom != null)
                        nodeMatcher = custom;
                    else
                        throw new ArgumentException($""unknown tag: {element.Name}"");
                    break;
                }

                if (nodeMatcher is ITreeWalkNodeMatcher treeWalk && !childrenHandled)
                {
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
                            treeWalk.AddChild(childMatcher);
                        }
                    }
                }
            }

                return nodeMatcher;
            }
");


            builder.AppendLine(@"
        private INodeMatcher ParseExplicit(XmlNode node, ParseNodeDelegate parseDelegate = null, INodeMatcher parent = null)
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
                                {{
                                    throw new ArgumentException($""unable to build matcher for {{listElement.Name}}"");
                                }}
                                matcher.Add{property.Name}(listElementMatcher);
                            }}
");
                        }
                        else if (property.Type.Name == nameof(SyntaxToken))
                        {
                            builder.AppendLine($@"{{
                                if (matcher.{property.Name} != null)
                                {{
                                    throw new ArgumentException($""expected only 1 matcher for {property.Name}"");
                                }}
                                
                                var childNode = child.ChildNodes.OfType<XmlElement>().FirstOrDefault(); 
                                TokenMatcher childMatcher = TokenMatcher.Default;

                                if (child.Attributes[""Kind""]?.Value is string kindString)
                                {{
                                    SyntaxKind kind = (SyntaxKind)Enum.Parse(typeof(SyntaxKind), kindString);
                                    childMatcher = childMatcher.WithKind(kind);
                                }}
                                matcher.{property.Name} = childMatcher; 
}}");
                        }
                        else
                        {
                            var matcher = property.Type.GetMatcherBase();

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
                                    throw new ArgumentException($""unable to build matcher for {{child.Name}}"");");

                            if (matcher is not ("ITokenMatcher" or "INodeMatcher"))
                            {
                                builder.AppendLine($"matcher.{property.Name} = childMatcher.For<{matcher}>();");
                            }
                            else
                            {

                                builder.AppendLine($"matcher.{property.Name} = childMatcher;");
                            }
                            builder.AppendLine("}");
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
                    builder.AppendLine("return matcher;");
                    builder.AppendLine("}");
                }
            }

            AddNonSyntaxClasses(builder, extraTypes);

            builder.AppendLine("}");
            builder.AppendLine("}");
            builder.AppendLine("return null;");
            builder.AppendLine("}");

            // class
            builder.AppendLine("}");

            //namespace
            builder.AppendLine("}");

            context.AddSource("SearchParser.g.cs", Utilities.Normalize(builder));
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
                if (classType.GetAttributes().Any(attr => attr.AttributeClass.Name == "ExcludeAttribute"))
                    continue;

                types.Add((classType, classDecl, classType.Name.Replace("Matcher", "")));
            }

            return types;
        }

        private void AddNonSyntaxClasses(StringBuilder builder,
                                         List<(INamedTypeSymbol, ClassDeclarationSyntax, string)> extraTypes)
        {
            foreach ((var classType, var classDecl, var tagName) in extraTypes)
            {
                builder.AppendLine($"case \"{tagName}\":");
                bool takesArg = classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
                {
                    IFieldSymbol[] fields = [.. classType.GetMembers()
                                                         .OfType<IFieldSymbol>()
                                                         .Where(f => f is {
                                                            CanBeReferencedByName : true,
                                                            IsStatic : false,
                                                            DeclaredAccessibility: Accessibility.Protected or Accessibility.Private
                                                         })];// && f.Type.Name != "INodeMatcher" && !f.Type.AllInterfaces.Any(intf => intf.Name == "INodeMatcher"))];

                    builder.AppendLine("{");

                    List<string> args = [];

                    foreach (var f in fields)
                    {
                        string shortName = f.Name.Trim('_').Replace("Kind", "");
                        string attributeName = $"{char.ToUpper(shortName[0])}{shortName.Substring(1)}";

                        switch (f.Type)
                        {
                            case { SpecialType: SpecialType.System_String }:
                                builder.AppendLine($"{f.Type} {shortName} = default;");
                                builder.AppendLine(@$"{shortName} = element.Attributes[""{attributeName}""]?.Value;");
                                args.Add(shortName);
                                break;

                            case { SpecialType: SpecialType.System_Boolean }:
                                builder.AppendLine($"{f.Type} {shortName} = default;");
                                builder.AppendLine($@"
                                if (element.Attributes[""{attributeName}""]?.Value is string {shortName}Raw)
                                {{
                                    {shortName} = bool.Parse({shortName}Raw);
                                }}");
                                args.Add(shortName);
                                break;

                            case { TypeKind: TypeKind.Enum }:
                                builder.AppendLine($"{f.Type} {shortName} = default;");
                                builder.AppendLine($@"
                                if (element.Attributes[""{attributeName}""]?.Value is string {shortName}Raw)
                                {{
                                    {shortName} = ({f.Type.ToDisplayString()})Enum.Parse(typeof({f.Type.ToDisplayString()}), {shortName}Raw);
                                }}");
                                args.Add(shortName);
                                break;

                            case { TypeKind: TypeKind.Interface, Name: "INodeMatcher" }:
                                args.Add($"{shortName}Nested");
                                builder.AppendLine($@"
                                    INodeMatcher {shortName}Nested = default;
                                    if (element.FirstChild is not null)
                                    {{
                                        {shortName}Nested = ParseTree(element.FirstChild, ParseTree, null);
                                    }}
                                ");
                                break;

                            default:
                                break;
                        }
                    }

                    if (classType.AllInterfaces.Any(intf => intf is { Name: "ITreeWalkNodeMatcher "}))
                    {
                        builder.AppendLine(@$"
                                var matcher = new {classType.Name}({string.Join(", ", args)});
                                
                                foreach (var c in element.ChildNodes.OfType<XmlElement>())
                                {{
                                    var childMatcher = parseDelegate(c, parseDelegate, matcher);
                                    if (childMatcher != null)
                                        matcher.AddChild(childMatcher);
                                }}
                                return matcher;");
                    }
                    else
                    {
                        builder.AppendLine($"return new {classType.Name}({string.Join(", ", args)});");
                    }

                    builder.AppendLine("}");
                }
            }
        }

        private void CompletePartialClasses(GeneratorExecutionContext context, Compilation injectedCompilation)
        {
            Compilation compilation = injectedCompilation;
            var baseMatcher = compilation.GetTypeByMetadataName("SyntaxSearch.Matchers.BaseMatcher");

            bool isOverrideWithProperties(INamedTypeSymbol type, INamedTypeSymbol stringType)
            {
                var currentType = type.BaseType;

                while (currentType is not null && !SymbolEqualityComparer.Default.Equals(baseMatcher, currentType))
                {
                    foreach (var f in currentType.GetMembers().OfType<IFieldSymbol>().Where(f => f.CanBeReferencedByName && !f.IsStatic && !f.IsReadOnly && !f.IsAbstract))
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

            Dictionary<string, StringBuilder> staticBuilders = new();
            void addToStaticClass(AttributeData attribute,
                                  INamedTypeSymbol returnType,
                                  string ctor,
                                  Optional<string> arguments)
            {
                var target = attribute.AttributeClass.Name.Replace("Attribute", "");
                if (!staticBuilders.TryGetValue(target, out var builder))
                {
                    builder = staticBuilders[target] = new StringBuilder();
                    builder.AppendLine($@"
using SyntaxSearch.Matchers;

namespace SyntaxSearch.Framework
{{
    public static partial class {target}
    {{
");
                }

                string methodName;
                if (attribute.ConstructorArguments.FirstOrDefault() is { Value: string v }
                    && !string.IsNullOrWhiteSpace(v))
                {
                    methodName = v;
                }
                else
                {
                    methodName = returnType.Name.Replace("Matcher", "");
                }

                if (arguments is { HasValue: true, Value: not "" or null })
                {
                    builder.AppendLine($"public static {returnType.ToDisplayString()} {methodName}({arguments.Value}) => {ctor};");
                }
                else
                {
                    builder.AppendLine($"public static {returnType.ToDisplayString()} {methodName} => {ctor};");
                }
            }

            foreach (var classDecl in _receiver.Collected)
            {
                var model = compilation.GetSemanticModel(classDecl.SyntaxTree);
                var interfaceType = model.Compilation.GetTypeByMetadataName("SyntaxSearch.Matchers.INodeMatcher");

                var stringType = model.Compilation.GetSpecialType(SpecialType.System_String);
                var boolType = model.Compilation.GetSpecialType(SpecialType.System_Boolean);

                var methodAttribute = model.Compilation.GetTypeByMetadataName("SyntaxSearch.Framework.MethodAttribute");

                var classType = model.GetDeclaredSymbol(classDecl);

                if (!classType.AllInterfaces.Any(intf => SymbolEqualityComparer.Default.Equals(intf, interfaceType)))
                    continue;
                if (classType.IsAbstract || classType.TypeKind != TypeKind.Class)
                    continue;

                bool isOverride = isOverrideWithProperties(classType, stringType);

                StringBuilder builder = new();

                IFieldSymbol[] fields = [
                    ..classType.GetMembers().OfType<IFieldSymbol>()
                      .Where(f => f is { CanBeReferencedByName: true, IsStatic: false, IsAbstract: false, DeclaredAccessibility: Accessibility.Protected or Accessibility.Private })];

                string[] constructorArgs = [.. fields.Select(f => $"{f.Type.ToDisplayString()} {f.Name.Trim('_')}")];

                bool generate = classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword))
                    || classType.GetMembers().OfType<IMethodSymbol>().Any(m => m.CanBeReferencedByName && m.MethodKind == MethodKind.Constructor && !m.IsImplicitlyDeclared);

                if (classType.GetAttributes().FirstOrDefault(p => p.AttributeClass.IsSubclassOf(methodAttribute)) is { } attribute)
                {
                    string ctor;
                    string arguments;
                    if (classType.AllInterfaces.Any(intf => intf.Name == "ICompoundLogicalMatcher"))
                    {
                        ctor = $"new {classType.ToDisplayString()}(matchers)";
                        arguments = "params INodeMatcher[] matchers";
                    }
                    else
                    {
                        ctor = $"new {classType.ToDisplayString()}()";
                        arguments = "";
                    }

                    addToStaticClass(attribute, classType, ctor, arguments);
                }

                if (generate)
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
    {{");

                    List<string> copyFields = [];
                    List<IFieldSymbol> withFields = [];

                    bool firstField = true;
                    foreach (var field in fields)
                    {
                        if (firstField)
                        {
                            builder.AppendLine(@$"
        public {classType.Name}({string.Join(", ", constructorArgs)}){(isOverride ? " : base()" : "")}
        {{");
                        }
                        firstField = false;

                        if (SymbolEqualityComparer.Default.Equals(field.Type, stringType))
                        {
                            builder.AppendLine($"{field.Name} = {field.Name.Trim('_')};");
                            copyFields.Add(field.Name);
                        }
                        else if (SymbolEqualityComparer.Default.Equals(field.Type, boolType))
                        {
                            builder.AppendLine($"{field.Name} = {field.Name.Trim('_')};");
                            copyFields.Add(field.Name);
                        }
                        else if (field.Type.TypeKind == TypeKind.Enum)
                        {
                            builder.AppendLine($"{field.Name} = {field.Name.Trim('_')};");
                            copyFields.Add(field.Name);
                        }
                        else if (field.Type.Name is "LogicalOrNodeMatcher" or "INodeMatcher")
                        {
                            builder.AppendLine($"{field.Name} = {field.Name.Trim('_')};");
                            copyFields.Add(field.Name);
                        }

                        if (field.GetAttributes().Any(attr => attr.AttributeClass.Name == "WithAttribute"))
                        {
                            withFields.Add(field);
                        }
                    }

                    if (!firstField)
                    {
                        // close constructor
                        builder.AppendLine("}");
                    }

                    // copy constructor
                    builder.AppendLine($@"
                            public {classType.Name}({classType.Name} copy) : base(copy)
                            {{");
                    foreach (var c in copyFields)
                    {
                        builder.AppendLine($"this.{c} = copy.{c};");
                    }

                    builder.AppendLine("}");

                    if (!classType.Constructors.Any(c => c is { DeclaredAccessibility: Accessibility.Public, CanBeReferencedByName: true, Parameters.IsEmpty: true }))
                    {
                        builder.AppendLine($"public {classType.Name}() {{ }}");
                    }

                    foreach (var field in withFields.Where(f => !f.IsReadOnly))
                    {
                        string trimmedName = field.Name.Trim('_');
                        string methodName = $"{char.ToUpper(trimmedName[0])}{trimmedName.Substring(1)}";

                        builder.AppendLine($@"
                            public {classType.Name} With{methodName}({field.Type.ToDisplayString()} matcher)
                            {{
                                var copy = new {classType.Name}(this);
                                copy.{field.Name} = matcher;
                                return copy;
                            }}
");
                    }
                }

                if (builder.Length > 0)
                {
                    // close class
                    builder.AppendLine("}");
                    // close namespace
                    builder.AppendLine("}");
                    context.AddSource($"{classType.Name}.Ctor.g.cs", Utilities.Normalize(builder));
                }
            }

            foreach (var kvp in staticBuilders)
            {
                kvp.Value.AppendLine("}}");
                context.AddSource($"{kvp.Key}.Partial.g.cs", Utilities.Normalize(kvp.Value));
            }
        }

        private List<(SyntaxClassInfo, string)> GenerateMatcherClasses(GeneratorExecutionContext context)
        {
            var syntaxKinds = context.Compilation.GetTypeByMetadataName(typeof(SyntaxKind).FullName)
                                                             .GetMembers()
                                                             .Where(m => !IgnoreKinds.Any(i => m.Name.Contains(i)))
                                                             .OfType<IFieldSymbol>()
                                                             .Where(f => f.HasConstantValue && f.ConstantValue is ushort v && v > 0);

            string namespaceString = string.Join(".", typeof(ClassDeclarationSyntax).FullName.Split('.').Reverse().Skip(1).Reverse());

            List<(SyntaxClassInfo, string)> newTrees = [];

            StringBuilder map = new();

            var syntaxNodeType = context.Compilation.GetTypeByMetadataName(typeof(SyntaxNode).FullName);
            var syntaxTokenType = context.Compilation.GetTypeByMetadataName(typeof(SyntaxToken).FullName);
            var syntaxTokenList = context.Compilation.GetTypeByMetadataName(typeof(SyntaxTokenList).FullName);

            Dictionary<INamedTypeSymbol, string> typeToClassMap = new(SymbolEqualityComparer.Default);

            (var classes, var abstractClasses) = GetKindToClassMap(context);

            BuildAbstractMatcherClasses(context, abstractClasses);

            StringBuilder isBuilder = new StringBuilder();
            isBuilder.AppendLine($@"using SyntaxSearch.Matchers.Explicit;

namespace SyntaxSearch.Framework
{{
    public static partial class Is
    {{");

            foreach (var entry in classes)
            {
                var kind = entry.Key;
                var associatedType = entry.Value;

                var info = new SyntaxClassInfo
                {
                    KindName = kind.Name,
                    ClassName = $"{kind.Name}Matcher"
                };

                SyntaxKind value = (SyntaxKind)((ushort)kind.ConstantValue);

                if (SyntaxFacts.IsAnyToken(value))
                {
                    // this is a token, skip
                }
                else if (associatedType is null)
                {
                    var slim = BuildClassNoOverrides(kind, namedProperties: [], classes: classes);
                    string classTypeName = $"{kind.Name}.Matcher";

                    string source = Utilities.Normalize(slim);
                    context.AddSource($"{classTypeName}.g.cs", source);

                    newTrees.Add((info, source));
                }
                else
                {
                    var namedProperties = Helpers.GetNamedProperties(kind, associatedType, syntaxNodeType, syntaxTokenType);
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

                    info.Properties = [.. namedProperties];
                    info.Fields = [.. fields];

                    string contents = BuildClass(kind, associatedType, fields, namedProperties, classes);

                    string source = Utilities.Normalize(contents);
                    context.AddSource($"{kind.Name}.Matcher.g.cs", source);
                    newTrees.Add((info, source));

                    isBuilder.AppendLine($"public static {info.ClassName} {kind.Name} => new {info.ClassName}();");
                }
            }

            // class
            isBuilder.AppendLine("}");
            // namespace
            isBuilder.AppendLine("}");

            context.AddSource($"Is.Syntax.g.cs", Utilities.Normalize(isBuilder));

            return newTrees;
        }

        private void BuildAbstractMatcherClasses(GeneratorExecutionContext context, HashSet<INamedTypeSymbol> abstractClasses)
        {
            var explicitMatcher = context.Compilation.GetTypeByMetadataName("SyntaxSearch.Matchers.ExplicitNodeMatcher");

            foreach (var abstractClass in abstractClasses)
            {
                string name = $"{abstractClass.Name}Matcher";

                string baseType = abstractClasses.Contains(abstractClass.BaseType)
                    ? $"{abstractClass.BaseType.Name}Matcher"
                    : "ExplicitNodeMatcher";

                StringBuilder builder = new();
                builder.AppendLine($@"
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Collections.Immutable;


namespace SyntaxSearch.Matchers.Explicit
{{
    public abstract partial class {name} : {baseType}
    {{
        protected {name}(string captureName, string matchName) : base(captureName, matchName)
        {{
        }}

        protected {name}({name} copy) : base(copy)
        {{
        }}

        protected {name}() : base()
        {{
        }}
    }}
}}
");
                context.AddSource($"{name}.g.cs", Utilities.Normalize(builder));
            }
        }

        private static string GenerateFieldName(string kvp)
        {
            return $"_{char.ToLower(kvp[0])}{kvp.Substring(1)}";
        }

        private string BuildClassNoOverrides(IFieldSymbol kind,
                                             INamedTypeSymbol classType = null,
                                             MatchProperty[] namedProperties = null,
                                             IReadOnlyDictionary<IFieldSymbol, INamedTypeSymbol> classes = null)
        {
            StringBuilder builder = new();
            string className = classType?.Name;

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
                    GenerateExplicitMatcherClass(kind, classType, [], namedProperties, builder, classes);
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
    public partial class {kind.Name}Matcher : NodeMatcher
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
                if (namedProperties.Any())
                {
                    GenerateExplicitMatcherClass(kind, classType, [], namedProperties, builder, classes);
                }
            }


            return builder.ToString();
        }

        private static void GenerateExplicitMatcherClass(IFieldSymbol kind,
                                                         INamedTypeSymbol classType,
                                                         List<MatchField> fields,
                                                         MatchProperty[] namedProperties,
                                                         StringBuilder builder,
                                                         IReadOnlyDictionary<IFieldSymbol, INamedTypeSymbol> classes)
        {
            string className = classType.Name;

            string baseType = classType.BaseType switch
            {
                { IsAbstract: true } bt when !bt.Name.StartsWith("Base") => $"{bt.Name}Matcher",
                _ => "ExplicitNodeMatcher"
            };

            builder.AppendLine("namespace SyntaxSearch.Matchers.Explicit {");

            builder.AppendLine($"public partial class {kind.Name}Matcher : {baseType} {{");

            foreach (var i in fields)
            {
                builder.AppendLine($"private {i.TypeName} {i.FieldName};");
            }

            StringBuilder builderMethods = new();

            foreach ((var namedProp, bool isList) in namedProperties)
            {
                if (isList)
                {
                    builder.AppendLine($"public ImmutableArray<INodeMatcher> {namedProp.Name} {{ get;  internal set; }} = ImmutableArray.Create<INodeMatcher>();");

                    builderMethods.AppendLine($@"internal void Add{namedProp.Name}(INodeMatcher matcher)
                    {{
                        if (matcher is null)
                        {{
                            throw new ArgumentNullException(nameof(matcher));
                        }}
                        {namedProp.Name} = {namedProp.Name}.Add(matcher);
                    }}");


                    builderMethods.AppendLine($@"public {kind.Name}Matcher With{namedProp.Name}(params INodeMatcher[] matcher)
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
                    bool isToken = namedProp.Type.Name == nameof(SyntaxToken);

                    (string matchType, string propertyType) = namedProp.Type switch
                    {
                        { Name: nameof(SyntaxToken) } => ("ITokenMatcher", "ITokenMatcher"),
                        { IsAbstract: true } t when !t.Name.StartsWith("Base") => ($"{t.Name}Matcher", $"LogicalOrNodeMatcher<{t.Name}Matcher>"),
                        _ => ("INodeMatcher", "INodeMatcher")
                    };

                    builder.AppendLine($"public {propertyType} {namedProp.Name} {{ get; internal set; }}");

                    KeyValuePair<IFieldSymbol, INamedTypeSymbol> p = classes.FirstOrDefault(p => SymbolEqualityComparer.Default.Equals(p.Value, namedProp.Type));

                    if (!isToken && !p.Equals(default(KeyValuePair<IFieldSymbol, INamedTypeSymbol>)))
                    {
                        builderMethods.AppendLine($@"
                        public {kind.Name}Matcher With{namedProp.Name}(ILogicalMatcher matcher) 
                        {{ 
                            return new {kind.Name}Matcher(this)
                            {{
                                {namedProp.Name} = matcher
                            }};
                        }}");

                        builderMethods.AppendLine($@"
                        public {kind.Name}Matcher With{namedProp.Name}({p.Key.Name}Matcher matcher) 
                        {{ 
                            return new {kind.Name}Matcher(this)
                            {{
                                {namedProp.Name} = matcher
                            }};
                        }}");
                    }
                    else
                    {
                        if (isToken)
                        {
                            builderMethods.AppendLine($@"
                        public {kind.Name}Matcher With{namedProp.Name}({typeof(SyntaxKind).FullName} kind) 
                        {{ 
                            return this.With{namedProp.Name}((TokenMatcher)kind);
                        }}");
                        }

                        builderMethods.AppendLine($@"
                        public {kind.Name}Matcher With{namedProp.Name}({matchType} matcher) 
                        {{ 
                            return new {kind.Name}Matcher(this)
                            {{
                                {namedProp.Name} = matcher
                            }};
                        }}");

                        if (matchType != propertyType)
                        {
                            builderMethods.AppendLine($@"
                        public {kind.Name}Matcher With{namedProp.Name}(ILogicalMatcher matcher) 
                        {{ 
                            return new {kind.Name}Matcher(this)
                            {{
                                {namedProp.Name} = new {propertyType}(matcher)
                            }};
                        }}");
                        }
                    }
                }
            }

            string constructorArgs = string.Join(
    ", ",
    [.. fields.Select(i => $"{i.TypeName} {i.FieldName.TrimStart('_')} = default"), "string captureName = null, string matchCapture = null"]);


            builder.AppendLine($@"
        public {kind.Name}Matcher(
            {constructorArgs}) : base(
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
                // TODO
            }
            foreach ((var namedProp, _) in namedProperties)
            {
                builder.AppendLine($"this.{namedProp.Name} = copy.{namedProp.Name};");
            }

            builder.AppendLine("}");

            builder.AppendLine(builderMethods.ToString());

            builder.AppendLine($@"
                protected override bool IsNodeMatch(SyntaxNode node, CaptureStore store) {{
                    if (node is not {className} obj)
                        return false;
                    if (!obj.IsKind(SyntaxKind.{kind.Name}))
                        return false;");

            foreach (var field in fields)
            {
                builder.AppendLine(@$"
            if (!string.IsNullOrWhiteSpace({field.FieldName}) && !obj.{field.TokenName}.{field.TokenValue}.Equals({field.FieldName}))
                return false;");
            }

            builder.AppendLine("return true;}");

            builder.AppendLine(@$"
                protected override bool DoChildrenMatch(SyntaxNode node, CaptureStore store)
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
            if (!{namedProp.Name}[i].IsMatch({localName}[i], store))
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

        if (!{namedProp.Name}.IsMatch({localName}, store))
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

        private readonly Lazy<string> NewLineLazy = new Lazy<string>(() =>
        {
            StringBuilder b = new();
            b.AppendLine();
            return b.ToString();
        });

        private string NewLine => NewLineLazy.Value;

        private string BuildClass(IFieldSymbol kind,
                                  INamedTypeSymbol classType,
                                  List<MatchField> fields,
                                  MatchProperty[] namedProperties,
                                  IReadOnlyDictionary<IFieldSymbol, INamedTypeSymbol> classes)
        {
            string className = classType.Name;

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
{string.Join(NewLine, fields.Select(i => $"        private {i.TypeName} {i.FieldName};"))}

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

        protected override bool IsNodeMatch(SyntaxNode node, CaptureStore store)
        {{
            if (!base.IsNodeMatch(node, store))
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

            GenerateExplicitMatcherClass(kind, classType, fields, namedProperties, builder, classes);

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
}