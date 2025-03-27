using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SyntaxSearch.Framework;
using System.Collections.Immutable;

namespace SyntaxSearch.Matchers
{
    public class HasSyntaxTokenListMatcher : ISyntaxTokenListMatcher
    {
        private ImmutableHashSet<SyntaxKind> _hasKinds = [];

        public static HasSyntaxTokenListMatcher Default { get; } = new();

        public HasSyntaxTokenListMatcher Has(SyntaxKind kind)
        {
            var copy = new HasSyntaxTokenListMatcher
            {
                _hasKinds = _hasKinds.Add(kind)
            };
            return copy;
        }

        public bool IsMatch(SyntaxTokenList list, CaptureStore store)
        {
            if (!_hasKinds.IsEmpty)
            {
                foreach (var l in list)
                {
                    if (_hasKinds.Contains(l.Kind()))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }

    public class DoesNotHaveSyntaxTokenListMatcher : ISyntaxTokenListMatcher
    {
        private ImmutableHashSet<SyntaxKind> _isNotKind = [];

        public static DoesNotHaveSyntaxTokenListMatcher Default { get; } = new();

        public DoesNotHaveSyntaxTokenListMatcher NoneAre(SyntaxKind kind)
        {
            var copy = new DoesNotHaveSyntaxTokenListMatcher
            {
                _isNotKind = _isNotKind.Add(kind)
            };
            return copy;
        }

        public bool IsMatch(SyntaxTokenList list, CaptureStore store)
        {
            if (!_isNotKind.IsEmpty)
            {
                foreach (var l in list)
                {
                    if (_isNotKind.Contains(l.Kind()))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }

    public sealed class AndSyntaxTokenListMatcher : ISyntaxTokenListMatcher
    {
        private ImmutableHashSet<ISyntaxTokenListMatcher> _matchers = [];

        public AndSyntaxTokenListMatcher With(params ISyntaxTokenListMatcher[] matcher)
        {
            return new AndSyntaxTokenListMatcher
            {
                _matchers = _matchers.Union(matcher)
            };
        }

        public AndSyntaxTokenListMatcher With(ISyntaxTokenListMatcher matcher)
        {
            return new AndSyntaxTokenListMatcher
            {
                _matchers = _matchers.Add(matcher)
            };
        }

        public bool IsMatch(SyntaxTokenList list, CaptureStore store)
        {
            foreach (var t in _matchers)
            {
                if (!t.IsMatch(list, store))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
