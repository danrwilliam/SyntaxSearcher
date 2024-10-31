using Microsoft.CodeAnalysis;
using SyntaxSearch.Matchers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SyntaxSearch
{
    public static class Extensions
    {
        public static IEnumerable<SearchResult> Search(this INodeMatcher matcher, SyntaxNode node)
        {
            var searcher = new Searcher(matcher);
            return searcher.Search(node);
        }

        public static bool IsMatch(this INodeMatcher matcher, SyntaxNode node)
        {
            var searcher = new Searcher(matcher);
            return searcher.IsMatch(node);
        }
    }

    /// <summary>
    /// Searches a SyntaxNode tree for matches
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <param name="matcher">match object</param>
    public class Searcher(INodeMatcher matcher)
    {
        public INodeMatcher Matcher { get; } = matcher;

        /// <summary>
        /// </summary>
        /// <param name="node">root node</param>
        /// <returns>result object with the matching node, additional node, and any captured nodes</returns>
        public IEnumerable<SearchResult> Search(SyntaxNode node)
        {
            CaptureStore store = new CaptureStore();

            // check passed in node first
            if (Matcher.IsMatch(node, store))
            {
                var result = new SearchResult(store.Override ?? node, store.AdditionalCaptures, store.CapturedGroups);
                yield return result;
                store.Reset();
            }

            foreach (var dNode in node.DescendantNodes(c => true))
            {
                store.Reset();

                if (Matcher.IsMatch(dNode, store))
                {
                    var result = new SearchResult(store.Override ?? dNode, store.AdditionalCaptures, store.CapturedGroups);
                    yield return result;
                    store.Reset();
                }
            }
        }

        /// <summary>
        /// Checks if the given node is a match
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public bool IsMatch(SyntaxNode node) => Matcher.IsMatch(node, new());
    }

    /// <summary>
    /// Create a new SearchResult object
    /// </summary>
    /// <param name="node"></param>
    /// <param name="additional">this argument is cloned with a shallow copy</param>
    /// <param name="captures">this argument is cloned with a shallow copy</param>
    public class SearchResult(SyntaxNode node,
                              IEnumerable<SyntaxNode> additional,
                              Dictionary<string, SyntaxNode> captures)
    {
        /// <summary>
        /// Node that matches the search criteria
        /// </summary>
        public SyntaxNode Node { get; } = node;

        /// <summary>
        /// Additional nodes that were found by the search criteria
        /// </summary>
        public SyntaxNode[] AdditionalNodes { get; } = additional.Any() ? additional.ToArray() : [];

        /// <summary>
        /// Named capture nodes
        /// </summary>
        public Dictionary<string, SyntaxNode> Captured { get; } = new Dictionary<string, SyntaxNode>(captures);
    }
}
