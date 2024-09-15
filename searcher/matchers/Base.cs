using Microsoft.CodeAnalysis;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace SyntaxSearch.Matchers
{
    public interface INodeMatcher
    {
        /// <summary>
        /// List of matchers for child nodes
        /// </summary>
        IReadOnlyList<INodeMatcher> Children { get; }

        /// <summary>
        /// Checks if <paramref name="node"/> matches this class's criteria
        /// </summary>
        /// <param name="node">node to examine</param>
        /// <returns>node matches criteria</returns>
        bool IsMatch(SyntaxNode node);

        /// <summary>
        /// Stores additional captured nodes found during search
        /// </summary>
        CaptureStore Store { get; set; }

        /// <summary>
        /// Specifies what node should be passed
        /// to <see cref="IsMatch(SyntaxNode)"/>
        /// </summary>
        NodeAccept Accepts { get; set; }

        string ToTreeString();

        internal void AddChild(INodeMatcher matcher);
    }

    public interface IUnaryNodeMatcher { }

    public interface ITreeWalkNodeMatcher : INodeMatcher
    {
    }

    public enum NodeAccept
    {
        Node,
        Child,
        PostNode
    }

    public class CaptureStore
    {
        /// <summary>
        /// Collection of named captures 
        /// </summary>
        public Dictionary<string, SyntaxNode> CapturedGroups { get; } = [];

        /// <summary>
        /// Additional non-named captures
        /// </summary>
        public List<SyntaxNode> AdditionalCaptures { get;  } = [];

        public SyntaxNode Override { get; set; } = default;

        public void Reset()
        {
            AdditionalCaptures.Clear();
            CapturedGroups.Clear();
            Override = default;
        }
    }

    public abstract class BaseMatcher : ITreeWalkNodeMatcher, IEnumerable<INodeMatcher>
    {
        private readonly List<INodeMatcher> _children = [];
        public IReadOnlyList<INodeMatcher> Children => _children;

        private CaptureStore _store;
        public CaptureStore Store
        {
            get => _store;
            set
            {
                _store = value;
                SetStore(value);
            }
        }

        protected virtual void SetStore(CaptureStore store)
        {
            foreach (var c in Children)
            {
                c.Store = _store;
            }
        }

        public virtual NodeAccept Accepts { get; set; } = NodeAccept.Node;

        public abstract bool IsMatch(SyntaxNode node);

        public string ToTreeString()
        {
            StringBuilder builder = new();

            ToTreeString(builder, 0);

            return builder.ToString();
        }

        internal void ToTreeString(StringBuilder builder, int level)
        {
            builder.AppendLine($"{"".PadLeft(level, ' ')}{this.GetType().Name}");
            foreach (var c in Children.Cast<BaseMatcher>())
            {
                c.ToTreeString(builder, level + 1);
            }
        }

        public void Add(INodeMatcher matcher)
        {
            _children.Add(matcher);
        }

        public void AddChild(INodeMatcher matcher)
        {
            _children.Add(matcher);
        }

        public IEnumerator<INodeMatcher> GetEnumerator()
        {
            return Children.AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Children.AsEnumerable().GetEnumerator();
        }
    }
}
