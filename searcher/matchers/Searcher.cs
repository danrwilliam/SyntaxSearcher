using Microsoft.CodeAnalysis;
using SyntaxSearch.Matchers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SyntaxSearch
{
    /// <summary>
    /// Searches a SyntaxNode tree for matches
    /// </summary>
    public class Searcher
    {
        private readonly INodeMatcher _match;

        public INodeMatcher Matcher => _match;

        /// <summary>
        /// </summary>
        /// <param name="root">match object</param>
        public Searcher(INodeMatcher root)
        {
            _match = root;
        }


        public string ToTreeString()
        {
            return _match.ToTreeString();
        }

        /// <summary>
        /// </summary>
        /// <param name="node">root node</param>
        /// <returns>result object with the matching node, additional node, and any captured nodes</returns>
        public IEnumerable<SearchResult> Search(SyntaxNode node)
        {
            CaptureStore store = new CaptureStore();

            // check passed in node first
            if (_match.IsMatch(node, store))
            {
                var result = new SearchResult(store.Override ?? node, store.AdditionalCaptures, store.CapturedGroups);
                yield return result;
                store.Reset();
            }

            foreach (var dNode in node.DescendantNodes(c => true))
            {
                store.Reset();

                if (_match.IsMatch(dNode, store))
                {
                    var result = new SearchResult(store.Override ?? dNode, store.AdditionalCaptures, store.CapturedGroups);
                    yield return result;
                    store.Reset();
                }
            }
        }
    }

    public class SearchResult
    {
        /// <summary>
        /// Node that matches the search criteria
        /// </summary>
        public SyntaxNode Node { get; private set; }

        /// <summary>
        /// Additional nodes that were found by the search criteria
        /// </summary>
        public SyntaxNode[] AdditionalNodes { get; private set; }

        /// <summary>
        /// Named capture nodes
        /// </summary>
        public Dictionary<string, SyntaxNode> Captured { get; private set; }

        /// <summary>
        /// Create a new SearchResult object
        /// </summary>
        /// <param name="node"></param>
        /// <param name="additional">this argument is cloned with a shallow copy</param>
        /// <param name="captures">this argument is cloned with a shallow copy</param>
        public SearchResult(SyntaxNode node,
                            IEnumerable<SyntaxNode> additional,
                            Dictionary<string, SyntaxNode> captures)
        {
            Node = node;
            AdditionalNodes = additional.Any() ? additional.ToArray() : Array.Empty<SyntaxNode>();
            Captured = new Dictionary<string, SyntaxNode>(captures);
        }
    }
}
