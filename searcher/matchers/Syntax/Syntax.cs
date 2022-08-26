using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyntaxSearch.Matchers
{
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

        public override bool IsMatch(SyntaxNode node)
        {
            if (!IsNodeMatch(node))
                return false;

            if (!string.IsNullOrEmpty(_matchName)
                && Store.CapturedGroups.TryGetValue(_matchName, out var compareToNode))
            {
                return CompareToCapturedNode(node, compareToNode);
            }

            return DoChildrenMatch(node);
        }

        protected virtual bool CompareToCapturedNode(SyntaxNode node, SyntaxNode compareToNode)
        {
            return SyntaxFactory.AreEquivalent(node, compareToNode);
        }

        protected virtual bool IsNodeMatch(SyntaxNode node)
        {
            return node.IsKind(_thisKind);
        }

        protected virtual bool DoChildNodesMatch(SyntaxNode node)
        {
            IEnumerable<SyntaxNode> childNodes = node.ChildNodes();

            var matchers = Children.Where(c => c.Accepts == NodeAccept.Child);

            // if there aren't any child matchers, treat that as a match
            if (!matchers.Any())
                return true;

            // should be same number of matchers as child nodes 
            if (matchers.Count() != childNodes.Count())
                return false;

            var zipped = Enumerable.Zip(matchers, childNodes, (m, c) => new { matcher = m, childNode = c });
            foreach (var step in zipped)
            {
                if (!step.matcher.IsMatch(step.childNode))
                    return false;

            }

            return true;
        }

        protected virtual bool DoChildrenMatch(SyntaxNode node)
        {
            // Run any checkers that operate on the current node
            foreach (var check in Children.Where(c => c.Accepts == NodeAccept.Node))
            {
                if (!check.IsMatch(node))
                    return false;
            }

            if (!DoChildNodesMatch(node))
            {
                return false;
            }

            // run any-post condition checkers
            foreach (var post in Children.Where(c => c.Accepts == NodeAccept.PostNode))
            {
                if (!post.IsMatch(node))
                    return false;
            }

            if (!string.IsNullOrWhiteSpace(_captureName))
            {
                Store.CapturedGroups.Add(_captureName, node);
            }

            return true;
        }
    }

    public abstract class ExplicitNodeMatcher : NodeMatcher
    {
        protected ExplicitNodeMatcher(SyntaxKind kind, string captureName, string matchName) : base(kind, captureName, matchName)
        {
        }

        protected abstract override bool DoChildNodesMatch(SyntaxNode node);
    }
}
