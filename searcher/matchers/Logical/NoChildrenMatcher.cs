using Microsoft.CodeAnalysis;

namespace SyntaxSearch.Matchers
{
    /// <summary>
    /// Requires node to have no children
    /// </summary>
    public class NoChildrenMatcher : HasChildrenMatcher
    {
        public override bool IsMatch(SyntaxNode node, CaptureStore store)
        {
            return !base.IsMatch(node, store);
        }
    }
}
