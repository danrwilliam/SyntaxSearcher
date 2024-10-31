using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit = NUnit.Framework;
using SyntaxSearch;
using SyntaxSearch.Matchers;
using SyntaxSearch.Framework;
using Microsoft.CodeAnalysis;

namespace SyntaxSearchUnitTests.Matcher
{
    [NUnit::TestFixture(@"
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

        [NUnit::SetUp]
        public void Setup()
        {
            _parser = new SyntaxSearch.Parser.SearchFileParser();
        }


        [NUnit::Test]
        public void TestEmpty()
        {
            NUnit::Assert.Throws<ArgumentException>(() => _parser.ParseFromString("<SyntaxSearchDefinition></SyntaxSearchDefinition>"));
        }

        [NUnit::Test]
        public void TestIdentifier()
        {
            var searcher = new Searcher(new IdentifierNameMatcher());
            var found = searcher.Search(_root);
            NUnit::Assert.That(found, NUnit::Has.Exactly(_numIdentifierName).Items);
        }

        [NUnit::Test]
        public void TestMethod()
        {
            NUnit::Assert.That(Is.MethodDeclaration.Search(_root), NUnit::Has.Exactly(_numMethods).Items);
        }

        [NUnit::Test]
        public void TestExplicitIdentifier()
        {
            var mult = Is.MultiplyAssignmentExpression
                .WithLeft(new IdentifierNameMatcher(identifier: "a"))
                .WithRight(new IdentifierNameMatcher(identifier: "f"));

            var searcher = new Searcher(mult);

            NUnit::Assert.That(searcher.Search(_root), NUnit::Has.Exactly(1).Items);
        }

        //[NUnit::Test]
        //public void TestExplicitIdentifier2()
        //{
        //    var mult = new SyntaxSearch.Matchers.Explicit.MultiplyAssignmentExpressionMatcher();
        //    mult = mult
        //        .WithLeft(new IdentifierNameMatcher(identifier: "a", captureName: "left"))
        //        .WithRight(new NotMatcher
        //        {
        //            new IdentifierNameMatcher(matchCapture: "left")
        //        });

        //    var searcher = new Searcher(mult);

        //    NUnit::Assert.That(searcher.Search(_root), NUnit::Has.Exactly(1).Items);
        //}

        [NUnit::Test]
        public void CaptureMultiple()
        {
            var matcher = new SyntaxSearch.Matchers.Explicit.LocalDeclarationStatementMatcher();
            var searcher = new Searcher(matcher);
            var results = searcher.Search(_root).ToArray();

            NUnit::Assert.Multiple(() =>
            {
                var first = results[0];
                NUnit::Assert.That(first.Node, NUnit::Is.InstanceOf<LocalDeclarationStatementSyntax>());
                var local = (LocalDeclarationStatementSyntax)first.Node;
                NUnit::Assert.That(local.Declaration.Variables[0].Identifier.Text, NUnit::Is.EqualTo("a"));
            });

            NUnit::Assert.Multiple(() =>
            {
                var second = results[1];
                NUnit::Assert.That(second.Node, NUnit::Is.InstanceOf<LocalDeclarationStatementSyntax>());
                var local = (LocalDeclarationStatementSyntax)second.Node;
                NUnit::Assert.That(local.Declaration.Variables[0].Identifier.Text, NUnit::Is.EqualTo("f"));
            });
        }

        [NUnit::Test]
        public void Constraint()
        {
            var matcher = Is.LocalDeclarationStatement
                .WithDeclaration(Is.VariableDeclaration
                    .WithType(Is.PredefinedType));
            var searcher = new Searcher(matcher);

            NUnit::Assert.That(searcher.Search(_root), NUnit::Has.Exactly(2).Items);
        }
    }
}