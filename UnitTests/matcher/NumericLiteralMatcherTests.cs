using Microsoft.CodeAnalysis.CSharp.Syntax;
using SyntaxSearch.Framework;
using SyntaxSearch;
using NUnit = NUnit.Framework;
using Microsoft.CodeAnalysis.CSharp;

namespace SyntaxSearchUnitTests.Matcher
{
    [NUnit::TestFixture]
    public class NumericLiteralMatcherTests
    {
        [NUnit::TestCase("1", 1, true)]
        [NUnit::TestCase("1", 2, false)]
        public void TestInt(string expr, int value, bool match)
        {
            var node = SyntaxFactory.ParseExpression(expr);

            var matcher = Is.NumericLiteralExpression.WithNumber(value);

            NUnit::Assert.That(matcher.IsMatch(node), NUnit::Is.EqualTo(match));
        }

        [NUnit::TestCase("1.5", 1.5, true)]
        [NUnit::TestCase("1.27", 2, false)]
        public void TestFloat(string expr, double value, bool match)
        {
            var node = SyntaxFactory.ParseExpression(expr);

            var matcher = Is.NumericLiteralExpression.WithNumber(value);

            NUnit::Assert.That(matcher.IsMatch(node), NUnit::Is.EqualTo(match));
        }
    }
}