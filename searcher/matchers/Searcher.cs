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
        /// <returns>any node matching the match criteria</returns>
        public IEnumerable<SyntaxNode> Search(SyntaxNode node)
        {
            foreach (var dNode in node.DescendantNodes(c => true))
            {
                Store.Reset();

                if (_match.IsMatch(dNode))
                {
                    yield return dNode;
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="node">root node</param>
        /// <returns>any node matching the match criteria and any additional nodes</returns>
        public IEnumerable<(SyntaxNode, SyntaxNode[])> SearchEx(SyntaxNode node)
        {
            foreach (var dNode in node.DescendantNodes(c => true))
            {
                Store.Reset();

                if (_match.IsMatch(dNode))
                {
                    yield return (dNode, Store.AdditionalCaptures.ToArray());
                }
            }
        }
    }
}
