﻿using Microsoft.CodeAnalysis;
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
                return SyntaxFactory.AreEquivalent(compareToNode, node);
            }

            return DoChildrenMatch(node);
        }

        protected virtual bool IsNodeMatch(SyntaxNode node)
        {
            return node.IsKind(_thisKind);
        }

        protected bool DoChildrenMatch(SyntaxNode node)
        {
            // Run any checkers that operate on the current node
            foreach (var check in Children.Where(c => c.Accepts == NodeAccept.Node))
            {
                if (!check.IsMatch(node))
                    return false;
            }

            var childNodeEnumerator = node.ChildNodes().GetEnumerator();
            
            childNodeEnumerator.MoveNext();

            // now run checkers for this node's children
            foreach (var childCheck in Children.Where(c => c.Accepts == NodeAccept.Child))
            {
                var child = childNodeEnumerator.Current;

                if (!childCheck.IsMatch(child))
                    return false;

                childNodeEnumerator.MoveNext();
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
}
