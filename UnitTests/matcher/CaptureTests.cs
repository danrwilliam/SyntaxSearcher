using SyntaxSearch.Framework;
using SyntaxSearch;
using NUnit = NUnit.Framework;
using Microsoft.CodeAnalysis.CSharp;

namespace SyntaxSearchUnitTests.Matcher
{
    [NUnit::TestFixture]
    public class CaptureTests
    {
        [NUnit::Test]
        public void TestIdentifier()
        {
            var nullCheckMatcher = Is.IfStatement
                .WithCondition(
                    Is.NotEqualsExpression
                        .WithLeft(Is.IdentifierName.Capture("name"))
                        .WithRight(Is.NullLiteralExpression))
                .WithStatement(
                    Is.ExpressionStatement
                        .WithExpression(
                            Is.InvocationExpression
                                .WithExpression(
                                    Is.SimpleMemberAccessExpression
                                        .WithExpression(Does.Match("name")))));

            var expr = SyntaxFactory.ParseStatement($@"
                if (a != null)
                    a.Method();
");

            NUnit::Assert.That(nullCheckMatcher.IsMatch(expr), NUnit::Is.True);
        }

        [NUnit::Test]
        public void TestAnything()
        {
            var nullCheckMatcher = Is.IfStatement
                .WithCondition(
                    Is.NotEqualsExpression
                        .WithLeft(Is.Anything.Capture("name"))
                        .WithRight(Is.NullLiteralExpression))
                .WithStatement(
                    Is.ExpressionStatement
                        .WithExpression(
                            Is.InvocationExpression
                                .WithExpression(
                                    Is.SimpleMemberAccessExpression
                                        .WithExpression(Does.Match("name")))));

            var expr = SyntaxFactory.ParseStatement($@"
                if (a.b.c.d.e != null)
                    a.b.c.d.e.Method();
");

            NUnit::Assert.That(nullCheckMatcher.IsMatch(expr), NUnit::Is.True);
        }

        [NUnit::Test]
        public void TestDoesNotMatchAnything()
        {
            var nullCheckMatcher = Is.IfStatement
                .WithCondition(
                    Is.NotEqualsExpression
                        .WithLeft(Is.Anything.Capture("name"))
                        .WithRight(Is.NullLiteralExpression))
                .WithStatement(
                    Is.ExpressionStatement
                        .WithExpression(
                            Is.InvocationExpression
                                .WithExpression(
                                    Is.SimpleMemberAccessExpression
                                        .WithExpression(Does.Match("name")))));

            var expr = SyntaxFactory.ParseStatement($@"
                if (a.b.c.d.e != null)
                    a.b.c.d.e.g.Method();
");

            NUnit::Assert.That(nullCheckMatcher.IsMatch(expr), NUnit::Is.False);
        }

        [NUnit::Test]
        public void IdenticalBranches()
        {
            var matcher = Is.IfStatement
                .WithStatement(Is.Anything.Capture("trueBranch"))
                .WithElse(Is.ElseClause.WithStatement(Does.Match("trueBranch")));

            var expr = SyntaxFactory.ParseStatement($@"
                if (a.b.Value > 100)
                {{
                    Execute(a);
                }}
                else
                {{
                    Execute(a);
                }}");

            NUnit::Assert.That(matcher.IsMatch(expr), NUnit::Is.True);
        }
    }
}