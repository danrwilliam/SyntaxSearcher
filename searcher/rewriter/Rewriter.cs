using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SyntaxSearch.Matchers;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SyntaxSearch.Rewriter
{
    public class RewriterTemplate
    {
        private readonly string _template;
        private readonly HashSet<(string, string)> _namedGroups;

        private readonly NodeWrapperDelegate _wrap;

        public delegate SyntaxNode NodeWrapperDelegate(ExpressionSyntax node);

        public RewriterTemplate(string templateString, NodeWrapperDelegate wrapperDelegate = null)
        {
            _template = templateString;
            _wrap = wrapperDelegate;

            _namedGroups = [];

            MatchCollection captures = Regex.Matches(_template, @"(\$([A-Za-z]+))");

            foreach (Match variable in captures)
            {
                _namedGroups.Add((variable.Groups[1].Value, variable.Groups[2].Value));
            }
        }

        /// <summary>
        /// Create a new SyntaxNode from the template using the captured groups
        /// </summary>
        /// <param name="originalNode"></param>
        /// <returns></returns>
        /// <param name="store">has captured groups</param>
        public SyntaxNode Rewrite(SyntaxNode originalNode, CaptureStore store)
        {
            string output = _template;

            foreach ((var templateVariable, var groupName) in _namedGroups)
            {
                if (store.CapturedGroups.TryGetValue(groupName, out var capture))
                {
                    string repr = capture.ToFullString();

                    output = output.Replace(templateVariable, repr);
                }
                else
                {
                    throw new InvalidOperationException($"capture group \"{groupName}\" expected but not found");
                }
            }

            var newNode = SyntaxFactory.ParseExpression(output).NormalizeWhitespace().WithTriviaFrom(originalNode);

            if (_wrap != null)
                return _wrap(newNode).NormalizeWhitespace();
            else
                return newNode;
        }
    }
}
