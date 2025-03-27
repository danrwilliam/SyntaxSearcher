using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SyntaxSearch.Framework;
using System;
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
        protected LogicalMatcher(BaseMatcher copy) : base(copy)
        {
        }

        protected LogicalMatcher()
        {
        }
    }

    public abstract class MultipleOperandLogicalMatcher : LogicalMatcher, ICompoundLogicalMatcher
    {
        public ImmutableArray<INodeMatcher> Matchers { get; private set; } = [];

        protected MultipleOperandLogicalMatcher(MultipleOperandLogicalMatcher copy) : base(copy)
        {
            Matchers = copy.Matchers;
        }

        protected MultipleOperandLogicalMatcher(params INodeMatcher[] matchers)
        {
            Matchers = Matchers.AddRange(matchers);
        }

        internal void AddChild(INodeMatcher matcher)
        {
            Matchers = Matchers.Add(matcher);
        }
    }

    /// <summary>
    /// A matcher that doesn't operate directly on the syntax node
    /// </summary>
    public interface ILogicalMatcher : INodeMatcher { }

    /// <summary>
    /// A matcher that contains a collection of matchers to attempt
    /// to match on the given syntax node
    /// </summary>
    public interface ICompoundLogicalMatcher : ILogicalMatcher
    {
        public ImmutableArray<INodeMatcher> Matchers { get; }
    }

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
    public partial class MatchCapture : LogicalMatcher
    {
        private string _name;

        public MatchCapture Named(string name)
        {
            var dup = new MatchCapture(name);
            dup._name = name;
            return dup;
        }

        public override bool IsMatch(SyntaxNode node, CaptureStore store)
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

    [Is]
    public sealed class NotNullMatcher : LogicalMatcher
    {
        public override bool IsMatch(SyntaxNode node, CaptureStore store)
        {
            return node is not null;
        }
    }

    [Is]
    public sealed class NullMatcher : LogicalMatcher
    {
        public override bool IsMatch(SyntaxNode node, CaptureStore store)
        {
            return node is null;
        }
    }

    public sealed partial class CaptureMatcher : BaseMatcher, ILogicalMatcher
    {
        public static CaptureMatcher Default { get; } = new CaptureMatcher();

        [With]
        private INodeMatcher _matcher;

        [With]
        private string _name;

        public override bool IsMatch(SyntaxNode node, CaptureStore store)
        {
            if (_matcher?.IsMatch(node, store) is true
                && _name is not null)
            {
                store.CapturedGroups.Add(_name, node);
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Accepts anything as a match
    /// </summary>
    [Is]
    public partial class AnythingMatcher : LogicalMatcher
    {
        [With]
        private readonly string _name;

        public static AnythingMatcher Default { get; } = new AnythingMatcher();

        public AnythingMatcher Capture(string name)
        {
            return new AnythingMatcher(name);
        }

        public override bool IsMatch(SyntaxNode node, CaptureStore store)
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
    public class IsOneOfMatcher(params INodeMatcher[] matchers) : MultipleOperandLogicalMatcher(matchers)
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
    public class AndMatcher(params INodeMatcher[] matchers) : MultipleOperandLogicalMatcher(matchers)
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
    public sealed class NotMatcher : LogicalMatcher
    {
        private readonly INodeMatcher _matcher;

        public NotMatcher(INodeMatcher matcher)
        {
            _matcher = matcher;
        }

        public NotMatcher() { }

        public override bool IsMatch(SyntaxNode node, CaptureStore store)
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
