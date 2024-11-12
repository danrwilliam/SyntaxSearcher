using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SyntaxSearch.Matchers;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SyntaxSearch
{
    public class Rewriter(INodeMatcher matcher, Func<SearchResult, SyntaxNode> computeReplacement)
    {
        private readonly INodeMatcher _matcher = matcher;
        private readonly Func<SearchResult, SyntaxNode> _computeReplacement = computeReplacement;

        public T Rewrite<T>(T node) where T : SyntaxNode
        {
            List<(SyntaxNode, SyntaxNode)> replacements = [];

            foreach (var result in _matcher.Search(node))
            {
                var replacement = _computeReplacement(result);
                replacements.Add((result.Node, replacement));
            }

            T tracked = node.TrackNodes(replacements.Select(n => n.Item1));
            foreach ((var original, var replacement) in replacements)
            {
                var current = tracked.GetCurrentNode(original);
                tracked = tracked.ReplaceNode(current, replacement);
            }

            return tracked;
        }
    }
}
