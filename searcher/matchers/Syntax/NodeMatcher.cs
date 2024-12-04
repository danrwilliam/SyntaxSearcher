using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace SyntaxSearch.Matchers
{
    public abstract class NodeMatcher : BaseMatcher, ITreeWalkNodeMatcher, IEnumerable<INodeMatcher>
    {
        protected SyntaxKind _thisKind;
        protected string _captureName;
        protected string _matchName;

        public ImmutableArray<INodeMatcher> Children { get; private set; } = [];

        protected NodeMatcher(SyntaxKind kind, string captureName, string matchName)
        {
            _thisKind = kind;
            _captureName = captureName;
            _matchName = matchName;
        }

        public void Add(INodeMatcher matcher)
        {
            Children = Children.Add(matcher);
        }

        public void AddChild(INodeMatcher matcher)
        {
            Children = Children.Add(matcher);
        }

        public IEnumerator<INodeMatcher> GetEnumerator()
        {
            return Children.AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Children.AsEnumerable().GetEnumerator();
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
            ImmutableArray<SyntaxNode> childNodes = [.. node.ChildNodes()];
            ImmutableArray<INodeMatcher> childMatchers = [..Children.Where(c => c.Accepts == NodeAccept.Child)];

            // if there aren't any child matchers, treat that as a match
            if (childMatchers.IsEmpty)
                return true;

            // should be same number of matchers as child nodes 
            if (childMatchers.Length != childNodes.Length)
                return false;

            var zipped = childMatchers.Zip(childNodes, (m, c) => (m ,c));
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
}
