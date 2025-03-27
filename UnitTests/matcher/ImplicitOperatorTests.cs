using Microsoft.CodeAnalysis.CSharp;
using NUnit = NUnit.Framework;
using SyntaxSearch;
using SyntaxSearch.Framework;

namespace SyntaxSearchUnitTests.Matcher
{
    public class ImplicitOperatorTests
    {
        [NUnit::TestCase("invocation(1, null, false)", true)]
        [NUnit::TestCase("invocation(456, null, false)", true)]
        [NUnit::TestCase("invocation(1, null, true)", false)]
        [NUnit::TestCase("new invocation()", false)]
        public void Argument(string expr, bool expected)
        {
            var a = SyntaxFactory.ParseExpression(expr);

            var matcher = Is.InvocationExpression.WithArgumentList(
                Is.ArgumentList.WithArguments(
                    Is.NumericLiteralExpression,
                    Is.NullLiteralExpression,
                    Is.FalseLiteralExpression));

            NUnit::Assert.That(matcher.IsMatch(a), NUnit::Is.EqualTo(expected));
        }

        [NUnit::TestCase("a.test", true)]
        [NUnit::TestCase("a.abc", false)]
        [NUnit::TestCase("aege[5344].test", true)]
        public void IdentifierName(string identifier, bool expected)
        {
            var a = SyntaxFactory.ParseExpression(identifier);
            var matcher = Is.SimpleMemberAccessExpression.WithName("test");
            NUnit::Assert.That(matcher.IsMatch(a), NUnit::Is.EqualTo(expected));
        }
    }
}