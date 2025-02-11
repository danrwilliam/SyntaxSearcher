using System.Collections.Generic;
using System.Collections.Immutable;

namespace SyntaxSearch.Matchers
{
    public class SyntaxListEqualTo : SyntaxListMatcherBase
    {
        public ImmutableArray<INodeMatcher> Elements { get; } = ImmutableArray.Create<INodeMatcher>();

        public SyntaxListEqualTo(INodeMatcher matcher)
        {
            Elements = [matcher];
        }

        public SyntaxListEqualTo(params INodeMatcher[] matchers)
        {
            Elements = [.. matchers];
        }

        public override bool IsMatch<T>(IReadOnlyList<T> list, CaptureStore store)
        {
            if (list.Count != Elements.Length)
            {
                return false;
            }
            for (int i = 0; i < list.Count; i++)
            {
                if (!Elements[i].IsMatch(list[i], store))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
