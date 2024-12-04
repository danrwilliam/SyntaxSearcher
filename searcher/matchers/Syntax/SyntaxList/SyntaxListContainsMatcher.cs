using Microsoft.CodeAnalysis;
using SyntaxSearch.Framework;
using System.Collections.Generic;

namespace SyntaxSearch.Matchers
{
    [Does("Contain")]
    public sealed class SyntaxListContainsMatcher : SyntaxListMatcherBase
    {
        public INodeMatcher Element { get; }

        [UseConstructor]
        public SyntaxListContainsMatcher(INodeMatcher element)
        {
            Element = element;
        }

        public SyntaxListContainsMatcher WithElement(ILogicalMatcher matcher) => new(matcher);

        public SyntaxListContainsMatcher WithElement<TNode>(IExplicitNodeMatcher<TNode> matcher) where TNode: SyntaxNode => new(matcher);

        public sealed override bool IsMatch<TNode>(IReadOnlyList<TNode> list, CaptureStore store)
        {
            if (Element is null)
            {
                return false;
            }

            foreach (var element in list)
            {
                if (Element.IsMatch(element, store))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
