using Microsoft.CodeAnalysis;

namespace SyntaxSearch.Matchers
{
    public interface ISyntaxNodeMatcher : INodeMatcher
    {
    }

    /// <summary>
    /// Marker interface
    /// </summary>
    /// <typeparam name="TNode"></typeparam>
    public interface IExplicitNodeMatcher<TNode> : ISyntaxNodeMatcher where TNode : SyntaxNode
    {
    }
}
