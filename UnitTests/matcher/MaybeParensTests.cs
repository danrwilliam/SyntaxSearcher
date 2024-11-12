using Microsoft.CodeAnalysis.CSharp;
using NUnit = NUnit.Framework;
using SyntaxSearch;
using SyntaxSearch.Framework;

using TestCase = NUnit.Framework.TestCaseAttribute;
using Assert = NUnit.Framework.Assert;

namespace SyntaxSearchUnitTests.Matcher
{
    public class MaybeParensTests
    {
        [TestCase("a != null", true)]
        [TestCase("(a != null)", true)]
        [TestCase("((a != null))", true)]
        [TestCase("a + b", false)]
        public void Basic(string test, bool expected)
        {
            var matcher = Maybe.Parentheses.WithExpression(Is.NotEqualsExpression.WithRight(Is.NullLiteralExpression));
            var expr = SyntaxFactory.ParseExpression(test);

            Assert.That(matcher.IsMatch(expr), NUnit::Is.EqualTo(expected));
        }
    }
}