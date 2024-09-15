using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;

namespace SyntaxSearcher.Generators
{
    internal class SyntaxClassInfo
    {
        public string KindName { get; set; }
        public string ClassName { get; set; }

        public SyntaxKind Kind { get; set;  }

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