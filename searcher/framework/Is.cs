using Microsoft.CodeAnalysis.CSharp;
using SyntaxSearch.Matchers;

namespace SyntaxSearch.Framework
{
    public sealed class ExtensionAttribute(string name = null) : MethodAttribute(name);

    /// <summary>
    /// Creates a helper property in <see cref="Is"/> for this type
    /// </summary>
    /// <param name="name"></param>
    public sealed class IsAttribute(string name = null) : MethodAttribute(name);

    public static partial class Is
    {
        public static NotMatcher Not(INodeMatcher matcher) => new NotMatcher(matcher);

        public static TokenMatcher Identifier => TokenMatcher.Default.WithKind(SyntaxKind.IdentifierToken);

        public static ISyntaxTokenListMatcher Public => HasSyntaxTokenListMatcher.Default.Has(SyntaxKind.PublicKeyword);
        public static ISyntaxTokenListMatcher Private => HasSyntaxTokenListMatcher.Default.Has(SyntaxKind.PrivateKeyword);
        public static ISyntaxTokenListMatcher Protected => HasSyntaxTokenListMatcher.Default.Has(SyntaxKind.ProtectedKeyword);
        public static ISyntaxTokenListMatcher Static => HasSyntaxTokenListMatcher.Default.Has(SyntaxKind.StaticKeyword);
        public static ISyntaxTokenListMatcher ReadOnly => HasSyntaxTokenListMatcher.Default.Has(SyntaxKind.ReadOnlyKeyword);
        public static ISyntaxTokenListMatcher Abstract => HasSyntaxTokenListMatcher.Default.Has(SyntaxKind.AbstractKeyword);
        public static ISyntaxTokenListMatcher Internal => HasSyntaxTokenListMatcher.Default.Has(SyntaxKind.InternalKeyword);
        public static ISyntaxTokenListMatcher Record => HasSyntaxTokenListMatcher.Default.Has(SyntaxKind.RecordKeyword);
        public static ISyntaxTokenListMatcher File => HasSyntaxTokenListMatcher.Default.Has(SyntaxKind.FileKeyword);
        public static ISyntaxTokenListMatcher Ref => HasSyntaxTokenListMatcher.Default.Has(SyntaxKind.RefKeyword);
        public static ISyntaxTokenListMatcher Override => HasSyntaxTokenListMatcher.Default.Has(SyntaxKind.OverrideKeyword);
        public static ISyntaxTokenListMatcher Virtual => HasSyntaxTokenListMatcher.Default.Has(SyntaxKind.VirtualKeyword);
    }
}
