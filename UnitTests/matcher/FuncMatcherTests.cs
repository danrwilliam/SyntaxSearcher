using Microsoft.CodeAnalysis.CSharp.Syntax;
using SyntaxSearch.Framework;
using SyntaxSearch;
using NUnit = NUnit.Framework;
using Microsoft.CodeAnalysis.CSharp;

namespace SyntaxSearchUnitTests.Matcher
{
    [NUnit::TestFixture]
    public class FuncMatcherTests
    {
        [NUnit::TestCase("a + a - b / c * 3 + d", 3)]
        [NUnit::TestCase("b + b / d", 0)]
        public void TestNodeExtension(string code, int result)
        {
            var node = SyntaxFactory.ParseExpression(code);
            var matcher = Is.Anything.Match(f => f is IdentifierNameSyntax { Identifier.Text: "a" or "c"});

            var results = matcher.Search(node);

            NUnit::Assert.That(results, NUnit::Has.Exactly(result).Items);
        }

        [NUnit::TestCase("a + a - b / c * 3 + d", 3)]
        [NUnit::TestCase("b + b / d", 0)]
        public void TestGenericNodeExtension(string code, int result)
        {
            var node = SyntaxFactory.ParseExpression(code);
            var matcher = Is.Anything.Match<IdentifierNameSyntax>(f => f is { Identifier.Text: "a" or "c" });

            var results = matcher.Search(node);

            NUnit::Assert.That(results, NUnit::Has.Exactly(result).Items);
        }

        [NUnit::TestCase("a + a - b / c * 3 + d", 3)]
        [NUnit::TestCase("b + b / d", 0)]
        public void TestNode(string code, int result)
        {
            var node = SyntaxFactory.ParseExpression(code);
            var matcher = Is.Match(f => f is IdentifierNameSyntax { Identifier.Text: "a" or "c" });

            var results = matcher.Search(node);

            NUnit::Assert.That(results, NUnit::Has.Exactly(result).Items);
        }

        [NUnit::TestCase("a + a - b / c * 3 + d", 3)]
        [NUnit::TestCase("b + b / d", 0)]
        public void TestGenericNode(string code, int result)
        {
            var node = SyntaxFactory.ParseExpression(code);
            var matcher = Is.Match<IdentifierNameSyntax>(f => f is { Identifier.Text: "a" or "c" });

            var results = matcher.Search(node);

            NUnit::Assert.That(results, NUnit::Has.Exactly(result).Items);
        }

        [NUnit::TestCase("a + a - b / c * 3 + d", 5)]
        [NUnit::TestCase("b + b / d", 3)]
        public void TestGenericNoFunc(string code, int result)
        {
            var node = SyntaxFactory.ParseExpression(code);
            var matcher = Is.Match<IdentifierNameSyntax>();

            var results = matcher.Search(node);

            NUnit::Assert.That(results, NUnit::Has.Exactly(result).Items);
        }

        [NUnit::TestCase("a + a - b / c * 3 + d", 3)]
        [NUnit::TestCase("b + b / d", 0)]
        public void TestGenericCombine(string code, int result)
        {
            var node = SyntaxFactory.ParseExpression(code);
            var matcher = Is.Match<IdentifierNameSyntax>().Then(f => f.Identifier.Text is "a" or "c");

            var results = matcher.Search(node);

            NUnit::Assert.That(results, NUnit::Has.Exactly(result).Items);
        }
    }
}