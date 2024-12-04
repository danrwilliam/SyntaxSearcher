using Microsoft.CodeAnalysis.CSharp;
using SyntaxSearch.Matchers;

namespace SyntaxSearch.Framework
{
    public sealed class NotAttribute(string name = null) : MethodAttribute(name);

    public static partial class Not
    {
        public static ISyntaxTokenListMatcher Public => DoesNotHaveSyntaxTokenListMatcher.Default.NoneAre(SyntaxKind.PublicKeyword);
        public static ISyntaxTokenListMatcher Private => DoesNotHaveSyntaxTokenListMatcher.Default.NoneAre(SyntaxKind.PrivateKeyword);
        public static ISyntaxTokenListMatcher Protected => DoesNotHaveSyntaxTokenListMatcher.Default.NoneAre(SyntaxKind.ProtectedKeyword);
        public static ISyntaxTokenListMatcher Static => DoesNotHaveSyntaxTokenListMatcher.Default.NoneAre(SyntaxKind.StaticKeyword);
        public static ISyntaxTokenListMatcher ReadOnly => DoesNotHaveSyntaxTokenListMatcher.Default.NoneAre(SyntaxKind.ReadOnlyKeyword);
        public static ISyntaxTokenListMatcher Abstract => DoesNotHaveSyntaxTokenListMatcher.Default.NoneAre(SyntaxKind.AbstractKeyword);
        public static ISyntaxTokenListMatcher Internal => DoesNotHaveSyntaxTokenListMatcher.Default.NoneAre(SyntaxKind.InternalKeyword);
        public static ISyntaxTokenListMatcher Record => DoesNotHaveSyntaxTokenListMatcher.Default.NoneAre(SyntaxKind.RecordKeyword);
    }
}
