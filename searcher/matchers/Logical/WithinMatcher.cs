using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;

namespace SyntaxSearch.Matchers
{
    //[Is]
    public partial class WithinMatcher : BaseMatcher
    {
        public override NodeAccept Accepts { get => NodeAccept.Node; set { } }
        private SyntaxKind _scopeKind;

        public override bool IsMatch(SyntaxNode node, CaptureStore store)
        {
            return node.Ancestors().FirstOrDefault(a => a.IsKind(_scopeKind)) != default;
        }
    }
}
