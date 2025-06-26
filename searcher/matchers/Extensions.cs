using Microsoft.CodeAnalysis;
using SyntaxSearch.Matchers;
using System;
using System.Collections.Generic;

namespace SyntaxSearch
{
    public static class Extensions
    {
        /// <summary>
        /// Runs the matcher against the given node
        /// </summary>
        /// <param name="matcher"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        public static IEnumerable<SearchResult> Search(this INodeMatcher matcher, SyntaxNode node)
        {
            var searcher = new Searcher(matcher);
            return searcher.Search(node);
        }

        /// <summary>
        /// Does the matcher match the given node?
        /// </summary>
        /// <param name="matcher"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        public static bool IsMatch(this INodeMatcher matcher, SyntaxNode node)
        {
            var searcher = new Searcher(matcher);
            return searcher.IsMatch(node);
        }

        /// <summary>
        /// Does the matcher match the given node?
        /// </summary>
        /// <param name="matcher"></param>
        /// <param name="node"></param>
        /// <param name="result">capture information if matched</param>
        /// <returns></returns>
        public static bool TryMatch(this INodeMatcher matcher, SyntaxNode node, out SearchResult result)
        {
            var searcher = new Searcher(matcher);
            return searcher.TryMatch(node, out result);
        }

        /// <summary>
        /// Uses the given matcher to find all matches, then calls
        /// <paramref name="computeReplacement"/> delegate to retrieve
        /// the modified node.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="matcher"></param>
        /// <param name="computeReplacement">called for each matched node</param>
        /// <param name="node">node to modify</param>
        /// <returns>modified node</returns>
        public static T Rewrite<T>(this INodeMatcher matcher, Func<SearchResult, SyntaxNode> computeReplacement, T node)
            where T : SyntaxNode
        {
            var rewriter = new Rewriter(matcher, computeReplacement);
            return rewriter.Rewrite<T>(node);
        }

        public static ISyntaxTokenListMatcher Merge(this ISyntaxTokenListMatcher left, ISyntaxTokenListMatcher right)
        {
            if (left is null)
            {
                return right;
            }
            else
            {
                return new CombinedSyntaxTokenListMatcher(left, right);
            }
        }
    }
}
