using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyntaxSearch.Matchers
{
    /// <summary>
    /// Represents a matcher that operates on its child matcher
    /// </summary>
    public abstract class LogicalMatcher : BaseMatcher
    {
    }

    public class ThisMatcher : BaseMatcher
    {
        public override NodeAccept Accepts { get => NodeAccept.Node; set { } }

        public override bool IsMatch(SyntaxNode node)
        {
            foreach (var f in Children)
            {
                if (!f.IsMatch(node))
                    return false;
            }

            return true;
        }
    }

    public partial class MatchCapture : BaseMatcher
    {
        private string _name;

        public override bool IsMatch(SyntaxNode node)
        {
            if (!string.IsNullOrWhiteSpace(_name) && Store.CapturedGroups.TryGetValue(_name, out var capturedNode))
            {
                return SyntaxFactory.AreEquivalent(node, capturedNode);
            }
            return false;
        }
    }

    public partial class WithinMatcher : BaseMatcher
    {
        public override NodeAccept Accepts { get => NodeAccept.Node; set { } }
        private SyntaxKind _scopeKind;

        public override bool IsMatch(SyntaxNode node)
        {
            return node.Ancestors().FirstOrDefault(a => a.IsKind(_scopeKind)) != default;
        }
    }

    public partial class ThenMatcher : BaseMatcher
    {
        public override NodeAccept Accepts { get => NodeAccept.PostNode; set { } }
        private SyntaxKind _scopeKind;

        public override bool IsMatch(SyntaxNode node)
        {
            SyntaxNode searchNode = node;

            if (_scopeKind != default)
            {
                searchNode = node.Ancestors().FirstOrDefault(a => a.IsKind(_scopeKind));
                if (searchNode is null)
                    return false;
            }
            else
            {
                node = node.SyntaxTree.GetRoot();
            }

            foreach (var extraChild in Children)
            {
                var nestedSearcher = new Searcher(extraChild, new CaptureStore());
                foreach (SyntaxNode extraMatch in nestedSearcher.Search(searchNode))
                {
                    Store.AdditionalCaptures.Add(extraMatch);
                }
            }

            return true;
        }

    }

    /// <summary>
    /// Requires node to have children
    /// </summary>
    public class HasChildrenMatcher : LogicalMatcher
    {
        public override NodeAccept Accepts { get => NodeAccept.Node; set { } }

        public override bool IsMatch(SyntaxNode node)
        {
            return node.ChildNodes().Any();
        }
    }

    /// <summary>
    /// Requires node to have no children
    /// </summary>
    public class NoChildrenMatcher : HasChildrenMatcher
    {
        public override NodeAccept Accepts { get => NodeAccept.Node; set { } }

        public override bool IsMatch(SyntaxNode node)
        {
            return !base.IsMatch(node);
        }
    }

    /// <summary>
    /// Accepts anything as a match
    /// </summary>
    public partial class AnythingMatcher : LogicalMatcher
    {
        private string _name;

        public override bool IsMatch(SyntaxNode node)
        {
            if (!string.IsNullOrWhiteSpace(_name))
            {
                Store.CapturedGroups.Add(_name, node);
            }
            return true;
        }
    }

    /// <summary>
    /// Node must match one of this matcher's child matcher
    /// </summary>
    public class IsOneOfMatcher : LogicalMatcher
    {
        public override bool IsMatch(SyntaxNode node)
        {
            foreach (var child in Children)
            {
                if (child.IsMatch(node))
                {
                    return true;
                }
            }
            return false;
        }
    }

    public class OrMatcher : LogicalMatcher
    {
        public override NodeAccept Accepts { get => NodeAccept.Node; set { } }

        public override bool IsMatch(SyntaxNode node)
        {
            foreach (var child in Children)
            {
                if (child.IsMatch(node))
                {
                    return true;
                }
            }
            return false;
        }
    }

    public class AndMatcher : LogicalMatcher
    {
        public override NodeAccept Accepts { get => NodeAccept.Node; set { } }

        public override bool IsMatch(SyntaxNode node)
        {
            foreach (var child in Children)
            {
                if (!child.IsMatch(node))
                {
                    return false;
                }
            }
            return true;
        }
    }

    /// <summary>
    /// Inverts match state of children
    /// </summary>
    public class NotMatcher : LogicalMatcher
    {
        public override bool IsMatch(SyntaxNode node)
        {
            return !Children[0].IsMatch(node);
        }
    }

    /// <summary>
    /// Matches when node contains something that matches this
    /// </summary>
    public class ContainsMatcher : LogicalMatcher
    {
        public override bool IsMatch(SyntaxNode node)
        {
            foreach (var c in node.DescendantNodes(f => true))
            {
                if (Children[0].IsMatch(c))
                    return true;
            }

            return false;
        }
    }
}
