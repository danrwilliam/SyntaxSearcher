using Microsoft.CodeAnalysis;
using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;

namespace SyntaxSearcher.Generators
{
    [Generator]
    public class MatcherGenerator : ISourceGenerator
    {
        private static readonly ImmutableArray<string> IgnoreKinds = ImmutableArray.Create("Keyword", "Trivia");
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
            {nameof(SyntaxKind.UnaryPlusExpression), nameof(PrefixUnaryExpressionSyntax) },
            {nameof(SyntaxKind.PostIncrementExpression), nameof(PostfixUnaryExpressionSyntax) },
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

            var injected = context.Compilation.AddSyntaxTrees(
                generated.Select(g => SyntaxFactory.ParseSyntaxTree(SourceText.From(g.Item2), context.ParseOptions, $"{g.Item1.ClassName}.g.cs")));
            CompletePartialClasses(context, injected);

            CreateIsTokenMethods(context);
        }

        private void CreateIsTokenMethods(GeneratorExecutionContext context)
        {
            StringBuilder b = new();
            b.AppendLine(@"
using SyntaxSearch.Matchers;
using Microsoft.CodeAnalysis.CSharp;

namespace SyntaxSearch.Framework
{
    public static partial class Is
    {
");

            var syntaxKind = context.Compilation.GetTypeByMetadataName(typeof(SyntaxKind).FullName);

            foreach (var kind in syntaxKind.GetMembers().OfType<IFieldSymbol>().Where(s => s.Name.EndsWith("Token")))
            {
                if (kind.Name == nameof(SyntaxKind.NumericLiteralToken))
                {
                    b.AppendLine($"public static TokenMatcher {kind.Name} => TokenMatcher.Default.WithKind(SyntaxKind.{kind.Name});");
                    foreach (var numeric in NumericTypes)
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

        static readonly SpecialType[] NumericTypes =
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
                    .Where(t => t.IsAbstract), // && !t.Name.StartsWith("Base")),
                SymbolEqualityComparer.Default);

            return (kindToClass, abstractTypes);
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

            Dictionary<string, StringBuilder> staticBuilders = [];
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

                bool methods = false;
                foreach (var useConstructor in returnType.Constructors.Where(c => c is { IsStatic: false, DeclaredAccessibility: Accessibility.Public or Accessibility.Internal }
                                                                        && c.GetAttributes().Any(attr => attr.AttributeClass is { Name: "UseConstructorAttribute" })))
                {
                    bool isExt = useConstructor.Parameters.Length > 0;
                    var parameters = string.Join(", ", useConstructor.Parameters.Select((p, idx) =>
                    {
                        if (isExt && idx == 0)
                        {
                            return $"this {p.Type.ToDisplayString()} {p.Name}";
                        }
                        else
                        {
                            return $"{p.Type.ToDisplayString()} {p.Name}";
                        }
                    }));
                    var argNames = string.Join(", ", useConstructor.Parameters.Select(p => p.Name));

                    builder.AppendLine($@"
                        public static {returnType.ToDisplayString()} {methodName}({parameters}) => new {returnType.ToDisplayString()}({argNames});
");

                    methods = true;
                }

                if (!methods)
                {
                    if (arguments is { HasValue: true, Value: not "" or null })
                    {
                        builder.AppendLine($"public static {returnType.ToDisplayString()} {methodName}({arguments.Value}) => {ctor};");
                    }
                    else
                    {
                        builder.AppendLine($"public static {returnType.ToDisplayString()} {methodName} => {ctor};");
                    }
                }
            }

            foreach (var classDecl in _receiver.Collected)
            {
                var model = compilation.GetSemanticModel(classDecl.SyntaxTree);
                var nodeMatcherInterface = model.Compilation.GetTypeByMetadataName("SyntaxSearch.Matchers.INodeMatcher");
                var syntaxListMatcherInterface = model.Compilation.GetTypeByMetadataName("SyntaxSearch.Matchers.ISyntaxListMatcher");

                var stringType = model.Compilation.GetSpecialType(SpecialType.System_String);
                var boolType = model.Compilation.GetSpecialType(SpecialType.System_Boolean);

                var methodAttribute = model.Compilation.GetTypeByMetadataName("SyntaxSearch.Framework.MethodAttribute");

                var classType = model.GetDeclaredSymbol(classDecl);

                if (!classType.AllInterfaces.Any(intf => SymbolEqualityComparer.Default.Equals(intf, nodeMatcherInterface))
                    && !classType.AllInterfaces.Any(intf => SymbolEqualityComparer.Default.Equals(intf, syntaxListMatcherInterface)))
                {
                    continue;
                }
                if (classType.IsAbstract || classType.TypeKind != TypeKind.Class)
                {
                    continue;
                }
                if (classType.AllInterfaces.Any(intf => intf is { Name: "IExplicitNodeMatcher", Arity: 1 }))
                {
                    continue;
                }

                bool isOverride = isOverrideWithProperties(classType, stringType);

                StringBuilder builder = new();

                PropertyOrField[] fields = [
                    ..classType.GetMembers()
                    .Select(PropertyOrField.From)
                      .Where(f => f is 
                      { 
                          CanBeReferencedByName: true, 
                          IsStatic: false, 
                          IsAbstract: false,
                          DeclaredAccessibility: Accessibility.Protected or Accessibility.Private 
                      } || f?.Attributes.Any(attr => attr.AttributeClass.Name == "WithAttribute") == true)];

                static string toParameter(string name)
                {
                    string n = name.Trim('_');
                    if (char.IsUpper(n[0]))
                    {
                        return $"{char.ToLower(n[0])}{n[1..]}";
                    }
                    else
                    {
                        return n;
                    }
                }

                string[] constructorArgs = [.. fields.Select(f => $"{f.Type.ToDisplayString()} {toParameter(f.Name)}")];

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
                    List<PropertyOrField> withFields = [];

                    bool needsConstructorWithFields =
                            !classType.Constructors.Any(c => c.Parameters.Select(t => t.Type).SequenceEqual(fields.Select(f => f.Type), SymbolEqualityComparer.Default));

                    void buildCtor(string code)
                    {
                        if (needsConstructorWithFields)
                        {
                            builder.AppendLine(code);

                        }
                    }

                    bool firstField = true;
                    foreach (var field in fields)
                    {
                        if (firstField)
                        {
                            buildCtor(@$"
        public {classType.Name}({string.Join(", ", constructorArgs)}){(isOverride ? " : base()" : "")}
        {{");
                        }
                        firstField = false;

                        string parameterName = toParameter(field.Name);

                        if (SymbolEqualityComparer.Default.Equals(field.Type, stringType))
                        {
                            buildCtor($"this.{field.Name} = {parameterName};");
                            copyFields.Add(field.Name);
                        }
                        else if (SymbolEqualityComparer.Default.Equals(field.Type, boolType))
                        {
                            buildCtor($"this.{field.Name} = {parameterName};");
                            copyFields.Add(field.Name);
                        }
                        else if (field.Type.TypeKind == TypeKind.Enum)
                        {
                            buildCtor($"this.{field.Name} = {parameterName};");
                            copyFields.Add(field.Name);
                        }
                        else if (field.Type.Name is "LogicalOrNodeMatcher" or "INodeMatcher" or "ISyntaxNodeMatcher")
                        {
                            buildCtor($"this.{field.Name} = {parameterName};");
                            copyFields.Add(field.Name);
                        }

                        if (field.Attributes.Any(attr => attr.AttributeClass.Name == "WithAttribute"))
                        {
                            withFields.Add(field);
                        }
                    }

                    if (!firstField)
                    {
                        // close constructor
                        buildCtor("}");
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

                    if (classType.BaseType.Constructors.FirstOrDefault(c => c is
                        {
                            IsStatic: false,
                            Parameters.IsEmpty: false
                        }) is { } baseCtor
                        && !baseCtor.Parameters.Any(p => p.Name == "copy"))
                    {
                        string parameters = string.Join(", ", baseCtor.Parameters.Select(p => p.ToDisplayString()));
                        string paramNames = string.Join(", ", baseCtor.Parameters.Select(p => p.Name));

                        builder.AppendLine($"public {classType.Name}({parameters}): base({paramNames}) {{ }}");

                    }
                    else if (!classType.Constructors.Any(c => c is
                        {
                            DeclaredAccessibility: Accessibility.Public,
                            IsStatic: false,
                            Parameters.IsEmpty: true,
                            CanBeReferencedByName: true
                        }))
                    {
                        builder.AppendLine($"public {classType.Name}() {{ }}");
                    }

                    foreach (var field in withFields)//.Where(f => !f.IsReadOnly))
                    {
                        string trimmedName = field.Name.Trim('_');
                        string methodName = $"{char.ToUpper(trimmedName[0])}{trimmedName.Substring(1)}";

                        if (field.Type.Name == "LogicalOrNodeMatcher")
                        {
                            var nodeMatcher = ((INamedTypeSymbol)field.Type).TypeArguments[0];

                            builder.AppendLine($@"
                            public {classType.Name} With{methodName}({nodeMatcher.ToDisplayString()} matcher)
                            {{
                                var copy = new {classType.Name}(this);
                                copy.{field.Name} = matcher;
                                return copy;
                            }}

                            public {classType.Name} With{methodName}(ILogicalMatcher matcher)
                            {{
                                var copy = new {classType.Name}(this);
                                copy.{field.Name} = matcher.For<{nodeMatcher.ToDisplayString()}>();
                                return copy;
                            }}
");
                        }
                        else
                        {
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



            StringBuilder isBuilder = new();
            isBuilder.AppendLine(@"using SyntaxSearch.Matchers;

namespace SyntaxSearch.Framework
{
    public static partial class Is
    {");

            HashSet<string> isMethods = [];

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
                }
                else
                {
                    var namedProperties = Helpers.GetNamedProperties(kind, associatedType, syntaxNodeType, syntaxTokenType);
                    // type, node access, field name
                    List<MatchField> fields = [];

                    foreach (var kvp in TokenUtils.Properties)
                    {
                        var property = associatedType.GetAllMembers(kvp.Key).OfType<IPropertySymbol>().FirstOrDefault();
                        if (property?.Type.GetAllMembers(kvp.Value).FirstOrDefault() is IPropertySymbol)
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

                    string contents = BuildClass(kind, associatedType, fields, namedProperties, classes, context);

                    string source = Utilities.Normalize(contents);
                    context.AddSource($"{kind.Name}.Matcher.g.cs", source);
                    newTrees.Add((info, source));

                    isBuilder.AppendLine($"public static {info.ClassName} {kind.Name} => new {info.ClassName}();");
                    isMethods.Add(kind.Name);
                }
            }

            // class
            isBuilder.AppendLine("}");
            // namespace
            isBuilder.AppendLine("}");

            context.AddSource("Is.Syntax.g.cs", Utilities.Normalize(isBuilder));

            var common = classes
                .Where(v => v is { Value: not null, Value.IsAbstract: false })
                .Select(v => v.Value)
                .SelectMany(v => new ITypeSymbol[] { v, v.BaseType })
                .Where(v => !v.Name.StartsWith("Base") && !v.Name.EndsWith("Base"))
                .GroupBy(v => v, SymbolEqualityComparer.Default)
                .Where(v => v.Count() > 1)
                .Select(v => v.First())
                .Where(v => !isMethods.Contains(v.Name.Replace("Syntax", "")))
                .ToArray();
            BuildCommonMatchers(context, common, classes);

            return newTrees;
        }

        private void BuildAbstractMatcherClasses(GeneratorExecutionContext context, HashSet<INamedTypeSymbol> abstractClasses)
        {
            var explicitMatcher = context.Compilation.GetTypeByMetadataName("SyntaxSearch.Matchers.ExplicitNodeMatcher");

            (var classes, _) = GetKindToClassMap(context);

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

namespace SyntaxSearch.Matchers
{{
    public abstract partial class {name} : {baseType}, IExplicitNodeMatcher<{abstractClass.Name}>
    {{
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

        private void BuildCommonMatchers(GeneratorExecutionContext context, ITypeSymbol[] common, Dictionary<IFieldSymbol, INamedTypeSymbol> classes)
        {
            var syntaxKind = context.Compilation.GetTypeByMetadataName(typeof(SyntaxKind).FullName);

            StringBuilder isExtensions = new("""
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.CSharp;
                using Microsoft.CodeAnalysis.CSharp.Syntax;
                using System;
                using System.Collections.Generic;
                using System.Linq;
                using System.Text;
                using System.Xml;
                using System.Collections.Immutable;
                using SyntaxSearch.Matchers.Common;

                namespace SyntaxSearch.Framework
                {
                    public static partial class Is
                    {                    
                """);

            foreach (INamedTypeSymbol commonBase in common.Cast<INamedTypeSymbol>())
            {
                StringBuilder b = new($"""
                    using Microsoft.CodeAnalysis;
                    using Microsoft.CodeAnalysis.CSharp;
                    using Microsoft.CodeAnalysis.CSharp.Syntax;
                    using System;
                    using System.Collections.Generic;
                    using System.Linq;
                    using System.Text;
                    using System.Xml;
                    using System.Collections.Immutable;
                    """);

                string className = $"{commonBase.Name.Replace("Syntax", "")}Matcher";

                MatchProperty[] namedProps =
                [
                    new(new PropertyWrapper(syntaxKind, "Kind", default), PropertyKind.SyntaxKind),
                    .. Helpers.GetNamedProperties(null, commonBase, context.Compilation)
                ];

                GenerateMatcherClass(className,
                                     null,
                                     commonBase,
                                     [],
                                     namedProps,
                                     b,
                                     classes,
                                     context,
                                     "SyntaxSearch.Matchers.Common");

                isExtensions.AppendLine($@"
                    public static {className} {commonBase.Name.Replace("Syntax", "")} => new {className}();
");

                context.AddSource($"{className}.Common.g.cs", Utilities.Normalize(b));
            }

            isExtensions.AppendLine("}");
            isExtensions.AppendLine("}");

            context.AddSource($"Is.Common.g.cs", Utilities.Normalize(isExtensions));
        }

        private static string GenerateFieldName(string kvp)
        {
            return $"_{char.ToLower(kvp[0])}{kvp.Substring(1)}";
        }

        private static void GenerateMatcherClass(
            string matcherClassName,
            string kindName,
            INamedTypeSymbol classType,
            List<MatchField> fields,
            MatchProperty[] namedProperties,
            StringBuilder builder,
            IReadOnlyDictionary<IFieldSymbol, INamedTypeSymbol> classes,
            GeneratorExecutionContext context,
            string namespaceName = "SyntaxSearch.Matchers")
        {
            string syntaxType = classType.Name;

            string baseType = classType.BaseType switch
            {
                { IsAbstract: true } bt => $"{bt.Name}Matcher",
                _ => "ExplicitNodeMatcher"
            };

            if (baseType == "ExplicitNodeMatcher")
            {
                Trace.WriteLine($"{classType.ToDisplayString()} couldn't get base type for matcher (base type = {classType.BaseType.ToDisplayString()})");
            }

            builder.AppendLine($"namespace {namespaceName} {{");

            builder.AppendLine($"public partial class {matcherClassName} : {baseType}, IExplicitNodeMatcher<{syntaxType}> {{");

            foreach (var i in fields)
            {
                builder.AppendLine($"private {i.TypeName} {i.FieldName};");
            }

            StringBuilder builderMethods = new();

            foreach ((var namedProp, PropertyKind generatorKind) in namedProperties)
            {
                if (generatorKind == PropertyKind.SyntaxKind)
                {
                    builder.AppendLine($"public INodeMatcher {namedProp.Name} {{ get; internal set; }} = default;");

                    builderMethods.AppendLine($@"
                        public {matcherClassName} With{namedProp.Name}({typeof(SyntaxKind).FullName} kind) 
                        {{ 
                            return new {matcherClassName}(this)
                            {{
                                Kind = new IsKindMatcher(kind)
                            }};
                        }}");
                }
                else if (generatorKind == PropertyKind.GenericTokenList)
                {
                    var namedType = (INamedTypeSymbol)namedProp.Type;
                    var typeArgument = namedType.TypeArguments[0];

                    builder.AppendLine($"public ISyntaxListMatcher {namedProp.Name} {{ get; internal set; }} = default;");

                    builderMethods.AppendLine($@"

                    public {matcherClassName} With{namedProp.Name}(params INodeMatcher[] matchers)
                    {{
                        return With{namedProp.Name}(new SyntaxListEqualTo(matchers));
                    }}

                    public {matcherClassName} With{namedProp.Name}(ISyntaxListMatcher matcher)
                    {{
                        if (matcher is null)
                        {{
                            throw new ArgumentNullException(nameof(matcher));
                        }}
                        
                        var copy = new {matcherClassName}(this)
                        {{
                            {namedProp.Name} = matcher
                        }};
                        return copy;
                    }}");
                }
                else
                {
                    bool isToken = namedProp.Type.Name == nameof(SyntaxToken);

                    (string matchType, string propertyType) = namedProp.Type switch
                    {
                        { } when generatorKind == PropertyKind.TokenList => ("ISyntaxTokenListMatcher", "ISyntaxTokenListMatcher"),
                        { Name: nameof(SyntaxToken) } => ("ITokenMatcher", "ITokenMatcher"),
                        { IsAbstract: true } t => ($"{t.Name}Matcher", $"LogicalOrNodeMatcher<{t.Name}Matcher>"),
                        _ => ("INodeMatcher", "INodeMatcher")
                    };

                    builder.AppendLine($"public {propertyType} {namedProp.Name} {{ get; internal set; }}");

                    KeyValuePair<IFieldSymbol, INamedTypeSymbol> p = classes.FirstOrDefault(p => SymbolEqualityComparer.Default.Equals(p.Value, namedProp.Type));

                    if (!isToken && !p.Equals(default(KeyValuePair<IFieldSymbol, INamedTypeSymbol>)))
                    {
                        builderMethods.AppendLine($@"
                        public {matcherClassName} With{namedProp.Name}(ILogicalMatcher matcher) 
                        {{ 
                            return new {matcherClassName}(this)
                            {{
                                {namedProp.Name} = matcher
                            }};
                        }}");

                        builderMethods.AppendLine($@"
                        public {matcherClassName} With{namedProp.Name}({p.Key.Name}Matcher matcher) 
                        {{ 
                            return new {matcherClassName}(this)
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
                        public {matcherClassName} With{namedProp.Name}({typeof(SyntaxKind).FullName} kind) 
                        {{ 
                            return this.With{namedProp.Name}((TokenMatcher)kind);
                        }}");
                        }

                        builderMethods.AppendLine($@"
                        public {matcherClassName} With{namedProp.Name}({matchType} matcher) 
                        {{ 
                            return new {matcherClassName}(this)
                            {{
                                {namedProp.Name} = matcher
                            }};
                        }}");

                        if (matchType != propertyType)
                        {
                            builderMethods.AppendLine($@"
                        public {matcherClassName} With{namedProp.Name}(ILogicalMatcher matcher) 
                        {{ 
                            return new {matcherClassName}(this)
                            {{
                                {namedProp.Name} = new {propertyType}(matcher)
                            }};
                        }}");
                        }
                    }
                }
            }

            string constructorArgs = string.Join(", ", [.. fields.Select(i => $"{i.TypeName} {i.FieldName.TrimStart('_')} = default")]);

            builder.AppendLine($@"
        public {matcherClassName}({constructorArgs}) : base()
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
            protected {matcherClassName}({matcherClassName} copy) : base(copy)
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

            if (matcherClassName == nameof(SyntaxKind.NumericLiteralExpression))
            {
                foreach (var numeric in NumericTypes)
                {
                    var t = context.Compilation.GetSpecialType(numeric);
                    builderMethods.AppendLine($"public {matcherClassName} WithNumber({t.ToDisplayString()} value) " +
                        "=> WithToken(SyntaxSearch.Framework.Is.Number(value));");
                }
            }

            builder.AppendLine(builderMethods.ToString());

            builder.AppendLine($@"
                protected override bool IsNodeMatch(SyntaxNode node, CaptureStore store) {{
                    if (node is not {syntaxType} obj)
                        return false;");

            if (kindName is not null)
            {
                builder.AppendLine($@"
                    if (!obj.IsKind(SyntaxKind.{kindName}))
                        return false;");
            }

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
                    var castNode = ({syntaxType})node;
");

            foreach ((var namedProp, PropertyKind generatorKind) in namedProperties)
            {
                string localName = $"{namedProp.Name.ToLower()}Node";

                if (generatorKind == PropertyKind.SyntaxKind)
                {
                    builder.AppendLine($@"
                        if ({namedProp.Name}?.IsMatch(castNode, store) == false)
                        {{
                            return false;
                        }}");
                }
                else if (generatorKind == PropertyKind.GenericTokenList)
                {
                    builder.AppendLine(@$"
    if ({namedProp.Name} is not null)
    {{
        var {localName} = castNode.{namedProp.Name};
        if (!{namedProp.Name}.IsMatch({localName}, store))
        {{
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

        private readonly Lazy<string> NewLineLazy = new(() =>
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
                                  IReadOnlyDictionary<IFieldSymbol, INamedTypeSymbol> classes,
                                  GeneratorExecutionContext context)
        {
            string className = classType.Name;

            string constructorArgs = string.Join(", ", [.. fields.Select(i => $"{i.TypeName} {i.FieldName.TrimStart('_')} = default")]);

            StringBuilder builder = new(@$"
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Collections.Immutable;");

            GenerateMatcherClass($"{kind.Name}Matcher", kind.Name,  classType, fields, namedProperties, builder, classes, context);

            return Utilities.Normalize(builder);
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            _receiver = new SyntaxCollector<ClassDeclarationSyntax>();
            context.RegisterForSyntaxNotifications(() => _receiver);
        }

        record PropertyOrField(ITypeSymbol Type,
                               string Name,
                               Accessibility DeclaredAccessibility,
                               bool IsAbstract,
                               bool IsStatic,
                               ImmutableArray<AttributeData> Attributes,
                               bool CanBeReferencedByName,
                               bool IsReadOnly
                               )
        {
            public PropertyOrField(IPropertySymbol p) : this(p.Type,
                                                             p.Name,
                                                             p.DeclaredAccessibility,
                                                             p.IsAbstract,
                                                             p.IsStatic,
                                                             p.GetAttributes(),
                                                             p.CanBeReferencedByName,
                                                             p.SetMethod is null)
            {

            }

            public PropertyOrField(IFieldSymbol f) : this(f.Type,
                                                          f.Name,
                                                          f.DeclaredAccessibility,
                                                          f.IsAbstract,
                                                          f.IsStatic,
                                                          f.GetAttributes(),
                                                          f.CanBeReferencedByName,
                                                          f.IsReadOnly)
            {
            }

            public static PropertyOrField From(ISymbol s)
            {
                return s switch
                {
                    IPropertySymbol p => new(p),
                    IFieldSymbol f => new(f),
                    _ => default
                };
            }
        }
    }
}