using Microsoft.CodeAnalysis.CSharp;
using NUnit = NUnit.Framework;
using SyntaxSearch;
using SyntaxSearch.Framework;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

using TestCase = NUnit.Framework.TestCaseAttribute;
using Assert = NUnit.Framework.Assert;

namespace SyntaxSearchUnitTests.Matcher
{
    public class RewriterTests
    {
        [TestCase("a != null", "b != null")]
        [TestCase("a.Call()", "b.Call()")]
        [TestCase("A a = new A()", "A a = new A()")]
        public void Basic(string input, string expected)
        {
            var @in = SyntaxFactory.ParseExpression(input);
            var @out = SyntaxFactory.ParseExpression(expected);

            var matcher = Is.IdentifierName.WithIdentifier(Is.Identifier.WithText("a"));

            var rewritten = matcher.Rewrite(a => IdentifierName("b"), @in);

            Assert.That(SyntaxFactory.AreEquivalent(rewritten, @out), NUnit::Is.True);
        }
    }
}