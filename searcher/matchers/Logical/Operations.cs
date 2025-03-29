using System;
using System.Collections.Immutable;

namespace SyntaxSearch.Matchers
{
    /// <summary>
    /// Generate a With method for marked field
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    internal sealed class WithAttribute : Attribute;

    public abstract class LogicalMatcher : BaseMatcher, ILogicalMatcher
    {
        protected LogicalMatcher(BaseMatcher copy) : base(copy)
        {
        }

        protected LogicalMatcher()
        {
        }
    }

    public abstract class MultipleOperandLogicalMatcher : LogicalMatcher, ICompoundLogicalMatcher
    {
        public ImmutableArray<INodeMatcher> Matchers { get; private set; } = [];

        protected MultipleOperandLogicalMatcher(MultipleOperandLogicalMatcher copy) : base(copy)
        {
            Matchers = copy.Matchers;
        }

        protected MultipleOperandLogicalMatcher(params INodeMatcher[] matchers)
        {
            Matchers = Matchers.AddRange(matchers);
        }

        internal void AddChild(INodeMatcher matcher)
        {
            Matchers = Matchers.Add(matcher);
        }
    }

    /// <summary>
    /// A matcher that doesn't operate directly on the syntax node
    /// </summary>
    public interface ILogicalMatcher : INodeMatcher { }

    /// <summary>
    /// A matcher that contains a collection of matchers to attempt
    /// to match on the given syntax node
    /// </summary>
    public interface ICompoundLogicalMatcher : ILogicalMatcher
    {
        public ImmutableArray<INodeMatcher> Matchers { get; }
    }
}
