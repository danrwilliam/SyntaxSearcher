using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace SyntaxSearch.Matchers
{
    public sealed class SyntaxListLengthMatcher : SyntaxListMatcherBase
    {
        private readonly Op _matcher;

        private enum Operation
        {
            MatchAll,
            Equal,
            NotEqual,
            LessThan,
            GreaterThan,
            LessThanEqual,
            GreaterThanEqual
        }

        private record struct Op(Operation Operation, int Value)
        {
            public static Op MatchAll { get; } = new Op(Operation.MatchAll, -1);

            public readonly bool Evaluate<T>(IReadOnlyList<T> list) where T : SyntaxNode
            {
                return Operation switch
                {
                    Operation.MatchAll => true,
                    Operation.Equal => list.Count == Value,
                    Operation.NotEqual => list.Count != Value,
                    Operation.LessThan => list.Count < Value,
                    Operation.GreaterThan => list.Count > Value,
                    Operation.LessThanEqual => list.Count <= Value,
                    Operation.GreaterThanEqual => list.Count >= Value,
                    _ => true,
                };
            }
        }

        internal static readonly SyntaxListLengthMatcher Instance = new SyntaxListLengthMatcher();

        private SyntaxListLengthMatcher()
        {
            _matcher = Op.MatchAll;
        }

        private SyntaxListLengthMatcher(Op matcher)
        {
            _matcher = matcher;
        }

        public override bool IsMatch<T>(IReadOnlyList<T> list, CaptureStore store)
        {
            return _matcher.Evaluate(list);
        }

        private SyntaxListLengthMatcher With(Op operation) => new(operation);

        public override bool Equals(object obj)
        {
            if (obj is not SyntaxListLengthMatcher other)
            {
                return false;
            }

            return _matcher == other._matcher;
        }

        public override int GetHashCode()
        {
            return _matcher.GetHashCode();
        }

        public static SyntaxListLengthMatcher operator ==(SyntaxListLengthMatcher matcher, int length)
        {
            return matcher.With(new Op(Operation.Equal, length));
        }

        public static SyntaxListLengthMatcher operator !=(SyntaxListLengthMatcher matcher, int length)
        {
            return matcher.With(new Op(Operation.NotEqual, length));
        }

        public static SyntaxListLengthMatcher operator <(SyntaxListLengthMatcher matcher, int length)
        {
            return matcher.With(new Op(Operation.LessThan, length));
        }

        public static SyntaxListLengthMatcher operator >(SyntaxListLengthMatcher matcher, int length)
        {
            return matcher.With(new Op(Operation.GreaterThan, length));
        }

        public static SyntaxListLengthMatcher operator <=(SyntaxListLengthMatcher matcher, int length)
        {
            return matcher.With(new Op(Operation.LessThanEqual, length));
        }

        public static SyntaxListLengthMatcher operator >=(SyntaxListLengthMatcher matcher, int length)
        {
            return matcher.With(new Op(Operation.GreaterThanEqual, length));
        }
    }
}
