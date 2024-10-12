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
            var searcher = new Searcher(new MethodDeclarationMatcher());
            var found = searcher.Search(_root);
            NUnit::Assert.That(found, NUnit::Has.Exactly(_numMethods).Items);
        }

        [NUnit::Test]
        public void TestExplicitIdentifier()
        {
            var mult = new SyntaxSearch.Matchers.Explicit.MultiplyAssignmentExpressionMatcher();
            mult = mult
                .WithLeft(new IdentifierNameMatcher(identifier: "a"))
                .WithRight(new IdentifierNameMatcher(identifier: "f"));

            var searcher = new Searcher(mult);

            NUnit::Assert.That(searcher.Search(_root), NUnit::Has.Exactly(1).Items);
        }

        [NUnit::Test]
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

            NUnit::Assert.That(searcher.Search(_root), NUnit::Has.Exactly(1).Items);
        }

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

    public class SyntaxTests
    {
        [NUnit::TestCase("base.Value", true)]
        [NUnit::TestCase("base.Value()", false)]
        [NUnit::TestCase("this.Add", false)]
        public void BaseAccessIsMatch(string text, bool isMatch)
        {
            var expr = SyntaxFactory.ParseExpression(text);
            NUnit::Assert.That(Is.BaseAccessExpression.IsMatch(expr), NUnit::Is.EqualTo(isMatch));
        }

        [NUnit::TestCase("base.Value", false)]
        [NUnit::TestCase("this.Value()", false)]
        [NUnit::TestCase("this.Add", true)]
        public void ThisAccessIsMatch(string text, bool isMatch)
        {
            var expr = SyntaxFactory.ParseExpression(text);
            NUnit::Assert.That(Is.ThisAccessExpression.IsMatch(expr), NUnit::Is.EqualTo(isMatch));
        }

        [NUnit::TestCase("base.Value", 1)]
        [NUnit::TestCase("base.Value + base.Value", 2)]
        [NUnit::TestCase("base.Value(base.A, base.B, base.C)", 4)]
        public void BaseAccessMatcher(string text, int count)
        {
            var expr = SyntaxFactory.ParseExpression(text);
            var results = Is.BaseAccessExpression.Search(expr);
            NUnit::Assert.That(results, NUnit::Has.Exactly(count).Items);
        }

        [NUnit::TestCase("base.Value(1)", 1)]
        [NUnit::TestCase("base.MyMethod(base.Value, 1, base.Calculate(), 3)", 2)]
        public void BaseAccessInvocation(string expression, int count)
        {
            var node = SyntaxFactory.ParseExpression(expression);
            var results = Is.InvocationExpression
                .WithExpression(Is.BaseAccessExpression)
                .Search(node);
            NUnit::Assert.That(results, NUnit::Has.Exactly(count).Items);
        }

        [NUnit::TestCase("this.Value", 1)]
        [NUnit::TestCase("this.Value + base.Value", 1)]
        [NUnit::TestCase("this.Value(this.A, base.B, this.C)", 3)]
        public void ThisAccessMatcher(string text, int count)
        {
            var expr = SyntaxFactory.ParseExpression(text);
            var results = Is.ThisAccessExpression.Search(expr);
            NUnit::Assert.That(results, NUnit::Has.Exactly(count).Items);
        }

        [NUnit::TestCase("this.Value(1)", 1)]
        [NUnit::TestCase("this.MyMethod(base.Value, 1, this.Test(1), 3)", 2)]
        public void ThisAccessInvocation(string expression, int count)
        {
            var node = SyntaxFactory.ParseExpression(expression);
            var results = Is.InvocationExpression
                .WithExpression(Is.ThisAccessExpression)
                .Search(node);
            NUnit::Assert.That(results, NUnit::Has.Exactly(count).Items);
        }
    }
}