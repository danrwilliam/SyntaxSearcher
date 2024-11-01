using Microsoft.CodeAnalysis;
using System;

namespace SyntaxSearch.Matchers
{
    [Exclude]
    public class LogicalOrNodeMatcher<T> : INodeMatcher where T : ExplicitNodeMatcher, INodeMatcher
    {
        private readonly ILogicalMatcher _logical;
        private readonly T _node;

        public LogicalOrNodeMatcher(ILogicalMatcher logical)
        {
            _logical = logical;
        }

        public LogicalOrNodeMatcher(T node)
        {
            _node = node;
        }

        public NodeAccept Accepts { get; set; }

        public bool IsMatch(SyntaxNode node, CaptureStore store)
        {
            INodeMatcher matcher = _logical ?? (INodeMatcher)_node;
            return matcher.IsMatch(node, store);
        }

        public static implicit operator LogicalOrNodeMatcher<T>(T matcher) => new LogicalOrNodeMatcher<T>(matcher);
    }

    public static class MatcherExtensions
    {
        public static LogicalOrNodeMatcher<T> For<T>(this ILogicalMatcher matcher) where T : ExplicitNodeMatcher, INodeMatcher
        {
            return new LogicalOrNodeMatcher<T>(matcher);
        }

        public static LogicalOrNodeMatcher<T> For<T>(this INodeMatcher matcher) where T : ExplicitNodeMatcher, INodeMatcher
        {
            if (matcher is ILogicalMatcher logical)
            {
                return logical.For<T>();
            }
            else if (matcher is T @explicit)
            {
                return @explicit;
            }
            else
            {
                throw new ArgumentException(nameof(matcher), $"matcher is not ILogicalMatcher or {typeof(T).Name}");
            }
        }
    }
}
