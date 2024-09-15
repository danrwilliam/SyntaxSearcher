//using Microsoft.CodeAnalysis;
//using Microsoft.CodeAnalysis.CSharp;
//using SyntaxSearch.Matchers;
//using System.Collections.Generic;
//using System.Xml;

//namespace SyntaxSearch.Builder
//{
//    internal partial class MatchWalker(TreeBuilderOptions options) : CSharpSyntaxWalker
//    {
//        private readonly TreeBuilderOptions _options = options;

//        private readonly List<(SyntaxNode, string)> _captured = [];
//        private readonly List<(SyntaxNode, XmlElement)> _staged = [];

//        private INodeMatcher _current;

//        public override void VisitToken(SyntaxToken token)
//        {
//            var thisElement = Document.CreateElement(token.Kind().ToString());
//            _current.AppendChild(thisElement);
//            var thisParent = _current;

//            _current = thisElement;

//            base.VisitToken(token);

//            _current = thisParent;
//        }

//        public override void Visit(SyntaxNode node)
//        {
//            var thisElement = Document.CreateElement(node.Kind().ToString());

//            _current.AppendChild(thisElement);

//            var thisParent = _current;
//            _current = thisElement;

//            base.Visit(node);

//            _current = thisParent;
//        }

//        private bool TryGetCaptured(SyntaxNode node, out string captureKey)
//        {
//            // check anything that we know already appears more than once
//            foreach ((var capturedNode, var key) in _captured)
//            {
//                if (SyntaxFactory.AreEquivalent(capturedNode, node))
//                {
//                    captureKey = key;
//                    return true;
//                }
//            }

//            captureKey = $"capture_{_captured.Count}";

//            // look through the other nodes that are candidates for capture 
//            for (int i = 0; i < _staged.Count; i++)
//            {
//                (var capturedNode, var element) = _staged[i];
//                if (SyntaxFactory.AreEquivalent(capturedNode, node))
//                {
//                    var name = Document.CreateAttribute("Name");
//                    name.Value = captureKey;

//                    if (_options.UseAnythingForAutomaticCapture)
//                    {
//                        var replaced = Document.CreateElement("Anything");
//                        replaced.Attributes.SetNamedItem(name);

//                        var parent = element.ParentNode;

//                        List<XmlNode> nodes = [];

//                        foreach (XmlNode child in parent.ChildNodes)
//                        {
//                            if (child == element)
//                            {
//                                nodes.Add(replaced);
//                            }
//                            else
//                            {
//                                nodes.Add(child);
//                            }
//                        }

//                        parent.RemoveAll();

//                        foreach (var c in nodes)
//                        {
//                            parent.AppendChild(c);
//                        }

//                        if (_current == element)
//                        {
//                            _current = replaced;
//                        }
//                    }
//                    else
//                    {
//                        element.Attributes.SetNamedItem(name);
//                    }

//                    _captured.Add((capturedNode, captureKey));

//                    _staged.RemoveAt(i);

//                    return true;
//                }
//            }

//            return false;
//        }
//    }
//}
