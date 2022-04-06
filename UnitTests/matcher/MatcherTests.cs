using System;
using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace SyntaxSearchUnitTests.Matcher
{
    [TestFixture]
    public class MatcherTests
    {
        [Test]
        public void TestEmpty()
        {
            Assert.Throws<ArgumentException>(() => SyntaxSearch.Parser.SearchFileParser.ParseFromString("<SyntaxSearchDefinition></SyntaxSearchDefinition>"));
        }

        [Test]
        public void TestIdentifier()
        {
            var searcher = SyntaxSearch.Parser.SearchFileParser.ParseFromString("<SyntaxSearchDefinition><IdentifierName /></SyntaxSearchDefinition>");
            var expr = SyntaxFactory.ParseExpression(@"
public void Method()
{
    int a = 3;
    float f = 4f;
}");

            var found = searcher.Search(expr);

            Assert.That(found.Count(), Is.EqualTo(3));

        }
    }
}