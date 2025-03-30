using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace SyntaxSearch.Matchers
{
    public abstract class NodeMatcher : BaseMatcher, ISyntaxNodeMatcher
    {
        protected SyntaxKind _thisKind;

        protected NodeMatcher(SyntaxKind kind)
        {
            _thisKind = kind;
        }

        public sealed override bool IsMatch(SyntaxNode node, CaptureStore store)
        {
            return IsNodeMatch(node, store) && DoChildrenMatch(node, store);
        }

        protected abstract bool IsNodeMatch(SyntaxNode node, CaptureStore store);

        protected abstract bool DoChildrenMatch(SyntaxNode node, CaptureStore store);
    }
}
