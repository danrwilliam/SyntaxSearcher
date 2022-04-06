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
    public class TreeBuilderOptions
    {
        /// <summary>
        /// Include modifier information for matcher
        /// </summary>
        public bool Modifiers { get; set; } = false;

        /// <summary>
        /// Include keyword text for matcher
        /// </summary>
        public bool Keywords { get; set; } = true;

        /// <summary>
        /// Include identifier text for matcher
        /// </summary>
        public bool Identifiers { get; set; } = true;

        /// <summary>
        /// Visit syntax tokens in addition to nodes
        /// </summary>
        public bool Tokens { get; set; } = false;

        /// <summary>
        /// Automatically introduce MatchCapture for identical nodes
        /// </summary>
        public bool AutomaticCapture { get; set; } = false;


        /// <summary>
        /// When used with <see cref="AutomaticCapture"/>, then use Anything
        /// matcher for captured items.
        /// </summary>
        public bool UseAnythingForAutomaticCapture { get; set; } = false;
    }
}
