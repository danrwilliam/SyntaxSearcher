using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SyntaxSearch.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace SyntaxSearch.Matchers
{
    /// <summary>
    /// Generate a With method for marked field
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class WithAttribute : Attribute;

    public abstract class LogicalMatcher : BaseMatcher, ILogicalMatcher
    {
        public ImmutableArray<INodeMatcher> Matchers { get; private set;  } = [];

        protected LogicalMatcher(params INodeMatcher[] matchers)
        {
            Matchers = Matchers.AddRange(matchers);
        }

        internal void AddChild(INodeMatcher matcher)
        {
            Matchers = Matchers.Add(matcher);
        }
    }

    public interface ILogicalMatcher : INodeMatcher { }

    //public class ThisMatcher : BaseMatcher
    //{
    //    public override NodeAccept Accepts { get => NodeAccept.Node; set { } }

    //    public override bool IsMatch(SyntaxNode node, CaptureStore store)
    //    {
    //        foreach (var f in Children)
    //        {
    //            if (!f.IsMatch(node, store))
    //                return false;
    //        }

    //        return true;
    //    }
    //}

    //[Does("Match")]
    public partial class MatchCapture : INodeMatcher
    {
        private string _name;

        public NodeAccept Accepts { get; set; }

        public MatchCapture Named(string name)
        {
            var dup = new MatchCapture(name);
            dup._name = name;
            return dup;
        }

        public bool IsMatch(SyntaxNode node, CaptureStore store)
        {
            if (!string.IsNullOrWhiteSpace(_name) 
                && store.CapturedGroups.TryGetValue(_name, out var capturedNode))
            {
                if (capturedNode is VariableDeclaratorSyntax variableDeclSyntax
                    && node is IdentifierNameSyntax identifier)
                {
                    return SyntaxFactory.AreEquivalent(variableDeclSyntax.Identifier, identifier.Identifier);
                }
                else
                {
                    return SyntaxFactory.AreEquivalent(node, capturedNode);
                }
            }
            return false;
        }
    }

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

    /*
    public abstract class GetExtraNodesMatcher : BaseMatcher
    {
        public override bool IsMatch(SyntaxNode node, CaptureStore store)
        {
            SyntaxNode searchNode = SearchFrom(node);
            if (searchNode is null)
                return false;


            foreach (var extraChild in Children)
            {
                var nestedSearcher = new Searcher(extraChild);
                foreach (var result in nestedSearcher.Search(searchNode))
                {
                    store.AdditionalCaptures.Add(result.Node);
                }
            }

            return GetReturnValue(store);
        }

        protected abstract bool GetReturnValue(CaptureStore store);

        protected abstract SyntaxNode SearchFrom(SyntaxNode node);
    }

    /// <summary>
    /// Allows for finding and returning additional syntax nodes
    /// </summary>
    public partial class ThenMatcher : GetExtraNodesMatcher
    {
        public override NodeAccept Accepts { get => NodeAccept.PostNode; set { } }

        /// <summary>
        /// Specifies what ancestor to go search within
        /// </summary>
        protected SyntaxKind _scopeKind;

        /// <summary>
        /// If true, extra nodes must be found for this node to be returned
        /// as a match.
        /// </summary>
        protected bool _require = false;

        protected override bool GetReturnValue(CaptureStore store)
        {
            if (_require)
            {
                return store.AdditionalCaptures.Any();
            }
            else
            {
                return true;
            }
        }

        protected override SyntaxNode SearchFrom(SyntaxNode node)
        {
            SyntaxNode searchNode = node;

            if (_scopeKind != default)
            {
                searchNode = node.Ancestors().FirstOrDefault(a => a.IsKind(_scopeKind));
            }
            else
            {
                searchNode = node.SyntaxTree.GetRoot();
            }

            return searchNode;
        }
    }

    public partial class ReturnMatcher : BaseMatcher
    {
        public override NodeAccept Accepts { get => NodeAccept.PostNode; set { } }

        protected SyntaxKind _ancestorKind;
        public override bool IsMatch(SyntaxNode node, CaptureStore store)
        {
            SyntaxNode searchNode = SearchFrom(node);
            if (searchNode is null)
                return false;

            if (Children.Any())
            {
                foreach (var extraChild in Children)
                {
                    var nestedSearcher = new Searcher(extraChild);
                    foreach (var result in nestedSearcher.Search(searchNode))
                    {
                        store.AdditionalCaptures.Add(result.Node);
                    }
                }

                if (store.AdditionalCaptures.Any())
                {
                    store.Override = store.AdditionalCaptures.First();
                    store.AdditionalCaptures.Clear();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                store.Override = searchNode;
                return true;
            }
        }
        protected SyntaxNode SearchFrom(SyntaxNode node)
        {
            SyntaxNode searchNode = node;

            if (_ancestorKind != default)
            {
                searchNode = node.Ancestors().FirstOrDefault(a => a.IsKind(_ancestorKind));
            }
            else
            {
                searchNode = node.SyntaxTree.GetRoot();
            }

            return searchNode;
        }
    }
    */

    /// <summary>
    /// Requires node to have children
    /// </summary>
    public class HasChildrenMatcher : LogicalMatcher
    {
        public override NodeAccept Accepts { get => NodeAccept.Node; set { } }

        public override bool IsMatch(SyntaxNode node, CaptureStore store)
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

        public override bool IsMatch(SyntaxNode node, CaptureStore store)
        {
            return !base.IsMatch(node, store);
        }
    }

    public class NotNullMatcher : INodeMatcher
    {
        public NodeAccept Accepts { get; set; }

        public bool IsMatch(SyntaxNode node, CaptureStore store)
        {
            return node is not null;
        }
    }

    [Is]
    public class NullMatcher : INodeMatcher
    {
        public NodeAccept Accepts { get; set; }

        public bool IsMatch(SyntaxNode node, CaptureStore store)
        {
            return node is null;
        }
    }

    /// <summary>
    /// Accepts anything as a match
    /// </summary>
    [Is]
    public partial class AnythingMatcher : INodeMatcher
    {
        [With]
        private readonly string _name;

        public static AnythingMatcher Default { get; } = new AnythingMatcher();

        public AnythingMatcher()
        {
        }

        public AnythingMatcher Capture(string name)
        {
            return new AnythingMatcher(name);
        }

        public NodeAccept Accepts { get; set; }

        public bool IsMatch(SyntaxNode node, CaptureStore store)
        {
            if (_name is not null)
            {
                store.CapturedGroups.Add(_name, node);
            }
            return true;
        }
    }

    /// <summary>
    /// Node must match one of this matcher's child matcher
    /// </summary>
    [Is("OneOf")]
    public class IsOneOfMatcher(params INodeMatcher[] matchers) : LogicalMatcher(matchers)
    {
        public override bool IsMatch(SyntaxNode node, CaptureStore store)
        {
            foreach (var child in Matchers)
            {
                if (child.IsMatch(node, store))
                {
                    return true;
                }
            }
            return false;
        }
    }

    [Is]
    public class AndMatcher(params INodeMatcher[] matchers) : LogicalMatcher(matchers)
    {
        public override NodeAccept Accepts { get => NodeAccept.Node; set { } }

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
    /// Inverts match state of children
    /// </summary>
    public sealed class NotMatcher : INodeMatcher
    {
        public NodeAccept Accepts { get; set; }

        private readonly INodeMatcher _matcher;

        public NotMatcher(INodeMatcher matcher)
        {
            _matcher = matcher;
        }

        public NotMatcher() { }

        public bool IsMatch(SyntaxNode node, CaptureStore store)
        {
            return !_matcher.IsMatch(node);
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
