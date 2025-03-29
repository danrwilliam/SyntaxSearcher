using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SyntaxSearch.Matchers
{
    public abstract class ExplicitNodeMatcher : BaseMatcher
    {
        protected ExplicitNodeMatcher(ExplicitNodeMatcher copy) : base(copy)
        {
        }

        protected ExplicitNodeMatcher() : base()
        {
        }

        public sealed override bool IsMatch(SyntaxNode node, CaptureStore store)
        {
            if (!IsNodeMatch(node, store))
                return false;

            return DoChildrenMatch(node, store);
        }

        protected abstract bool IsNodeMatch(SyntaxNode node, CaptureStore store);

        protected abstract bool DoChildrenMatch(SyntaxNode node, CaptureStore store);
    }
}
