using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace SyntaxSearch.Matchers
{
    public abstract class SyntaxListMatcherBase : ISyntaxListMatcher
    {
        public abstract bool IsMatch<T>(IReadOnlyList<T> list, CaptureStore store) where T : SyntaxNode;
    }

    public static class SyntaxList
    {
        public static SyntaxListLengthMatcher Length()
        {
            return SyntaxListLengthMatcher.Instance;
        }

        public static SyntaxListLengthMatcher IsEmpty()
        {
            return Length() == 0;
        }

        public static SyntaxListLengthMatcher NotEmpty()
        {
            return Length() != 0;
        }
    }
}
