using Microsoft.CodeAnalysis;
using SyntaxSearch.Framework;

namespace SyntaxSearch.Matchers
{
    [Has("Descendant")]
    public partial class HasDescendantMatcher : LogicalMatcher
    {
        private readonly INodeMatcher _matcher;

        [UseConstructor]
        public HasDescendantMatcher(INodeMatcher matcher)
        {
            _matcher = matcher;
        }

        public override bool IsMatch(SyntaxNode node, CaptureStore store)
        {
            foreach (var descendant in node.DescendantNodes(f => true))
            {
                if (_matcher.IsMatch(descendant, store))
                {
                    return true;
                }
            }
            return false;
        }
    }

    [Has("Ancestor")]
    public partial class HasAncestorMatcher : LogicalMatcher
    {
        public INodeMatcher Matcher { get; }

        [UseConstructor]
        public HasAncestorMatcher(INodeMatcher matcher)
        {
            Matcher = matcher;
        }

        public override bool IsMatch(SyntaxNode node, CaptureStore store)
        {
            foreach (var ancestor in  node.Ancestors())
            {
                if (Matcher.IsMatch(ancestor, store))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
