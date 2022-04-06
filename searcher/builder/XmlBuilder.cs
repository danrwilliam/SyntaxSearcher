using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace SyntaxSearch.Builder
{
    /// <summary>
    /// Creates an Xml representation of the syntax tree
    /// </summary>
    public class XmlTreeBuilder
    {
        private TreeWalker _walker;

        /// <summary>
        /// Constructed Xml document after calling <see cref="Build(SyntaxNode, TreeBuilderOptions)"/>
        /// </summary>
        public XmlDocument Document => _walker?.Document;

        public void Build(SyntaxNode node, TreeBuilderOptions options = null)
        {
            _walker = new TreeWalker(options ?? new TreeBuilderOptions());
            _walker.Visit(node);
        }
    }

    internal partial class TreeWalker : CSharpSyntaxWalker
    {
        public XmlDocument Document { get; private set; }
        private XmlElement _current;
        private readonly TreeBuilderOptions _options;

        internal TreeWalker(TreeBuilderOptions options)
            : base(options.Tokens ? SyntaxWalkerDepth.Token : SyntaxWalkerDepth.Node)
        {
            _options = options;

            Document = new XmlDocument();
            _current = Document.CreateElement("SyntaxSearchDefinition");

            Document.AppendChild(_current);
        }

        public override void VisitToken(SyntaxToken token)
        {
            var thisElement = Document.CreateElement(token.Kind().ToString());
            _current.AppendChild(thisElement);
            var thisParent = _current;

            _current = thisElement;

            base.VisitToken(token);

            _current = thisParent;
        }

        public override void Visit(SyntaxNode node)
        {
            var thisElement = Document.CreateElement(node.Kind().ToString());

            _current.AppendChild(thisElement);

            var thisParent = _current;
            _current = thisElement;

            base.Visit(node);

            _current = thisParent;
        }
    }
}
