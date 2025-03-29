using Microsoft.CodeAnalysis;
using SyntaxSearch.Framework;

namespace SyntaxSearch.Matchers
{
    [Is]
    public sealed class NullMatcher : LogicalMatcher
    {
        public override bool IsMatch(SyntaxNode node, CaptureStore store)
        {
            return node is null;
        }
    }
}
