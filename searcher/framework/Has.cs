using SyntaxSearch.Matchers;

namespace SyntaxSearch.Framework
{
    public sealed class HasAttribute(string name = null) : MethodAttribute(name);

    public static partial class Has
    {
        public static AndSyntaxTokenListMatcher Modifiers(ISyntaxTokenListMatcher matcher)
            => new AndSyntaxTokenListMatcher().With(matcher);

        /// <summary>
        /// Creates a SyntaxTokenList matcher that requires all given matchers to mtach
        /// </summary>
        /// <param name="matchers"></param>
        /// <returns></returns>
        public static AndSyntaxTokenListMatcher Modifiers(params ISyntaxTokenListMatcher[] matchers)
            => new AndSyntaxTokenListMatcher().With(matchers);
    }
}
