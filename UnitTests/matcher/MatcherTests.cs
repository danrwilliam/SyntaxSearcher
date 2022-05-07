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
        private SyntaxSearch.Parser.SearchFileParser _parser;

        [SetUp]
        public void Setup()
        {
            _parser = new SyntaxSearch.Parser.SearchFileParser();
        }


        [Test]
        public void TestEmpty()
        {
            Assert.Throws<ArgumentException>(() => _parser.ParseFromString("<SyntaxSearchDefinition></SyntaxSearchDefinition>"));
        }

        [Test]
        public void TestIdentifier()
        {
            var searcher = _parser.ParseFromString("<SyntaxSearchDefinition><IdentifierName /></SyntaxSearchDefinition>");
            var expr = SyntaxFactory.ParseCompilationUnit(@"
public void Method()
{
    int a = 3;
    float f = 4f;

    a *= f;
}");

            var found = searcher.Search(expr);

            Assert.That(found.Count(), Is.EqualTo(2));
        }
    }
}