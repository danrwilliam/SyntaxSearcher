using Microsoft.CodeAnalysis;

namespace SyntaxSearch.Matchers
{
    /// <summary>
    /// Marker interface
    /// </summary>
    /// <typeparam name="TNode"></typeparam>
    public interface IExplicitNodeMatcher<TNode> : INodeMatcher where TNode : SyntaxNode
    {
    }
}
