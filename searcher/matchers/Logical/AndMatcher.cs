﻿using Microsoft.CodeAnalysis;
using SyntaxSearch.Framework;

namespace SyntaxSearch.Matchers
{
    [Is]
    public class AndMatcher(params INodeMatcher[] matchers) : MultipleOperandLogicalMatcher(matchers)
    {
        public override bool IsMatch(SyntaxNode node, CaptureStore store)
        {
            foreach (var child in Matchers)
            {
                if (!child.IsMatch(node, store))
                {
                    return false;
                }
            }
            return true;
        }
    }

    /// <summary>
    /// Matches when node contains something that matches this
    /// </summary>
    //[Does("Contain")]
    //public partial class ContainsMatcher : LogicalMatcher
    //{
    //    internal ContainsMatcher()
    //    {
    //    }

    //    public ContainsMatcher(INodeMatcher matcher)
    //    {
    //        AddChild(matcher);
    //    }

    //    public override bool IsMatch(SyntaxNode node, CaptureStore store)
    //    {
    //        if (Children[0].IsMatch(node, store))
    //        {
    //            var other = Children.FirstOrDefault(f => f.Accepts == NodeAccept.PostNode);
    //            if (other is null || other.IsMatch(node, store))
    //            {
    //                return true;
    //            }
    //        }

    //        foreach (var c in node.DescendantNodes(f => true))
    //        {
    //            if (Children[0].IsMatch(c, store))
    //            {
    //                var other = Children.FirstOrDefault(f => f.Accepts == NodeAccept.PostNode);
    //                if (other is null || other.IsMatch(c, store))
    //                {
    //                    return true;
    //                }
    //            }
    //        }

    //        return false;
    //    }
    //}
}
