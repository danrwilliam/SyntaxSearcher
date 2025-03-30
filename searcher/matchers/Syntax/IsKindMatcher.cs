using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SyntaxSearch.Framework;

namespace SyntaxSearch.Matchers
{
    [Is("Kind")]
    public partial class IsKindMatcher : ExplicitNodeMatcher
    {
        [With]
        public SyntaxKind Kind { get; internal set; }

        [UseConstructor]
        public IsKindMatcher(SyntaxKind kind)
        {
            Kind = kind;
        }

        protected override bool DoChildrenMatch(SyntaxNode node, CaptureStore store) => true;

        protected override bool IsNodeMatch(SyntaxNode node, CaptureStore store)
        {
            return node?.IsKind(Kind) == true;
        }

        public static implicit operator IsKindMatcher(SyntaxKind kind) => new IsKindMatcher(kind);
    }
}
