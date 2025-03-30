using Microsoft.CodeAnalysis;
using System.Linq;

namespace SyntaxSearch.Matchers
{
    /// <summary>
    /// Requires node to have children
    /// </summary>
    public class HasChildrenMatcher : LogicalMatcher
    {
        public override bool IsMatch(SyntaxNode node, CaptureStore store)
        {
            return node.ChildNodes().Any();
        }
    }
}
