using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace SyntaxSearcher.Generators
{
    /// <summary>
    /// Collects all nodes of type <typeparamref name="TNode"/> for later
    /// processing in generator
    /// </summary>
    /// <typeparam name="TNode"><see cref="SyntaxNode"/> type to collect</typeparam>
    public class SyntaxCollector<TNode> : ISyntaxReceiver where TNode : SyntaxNode
    {
        /// <summary>
        /// Collected syntax nodes
        /// </summary>
        public List<TNode> Collected { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is TNode nodeType)
            {
                Collected.Add(nodeType);
            }
        }
    }
}