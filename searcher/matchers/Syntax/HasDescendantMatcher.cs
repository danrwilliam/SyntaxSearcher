using Microsoft.CodeAnalysis;
using SyntaxSearch.Framework;

namespace SyntaxSearch.Matchers
{
    public abstract class CompoundMatcher : LogicalMatcher
    {
        public ISyntaxNodeMatcher NodeMatcher { get; }

        protected CompoundMatcher(ISyntaxNodeMatcher nodeMatcher)
        {
            NodeMatcher = nodeMatcher;
        }

        protected CompoundMatcher(CompoundMatcher copy) : this(copy.NodeMatcher)
        {
        }

        public sealed override bool IsMatch(SyntaxNode node, CaptureStore store)
        {
            return NodeMatcher.IsMatch(node, store) && IsCompoundMatch(node, store);
        }

        protected abstract bool IsCompoundMatch(SyntaxNode node, CaptureStore store);
    }

    [Extension]
    public partial class HasDescendantMatcher : CompoundMatcher
    {
        public INodeMatcher DescendantMatcher { get; }

        [UseConstructor]
        public HasDescendantMatcher(ISyntaxNodeMatcher matcher, INodeMatcher descendant) : base(matcher)
        {
            DescendantMatcher = descendant;
        }

        protected override bool IsCompoundMatch(SyntaxNode node, CaptureStore store)
        {
            foreach (var descendant in node.DescendantNodes(f => true))
            {
                if (DescendantMatcher.IsMatch(descendant, store))
                {
                    return true;
                }
            }
            return false;
        }
    }

    [Extension]
    public partial class HasAncestorMatcher : CompoundMatcher
    {
        public INodeMatcher AncestorMatcher { get; }

        [UseConstructor]
        public HasAncestorMatcher(
            ISyntaxNodeMatcher matcher, 
            INodeMatcher descendant) : base(matcher)
        {
            AncestorMatcher = descendant;
        }

        protected override bool IsCompoundMatch(SyntaxNode node, CaptureStore store)
        {
            foreach (var ancestor in node.Ancestors())
            {
                if (AncestorMatcher.IsMatch(ancestor, store))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
