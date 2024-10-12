using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using SyntaxSearch;

namespace SyntaxSearchUnitTests.Matcher
{
    [TestFixture]
    public class ConstraintTests
    {
        [TestCase(@"
            if (a != null)
            {
                a.Expression();
            }", 1)]
        [TestCase(@"
            if (obj.element != null)
            {
                obj.element.Expression();
            }", 1)]
        [TestCase(@"
            if (obj.element != null)
            {
                obj.element.Expression(arg1, arg2, arg3);
            }", 1)]
        [TestCase(@"
            if (obj.element != null)
            {
                obj.value.Expression();
            }", 0)]
        public void IfStatementInvocation(string source, int expected)
        {
            var expr = SyntaxFactory.ParseStatement(source);
            var match = SyntaxSearch.Framework.Is.IfStatement
                .WithCondition(SyntaxSearch.Framework.Is.NotEqualsExpression
                    .WithLeft(SyntaxSearch.Framework.Is.Anything.Capture("obj"))
                    .WithRight(SyntaxSearch.Framework.Is.NullLiteralExpression))
                .WithStatement(SyntaxSearch.Framework.Is.Block
                    .WithStatements(
                        SyntaxSearch.Framework.Is.ExpressionStatement
                            .WithExpression(SyntaxSearch.Framework.Is.InvocationExpression
                                .WithExpression(SyntaxSearch.Framework.Is.SimpleMemberAccessExpression
                                    .WithExpression(SyntaxSearch.Framework.Does.Match("obj"))))));

            var search = new Searcher(match);

            Assert.That(search.Search(expr), Has.Exactly(expected).Items);
        }

        [TestCase("1 + 1000", 1)]
        [TestCase("1.43 + 433.1", 1)]
        [TestCase("1 + b", 0)]
        [TestCase("1 - b", 0)]
        public void Add(string source, int expected)
        {
            var match = SyntaxSearch.Framework.Is.AddExpression
                .WithLeft(SyntaxSearch.Framework.Is.NumericLiteralExpression)
                .WithRight(SyntaxSearch.Framework.Is.NumericLiteralExpression);
            var expr = SyntaxFactory.ParseExpression(source);
            var searcher = new Searcher(match);
            Assert.That(searcher.Search(expr), Has.Exactly(expected).Items);
        }
    }
}