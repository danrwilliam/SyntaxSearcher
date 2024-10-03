using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace SyntaxSearch.Matchers
{
    /// <summary>
    /// Represents a matcher that operates on its child matcher
    /// </summary>
    public abstract class LogicalMatcher : BaseMatcher
    {
    }

    public interface ILogicalMatcher : INodeMatcher { }

    public class ThisMatcher : BaseMatcher
    {
        public override NodeAccept Accepts { get => NodeAccept.Node; set { } }

        public override bool IsMatch(SyntaxNode node, CaptureStore store)
        {
            foreach (var f in Children)
            {
                if (!f.IsMatch(node, store))
                    return false;
            }

            return true;
        }
    }

    public partial class MatchCapture : BaseMatcher
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

    public partial class WithinMatcher : BaseMatcher
    {
        public override NodeAccept Accepts { get => NodeAccept.Node; set { } }
        private SyntaxKind _scopeKind;

        public override bool IsMatch(SyntaxNode node, CaptureStore store)
        {
            return node.Ancestors().FirstOrDefault(a => a.IsKind(_scopeKind)) != default;
        }
    }

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

    public class NotNullMatcher : LogicalMatcher
    {
        public override bool IsMatch(SyntaxNode node, CaptureStore store)
        {
            return node != null;
        }
    }

    /// <summary>
    /// Accepts anything as a match
    /// </summary>
    public partial class AnythingMatcher : LogicalMatcher
    {
        private readonly string _name;

        public override bool IsMatch(SyntaxNode node, CaptureStore store)
        {
            if (!string.IsNullOrWhiteSpace(_name))
            {
                store.CapturedGroups.Add(_name, node);
            }

            foreach (var child in Children)
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
    /// Node must match one of this matcher's child matcher
    /// </summary>
    public class IsOneOfMatcher : LogicalMatcher
    {
        public override bool IsMatch(SyntaxNode node, CaptureStore store)
        {
            foreach (var child in Children)
            {
                if (child.IsMatch(node, store))
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

        public override bool IsMatch(SyntaxNode node, CaptureStore store)
        {
            foreach (var child in Children)
            {
                if (child.IsMatch(node, store))
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

        public override bool IsMatch(SyntaxNode node, CaptureStore store)
        {
            foreach (var child in Children)
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
    public class NotMatcher : LogicalMatcher
    {
        public override bool IsMatch(SyntaxNode node, CaptureStore store)
        {
            return !Children[0].IsMatch(node, store);
        }
    }

    /// <summary>
    /// Matches when node contains something that matches this
    /// </summary>
    public partial class ContainsMatcher : LogicalMatcher
    {
        public override bool IsMatch(SyntaxNode node, CaptureStore store)
        {
            if (Children[0].IsMatch(node, store))
            {
                var other = Children.FirstOrDefault(f => f.Accepts == NodeAccept.PostNode);
                if (other is null || other.IsMatch(node, store))
                {
                    return true;
                }
            }

            foreach (var c in node.DescendantNodes(f => true))
            {
                if (Children[0].IsMatch(c, store))
                {
                    var other = Children.FirstOrDefault(f => f.Accepts == NodeAccept.PostNode);
                    if (other is null || other.IsMatch(c, store))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
