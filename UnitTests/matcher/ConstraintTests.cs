using Microsoft.CodeAnalysis.CSharp;
using NUnit = NUnit.Framework;
using SyntaxSearch;
using SyntaxSearch.Framework;

namespace SyntaxSearchUnitTests.Matcher
{
    [NUnit::TestFixture]
    public class ConstraintTests
    {
        [NUnit::TestCase(@"
            if (a != null)
            {
                a.Expression();
            }", 1)]
        [NUnit::TestCase(@"
            if (obj.element != null)
            {
                obj.element.Expression();
            }", 1)]
        [NUnit::TestCase(@"
            if (obj.element != null)
            {
                obj.element.Expression(arg1, arg2, arg3);
            }", 1)]
        [NUnit::TestCase(@"
            if (obj.element != null)
            {
                obj.value.Expression();
            }", 0)]
        public void IfStatementInvocation(string source, int expected)
        {
            var expr = SyntaxFactory.ParseStatement(source);
            var match = Is.IfStatement
                .WithCondition(Is.NotEqualsExpression
                    .WithLeft(Is.Anything.Capture("obj"))
                    .WithRight(Is.NullLiteralExpression))
                .WithStatement(Is.Block
                    .WithStatements(
                        Is.ExpressionStatement
                            .WithExpression(Is.InvocationExpression
                                .WithExpression(Is.SimpleMemberAccessExpression
                                    .WithExpression(Does.Match("obj"))))));

            var search = new Searcher(match);

            NUnit::Assert.That(search.Search(expr), NUnit::Has.Exactly(expected).Items);
        }

        [NUnit::TestCase("1 + 1000", 1)]
        [NUnit::TestCase("1.43 + 433.1", 1)]
        [NUnit::TestCase("1 + b", 0)]
        [NUnit::TestCase("1 - b", 0)]
        public void Add(string source, int expected)
        {
            var match = Is.AddExpression
                .WithLeft(Is.NumericLiteralExpression)
                .WithRight(Is.NumericLiteralExpression);
            var expr = SyntaxFactory.ParseExpression(source);
            var searcher = new Searcher(match);
            NUnit::Assert.That(searcher.Search(expr), NUnit::Has.Exactly(expected).Items);
        }

        [NUnit::TestCase("1+1", true)]
        [NUnit::TestCase("1-1", true)]
        [NUnit::TestCase("1*1", true)]
        [NUnit::TestCase("1/1", true)]
        [NUnit::TestCase("1 += 1", false)]
        [NUnit::TestCase("1 /= 1", false)]
        [NUnit::TestCase("1 *= 1", false)]
        [NUnit::TestCase("1 /= 1", false)]
        public void OneOf(string source, bool isMatch)
        {
            var match = Is.OneOf(
                Is.AddExpression,
                Is.SubtractExpression,
                Is.MultiplyExpression,
                Is.DivideExpression);
            var expr = SyntaxFactory.ParseExpression(source);
            NUnit::Assert.That(match.IsMatch(expr), NUnit::Is.EqualTo(isMatch));
        }

        [NUnit::TestCase("a = 1+1", true)]
        [NUnit::TestCase("a = 1-1", true)]
        [NUnit::TestCase("a = 1*1", true)]
        [NUnit::TestCase("a = 1/1", true)]
        [NUnit::TestCase("a = 1 += 1", false)]
        [NUnit::TestCase("a = 1 /= 1", false)]
        [NUnit::TestCase("a = 1 *= 1", false)]
        [NUnit::TestCase("a = 1 /= 1", false)]
        public void OneOfAsChild(string source, bool isMatch)
        {
            var match = Is.SimpleAssignmentExpression.WithRight(
                Is.OneOf(
                Is.AddExpression,
                Is.SubtractExpression,
                Is.MultiplyExpression,
                Is.DivideExpression));
            var expr = SyntaxFactory.ParseExpression(source);
            NUnit::Assert.That(match.IsMatch(expr), NUnit::Is.EqualTo(isMatch));
        }

        [NUnit::TestCase("if (a != null) { block(); }", true)]
        [NUnit::TestCase("if (a != null) { block(); } else { otherStuff(); }", false)]
        [NUnit::TestCase("if (a != null) block();", true)]
        [NUnit::TestCase("if (a != null) block(); else otherStuff();", false)]
        public void IfWithoutElse(string source, bool expected)
        {
            var matcher = Is.IfStatement.WithElse(Is.Null);
            var node = SyntaxFactory.ParseStatement(source);
            NUnit::Assert.That(matcher.IsMatch(node), NUnit::Is.EqualTo(expected));
        }
    }
}