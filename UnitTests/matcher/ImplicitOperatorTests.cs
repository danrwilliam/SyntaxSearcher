using Microsoft.CodeAnalysis.CSharp;
using NUnit = NUnit.Framework;
using SyntaxSearch;
using SyntaxSearch.Framework;
using SyntaxSearch.Matchers.Explicit;
using SyntaxSearch.Matchers;

namespace SyntaxSearchUnitTests.Matcher
{
    [NUnit::TestFixture("invocation(1, null, false)", true)]
    [NUnit::TestFixture("invocation(456, null, false)", true)]
    [NUnit::TestFixture("invocation(1, null, true)", false)]
    [NUnit::TestFixture("new invocation()", false)]
    public class InvocationImplicitOperatorTests(string expr, bool expected)
    {
        [NUnit::Test]
        public void ThroughArgumentList()
        {
            var a = SyntaxFactory.ParseExpression(expr);

            var matcher = Is.InvocationExpression.WithArgumentList(
                Is.ArgumentList.WithArguments(
                    Is.NumericLiteralExpression,
                    Is.NullLiteralExpression,
                    Is.FalseLiteralExpression));

            NUnit::Assert.That(matcher.IsMatch(a), NUnit::Is.EqualTo(expected));
        }

        [NUnit::Test]
        public void ImplicitArguments()
        {
            var a = SyntaxFactory.ParseExpression(expr);

            var matcher = Is.InvocationExpression.WithArguments(
                [
                    Is.NumericLiteralExpression,
                    Is.NullLiteralExpression,
                    Is.FalseLiteralExpression
                ]);

            NUnit::Assert.That(matcher.IsMatch(a), NUnit::Is.EqualTo(expected));
        }
    }

    [NUnit::TestFixture("invocation[1, null, false]", true)]
    [NUnit::TestFixture("invocation[456, null, false]", true)]
    [NUnit::TestFixture("invocation[ba]", false)]
    [NUnit::TestFixture("new invocation()", false)]
    public class ElementAccessTests(string expr, bool expected)
    {
        [NUnit::Test]
        public void ThroughArgumentList()
        {
            var a = SyntaxFactory.ParseExpression(expr);

            var matcher = Is.ElementAccessExpression.WithArgumentList(
                Is.BracketedArgumentList.WithArguments(
                    Is.NumericLiteralExpression,
                    Is.NullLiteralExpression,
                    Is.FalseLiteralExpression));

            NUnit::Assert.That(matcher.IsMatch(a), NUnit::Is.EqualTo(expected));
        }

        [NUnit::Test]
        public void ImplicitArguments()
        {
            var a = SyntaxFactory.ParseExpression(expr);

            var matcher = Is.ElementAccessExpression.WithArguments(
                [
                    Is.NumericLiteralExpression,
                    Is.NullLiteralExpression,
                    Is.FalseLiteralExpression
                ]);

            NUnit::Assert.That(matcher.IsMatch(a), NUnit::Is.EqualTo(expected));
        }
    }
    [NUnit::TestFixture("new invocation(1, null, false)", true)]
    [NUnit::TestFixture("new invocation(456, null, false)", true)]
    [NUnit::TestFixture("new invocation(ba)", false)]
    [NUnit::TestFixture("new invocation()", false)]
    public class ObjectCreationExpressionTests(string expr, bool expected)
    {
        [NUnit::Test]
        public void ThroughArgumentList()
        {
            var a = SyntaxFactory.ParseExpression(expr);

            var matcher = Is.ObjectCreationExpression.WithArgumentList(
                Is.ArgumentList.WithArguments(
                    Is.NumericLiteralExpression,
                    Is.NullLiteralExpression,
                    Is.FalseLiteralExpression));

            NUnit::Assert.That(matcher.IsMatch(a), NUnit::Is.EqualTo(expected));
        }

        [NUnit::Test]
        public void ImplicitArguments()
        {
            var a = SyntaxFactory.ParseExpression(expr);

            var matcher = Is.ObjectCreationExpression.WithArguments(
                [
                    Is.NumericLiteralExpression,
                    Is.NullLiteralExpression,
                    Is.FalseLiteralExpression
                ]);

            NUnit::Assert.That(matcher.IsMatch(a), NUnit::Is.EqualTo(expected));
        }
    }

    public class IdentifierNameOperatorTests
    {
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

    //public class ArgumentListTests
    //{
    //    [NUnit::Test]
    //    public void TestSingle()
    //    {
    //        ArgumentListMatcher a = Is.IdentifierName;
    //        NUnit::Assert.That(a, NUnit::Is.Not.Null);
    //    }

    //    [NUnit::Test]
    //    public void TestArray()
    //    {
    //        ArgumentListMatcher a = new ExpressionSyntaxMatcher[]
    //        {
    //            Is.IdentifierName,
    //            Is.NullLiteralExpression
    //        };
    //        NUnit::Assert.That(a, NUnit::Is.Not.Null);
    //    }
    //}
}