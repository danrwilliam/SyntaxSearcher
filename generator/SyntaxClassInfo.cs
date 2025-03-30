using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;

namespace SyntaxSearcher.Generators
{
    public enum PropertyKind
    {
        Normal = 0,
        GenericTokenList = 1,
        TokenList = 2,
        SyntaxKind = 3
    }

    internal class SyntaxClassInfo
    {
        public string KindName { get; set; }
        public string ClassName { get; set; }

        public SyntaxKind Kind { get; set; }

        public IReadOnlyList<MatchProperty> Properties { get; internal set; } = [];

        public IReadOnlyList<MatchField> Fields { get; internal set; } = [];

        public SyntaxClassInfo(string kind, string className)
        {
            KindName = kind;
            ClassName = className;
        }

        public SyntaxClassInfo()
        {
        }
    }
}