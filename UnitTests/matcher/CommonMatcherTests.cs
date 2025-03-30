using Microsoft.CodeAnalysis.CSharp;
using NUnit = NUnit.Framework;
using SyntaxSearch;
using SyntaxSearch.Framework;

namespace SyntaxSearchUnitTests.Matcher
{
    [NUnit::TestFixture]
    public class CommonMatcherTests
    {
        [NUnit::TestCase("a + b", true)]
        [NUnit::TestCase("a + 321.1", true)]
        [NUnit::TestCase("a * b", true)]
        [NUnit::TestCase("a / b", true)]
        [NUnit::TestCase("a - b", true)]
        public void BinaryExpression(string text, bool expected)
        {
            var expr = SyntaxFactory.ParseExpression(text);
            NUnit::Assert.That(Is.BinaryExpression.IsMatch(expr), NUnit::Is.EqualTo(expected));
        }

        [NUnit::TestCase("a + b", true)]
        [NUnit::TestCase("a + 321.1", true)]
        [NUnit::TestCase("a * b", false)]
        [NUnit::TestCase("a / b", false)]
        [NUnit::TestCase("a - b", false)]
        public void BinaryExpressionKind(string text, bool expected)
        {
            var expr = SyntaxFactory.ParseExpression(text);
            NUnit::Assert.That(Is.BinaryExpression.WithKind(SyntaxKind.AddExpression).IsMatch(expr), NUnit::Is.EqualTo(expected));
        }
    }
}