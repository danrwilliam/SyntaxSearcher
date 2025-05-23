﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        /// Checks if <paramref name="node"/> matches this class's criteria
        /// </summary>
        /// <param name="node">node to examine</param>
        /// <returns>node matches criteria</returns>
        bool IsMatch(SyntaxNode node, CaptureStore store);
    }

    public interface ITokenMatcher
    {
        /// <summary>
        /// Checks if <paramref name="token"/> matches this class's criteria
        /// </summary>
        /// <param name="token">token to examine</param>
        /// <returns>if token matches criteria</returns>
        bool IsMatch(SyntaxToken token, CaptureStore store);
    }

    public interface ISyntaxTokenListMatcher
    {
        bool IsMatch(SyntaxTokenList list, CaptureStore store);
    }

    public interface ISyntaxListMatcher
    {
        bool IsMatch<T>(IReadOnlyList<T> list, CaptureStore store) where T : SyntaxNode;
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
        public List<SyntaxNode> AdditionalCaptures { get; } = [];

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
        public abstract bool IsMatch(SyntaxNode node, CaptureStore store);

        protected BaseMatcher(BaseMatcher copy)
        {
        }

        protected BaseMatcher()
        {
        }
    }
}
