using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SyntaxSearch.Framework;
using System.Collections.Generic;
using System.Linq;

namespace SyntaxSearch.Matchers
{
    public class TokenMatcher
    {
        private SyntaxKind _kind;

        public TokenMatcher(SyntaxKind kind)
        {
            _kind = kind;
        }

        public bool IsMatch(SyntaxToken token)
        {
            return token.IsKind(_kind);
        }
    }

    public abstract class NodeMatcher : BaseMatcher
    {
        protected SyntaxKind _thisKind;
        protected string _captureName;
        protected string _matchName;

        protected NodeMatcher(SyntaxKind kind, string captureName, string matchName)
        {
            _thisKind = kind;
            _captureName = captureName;
            _matchName = matchName;
        }

        public override bool IsMatch(SyntaxNode node, CaptureStore store)
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

        protected virtual bool IsNodeMatch(SyntaxNode node, CaptureStore store)
        {
            return node.IsKind(_thisKind);
        }

        protected virtual bool DoChildNodesMatch(SyntaxNode node, CaptureStore store)
        {
            IEnumerable<SyntaxNode> childNodes = node.ChildNodes();

            var matchers = Children.Where(c => c.Accepts == NodeAccept.Child);

            // if there aren't any child matchers, treat that as a match
            if (!matchers.Any())
                return true;

            // should be same number of matchers as child nodes 
            if (matchers.Count() != childNodes.Count())
                return false;

            var zipped = matchers.Zip(childNodes, (m, c) => (m ,c));
            foreach (var (matcher, childNode) in zipped)
            {
                if (!matcher.IsMatch(childNode, store))
                    return false;

            }

            return true;
        }

        protected virtual bool DoChildrenMatch(SyntaxNode node, CaptureStore store)
        {
            // Run any checkers that operate on the current node
            foreach (var check in Children.Where(c => c.Accepts == NodeAccept.Node))
            {
                if (!check.IsMatch(node, store))
                    return false;
            }

            if (!DoChildNodesMatch(node, store))
            {
                return false;
            }

            // run any-post condition checkers
            foreach (var post in Children.Where(c => c.Accepts == NodeAccept.PostNode))
            {
                if (!post.IsMatch(node, store))
                    return false;
            }

            if (!string.IsNullOrWhiteSpace(_captureName))
            {
                store.CapturedGroups.Add(_captureName, node);
            }

            return true;
        }
    }

    public abstract class ExplicitNodeMatcher : NodeMatcher
    {
        protected ExplicitNodeMatcher(SyntaxKind kind, string captureName, string matchName) : base(kind, captureName, matchName)
        {
        }

        protected ExplicitNodeMatcher(ExplicitNodeMatcher copy) : base(copy._thisKind, copy._captureName, copy._matchName)
        {

        }

        protected abstract override bool DoChildNodesMatch(SyntaxNode node, CaptureStore store);
    }

    [Is]
    public partial class BaseAccessExpressionMatcher : BaseMatcher
    {
        public override bool IsMatch(SyntaxNode node, CaptureStore store)
        {
            if (node is MemberAccessExpressionSyntax member)
            {
                var left = member.Expression;
                while (left is MemberAccessExpressionSyntax m)
                {
                    left = m;
                }

                return left is BaseExpressionSyntax;
            }
            else
            {
                return false;
            }
        }
    }

    [Is]
    public partial class ThisAccessExpressionMatcher : BaseMatcher
    {
        public override bool IsMatch(SyntaxNode node, CaptureStore store)
        {
            if (node is MemberAccessExpressionSyntax member)
            {
                var left = member.Expression;
                while (left is MemberAccessExpressionSyntax m)
                {
                    left = m;
                }

                return left is ThisExpressionSyntax;
            }
            else
            {
                return false;
            }
        }
    }
}
