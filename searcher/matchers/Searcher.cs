using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SyntaxSearch.Matchers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyntaxSearch
{
    /// <summary>
    /// Searches a SyntaxNode tree for matches
    /// </summary>
    public class Searcher
    {
        private INodeMatcher _match;
        public CaptureStore Store { get; protected set; }

        public INodeMatcher Matcher => _match;

        /// <summary>
        /// </summary>
        /// <param name="root">match object</param>
        public Searcher(INodeMatcher root)
        {
            _match = root;
            Store = _match.Store;
        }

        public Searcher(INodeMatcher root, CaptureStore store)
        {
            _match = root;
            Store = store; 
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
            foreach (var dNode in node.DescendantNodes(c => true))
            {
                Store.Reset();

                if (_match.IsMatch(dNode))
                {
                    var result = new SearchResult(dNode,
                        Store.AdditionalCaptures,
                        Store.CapturedGroups);

                    yield return result;
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

        public Dictionary<string, SyntaxNode> Captured { get; private set; }

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
