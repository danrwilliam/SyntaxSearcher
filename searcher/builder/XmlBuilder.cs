using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
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

        private readonly List<(SyntaxNode, string)> _captured = [];
        private readonly List<(SyntaxNode, XmlElement)> _staged = [];

        internal TreeWalker(TreeBuilderOptions options)
            : base(options.Tokens ? SyntaxWalkerDepth.Token : SyntaxWalkerDepth.Node)
        {
            _options = options;

            Document = new XmlDocument();
            _current = Document.CreateElement("SyntaxSearchDefinition");

            if (_options.NamedChildren)
            {
                var attr = Document.CreateAttribute("Format");
                attr.Value = "Explicit";
                _current.Attributes.SetNamedItem(attr);
            }

            Document.AppendChild(_current);
        }


        public override void VisitToken(SyntaxToken token)
        {
            (_current, var thisParent) = (Document.CreateElement(token.Kind().ToString()), _current);
            thisParent.AppendChild(_current);

            base.VisitToken(token);

            _current = thisParent;
        }

        public override void Visit(SyntaxNode node)
        {
            (_current, var thisParent) = (Document.CreateElement(node.Kind().ToString()), _current);

            thisParent.AppendChild(_current);

            base.Visit(node);

            _current = thisParent;
        }

        private bool TryGetCaptured(SyntaxNode node, out string captureKey)
        {
            // check anything that we know already appears more than once
            foreach ((var capturedNode, var key) in _captured)
            {
                if (SyntaxFactory.AreEquivalent(capturedNode, node))
                {
                    captureKey = key;
                    return true;
                }
            }

            captureKey = $"capture_{_captured.Count}";

            // look through the other nodes that are candidates for capture 
            for (int i = 0; i < _staged.Count; i++)
            {
                (var capturedNode, var element) = _staged[i];
                if (SyntaxFactory.AreEquivalent(capturedNode, node))
                {
                    var name = Document.CreateAttribute("Name");
                    name.Value = captureKey;

                    if (_options.UseAnythingForAutomaticCapture)
                    {
                        var replaced = Document.CreateElement("Anything");
                        replaced.Attributes.SetNamedItem(name);

                        var parent = element.ParentNode;

                        List<XmlNode> nodes = [];

                        foreach (XmlNode child in parent.ChildNodes)
                        {
                            if (child == element)
                            {
                                nodes.Add(replaced);
                            }
                            else
                            {
                                nodes.Add(child);
                            }
                        }

                        parent.RemoveAll();

                        foreach (var c in nodes)
                        {
                            parent.AppendChild(c);
                        }

                        if (_current == element)
                        {
                            _current = replaced;
                        }
                    }
                    else
                    {
                        element.Attributes.SetNamedItem(name);
                    }

                    _captured.Add((capturedNode, captureKey));

                    _staged.RemoveAt(i);

                    return true;
                }
            }

            return false;
        }
    }
}
