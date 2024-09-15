using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SyntaxSearch;
using SyntaxSearch.Matchers;

namespace SyntaxSearchUnitTests.Matcher
{
    [TestFixture(@"
    public class Test
    {
        public void Method()
        {
            int a = 3;
            float f = 4f;
            a *= f;
        }
    }", 2, 1)]
    public class MatcherTests
    {
        private readonly CompilationUnitSyntax _root;
        private readonly int _numIdentifierName;
        private readonly int _numMethods;
        private SyntaxSearch.Parser.SearchFileParser _parser;

        public MatcherTests(string code, int numIdentifierName, int methods)
        {
            _root = SyntaxFactory.ParseCompilationUnit(code);
            _numIdentifierName = numIdentifierName;
            _numMethods = methods;
        }

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
            var searcher = new Searcher(new IdentifierNameMatcher(), new CaptureStore());
            var found = searcher.Search(_root);
            Assert.That(found, Has.Exactly(_numIdentifierName).Items);
        }

        [Test]
        public void TestMethod()
        {
            var searcher = new Searcher(new MethodDeclarationMatcher(), new CaptureStore());
            var found = searcher.Search(_root);
            Assert.That(found, Has.Exactly(_numMethods).Items);
        }

        [Test]
        public void TestExplicitIdentifier()
        {
            var mult = new SyntaxSearch.Matchers.Explicit.MultiplyAssignmentExpressionMatcher();
            mult = mult
                .WithLeft(new IdentifierNameMatcher(identifier: "a"))
                .WithRight(new IdentifierNameMatcher(identifier: "f"));

            var searcher = new Searcher(mult);

            Assert.That(searcher.Search(_root), Has.Exactly(1).Items);
        }

        [Test]
        public void TestExplicitIdentifier2()
        {
            var mult = new SyntaxSearch.Matchers.Explicit.MultiplyAssignmentExpressionMatcher();
            mult = mult
                .WithLeft(new IdentifierNameMatcher(identifier: "a", captureName: "left"))
                .WithRight(new NotMatcher
                {
                    new IdentifierNameMatcher(matchCapture: "left")
                });

            var searcher = new Searcher(mult);

            Assert.That(searcher.Search(_root), Has.Exactly(1).Items);
        }
    }
}