using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SyntaxSearch.Matchers
{
    public abstract class ExplicitNodeMatcher(string captureName, string matchName) : BaseMatcher
    {
        private readonly string _captureName = captureName;
        private readonly string _matchName = matchName;

        protected ExplicitNodeMatcher(ExplicitNodeMatcher copy) : this(copy._captureName, copy._matchName)
        {
        }

        protected ExplicitNodeMatcher() : this(null, null)
        {
        }

        public sealed override bool IsMatch(SyntaxNode node, CaptureStore store)
        {
            if (!IsNodeMatch(node, store))
                return false;

            if (!string.IsNullOrEmpty(_matchName)
                && store.CapturedGroups.TryGetValue(_matchName, out var compareToNode))
            {
                return CompareToCapturedNode(node, compareToNode);
            }

            return DoChildrenMatch(node, store);
        }

        protected virtual bool CompareToCapturedNode(SyntaxNode node, SyntaxNode compareToNode)
        {
            return SyntaxFactory.AreEquivalent(node, compareToNode);
        }

        protected abstract bool IsNodeMatch(SyntaxNode node, CaptureStore store);

        protected abstract bool DoChildrenMatch(SyntaxNode node, CaptureStore store);
    }
}
