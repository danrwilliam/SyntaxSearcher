using SyntaxSearch.Framework;

namespace SyntaxSearch.Matchers.Explicit
{
    public partial class IdentifierNameMatcher
    {
        public IdentifierNameMatcher WithName(string name)
        {
            return WithIdentifier(Is.Identifier.WithText(name));
        }
    }
}
