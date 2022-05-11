using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyntaxSearch.Matchers
{
    public interface INodeMatcher
    {
        /// <summary>
        /// List of matchers for child nodes
        /// </summary>
        List<INodeMatcher> Children { get; }

        /// <summary>
        /// Checks if <paramref name="node"/> matches this class's criteria
        /// </summary>
        /// <param name="node">node to examine</param>
        /// <returns>node matches criteria</returns>
        bool IsMatch(SyntaxNode node);

        CaptureStore Store { get; set; }

        /// <summary>
        /// Specifies what node should be passed
        /// to <see cref="IsMatch(SyntaxNode)"/>
        /// </summary>
        NodeAccept Accepts { get; set; }

        string ToTreeString();
    }

    public enum NodeAccept
    {
        Node,
        Child,
        PostNode
    }

    public class CaptureStore
    {
        public Dictionary<string, SyntaxNode> CapturedGroups { get; } = new Dictionary<string, SyntaxNode>();
        public List<SyntaxNode> AdditionalCaptures { get;  } = new List<SyntaxNode>();

        public SyntaxNode Override { get; set; } = default;

        public void Reset()
        {
            AdditionalCaptures.Clear();
            CapturedGroups.Clear();
            Override = default;
        }
    }

    public abstract class BaseMatcher : INodeMatcher
    {
        public List<INodeMatcher> Children { get; } = new List<INodeMatcher>();

        public CaptureStore Store { get; set; }

        public virtual NodeAccept Accepts { get; set; } = NodeAccept.Node;

        public abstract bool IsMatch(SyntaxNode node);

        public string ToTreeString()
        {
            StringBuilder builder = new StringBuilder();

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
    }

}
