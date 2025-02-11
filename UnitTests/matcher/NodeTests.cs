using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit = NUnit.Framework;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SyntaxSearch;
using SyntaxSearch.Matchers;
using SyntaxSearch.Framework;
using Microsoft.CodeAnalysis;

namespace SyntaxSearchUnitTests.Matcher
{
    public class NodeTests
    {
        [NUnit::TestCase]
        public void LogicalNotExpression() => NUnit::Assert.That(Is.LogicalNotExpression.IsMatch(ParseExpression("!obj")), NUnit::Is.True);

        [NUnit::TestCase]
        public void PostIncrementExpression() => NUnit::Assert.That(Is.PostIncrementExpression.IsMatch(ParseExpression("a++")), NUnit::Is.True);

        [NUnit::TestCase]
        public void PreIncrementExpression() => NUnit::Assert.That(Is.PreIncrementExpression.IsMatch(ParseExpression("++a")), NUnit::Is.True);

        [NUnit::TestCase]
        public void PostDecrementExpression() => NUnit::Assert.That(Is.PostDecrementExpression.IsMatch(ParseExpression("a--")), NUnit::Is.True);

        [NUnit::TestCase]
        public void PreDecrementExpression() => NUnit::Assert.That(Is.PreDecrementExpression.IsMatch(ParseExpression("--aa")), NUnit::Is.True);

        [NUnit::TestCase]
        public void LogicalAndExpression() => NUnit::Assert.That(Is.LogicalAndExpression.IsMatch(ParseExpression("a && b")), NUnit::Is.True);
    }
}