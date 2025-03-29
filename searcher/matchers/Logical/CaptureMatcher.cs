using Microsoft.CodeAnalysis;
using SyntaxSearch.Framework;

namespace SyntaxSearch.Matchers
{
    [Extension]
    public sealed partial class CaptureMatcher : BaseMatcher, ILogicalMatcher
    {
        [With]
        public INodeMatcher Matcher { get; internal set; }
        [With]
        public string Name { get; internal set; }

        [UseConstructor]
        public CaptureMatcher(INodeMatcher matcher, string name)
        {
            Matcher = matcher;
            Name = name;
        }

        public override bool IsMatch(SyntaxNode node, CaptureStore store)
        {
            if (Matcher?.IsMatch(node, store) is true
                && Name is not null)
            {
                store.CapturedGroups.Add(Name, node);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
