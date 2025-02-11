using Microsoft.CodeAnalysis;
using SyntaxSearch.Framework;

namespace SyntaxSearch.Matchers
{
    [Has("Descendant")]
    public partial class HasDescendantMatcher : LogicalMatcher
    {
        public INodeMatcher Matcher { get; }

        [UseConstructor]
        public HasDescendantMatcher(INodeMatcher matcher)
        {
            Matcher = matcher;
        }

        public override bool IsMatch(SyntaxNode node, CaptureStore store)
        {
            foreach (var descendant in node.DescendantNodes(f => true))
            {
                if (Matcher.IsMatch(descendant, store))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
