using Microsoft.CodeAnalysis;
using SyntaxSearch.Framework;
using System;

namespace SyntaxSearch.Matchers
{
    [Does("Match")]
    public partial class SyntaxNodeFuncMatcher : LogicalMatcher, ISyntaxNodeMatcher
    {
        public INodeMatcher Matcher { get; }

        [With]
        public Func<SyntaxNode, bool> Func { get; internal set; }

        [UseConstructor]
        public SyntaxNodeFuncMatcher(INodeMatcher matcher, Func<SyntaxNode, bool> func)
        {
            Matcher = matcher;
            Func = func;
        }

        [UseConstructor]
        public SyntaxNodeFuncMatcher(Func<SyntaxNode, bool> func)
        {
            Func = func;
        }

        public override bool IsMatch(SyntaxNode node, CaptureStore store)
        {
            if (Matcher?.IsMatch(node, store) == false)
            {
                return false;
            }
            return Func?.Invoke(node) == true;
        }
    }

    [Does("Match")]
    public partial class ExplicitNodeFuncMatcher<T> : LogicalMatcher, IExplicitNodeMatcher<T>
        where T : SyntaxNode
    {
        public INodeMatcher Matcher { get; }

        [With]
        public Func<T, bool> Func { get; internal set; }

        [UseConstructor]
        public ExplicitNodeFuncMatcher(INodeMatcher matcher, Func<T, bool> func)
        {
            Matcher = matcher;
            Func = func;
        }

        [UseConstructor]
        public ExplicitNodeFuncMatcher(Func<T, bool> func)
        {
            Func = func;
        }

        [UseConstructor]
        public ExplicitNodeFuncMatcher()
        {
            Func = static _ => true;
        }

        public ExplicitNodeFuncMatcher<T> Then(Func<T, bool> func)
        {
            return this.Match(func);
        }

        public override bool IsMatch(SyntaxNode node, CaptureStore store)
        {
            if (node is T cast)
            {
                if (Matcher?.IsMatch(cast, store) == false)
                {
                    return false;
                }
                return Func?.Invoke(cast) == true;
            }
            else
            {
                return false;
            }
        }
    }
}
