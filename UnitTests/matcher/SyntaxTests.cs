using Microsoft.CodeAnalysis.CSharp;
using NUnit = NUnit.Framework;
using SyntaxSearch;
using SyntaxSearch.Framework;

namespace SyntaxSearchUnitTests.Matcher
{
    [NUnit::TestFixture]
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
        [NUnit::TestCase("base.Value + base.Value.Test", 3)]
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