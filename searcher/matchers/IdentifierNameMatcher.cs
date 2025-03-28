using SyntaxSearch.Framework;

namespace SyntaxSearch.Matchers.Explicit
{
    public partial class IdentifierNameMatcher
    {
        public IdentifierNameMatcher WithText(string name)
        {
            return WithIdentifier(Is.Identifier.WithText(name));
        }

        public static implicit operator IdentifierNameMatcher(string name) => new IdentifierNameMatcher().WithText(name);
    }
}
