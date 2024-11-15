using Microsoft.CodeAnalysis.CSharp;
using SyntaxSearch.Framework;
using SyntaxSearch;
using NUnit = NUnit.Framework;

namespace SyntaxSearchUnitTests.Matcher
{
    [NUnit::TestFixture]
    public class NameMatcherTests
    {
        [NUnit::TestCase("A.Name", 1)]
        [NUnit::TestCase("Test.A.Name", 1)]
        [NUnit::TestCase("B.Name", 0)]
        [NUnit::TestCase("Test.B.Name", 0)]
        [NUnit::TestCase("Test.A.Name.Extra", 1)]
        [NUnit::TestCase("Test.A.Name(value, A.Name)", 2)]
        public void Test(string input, int numMatches)
        {
            var expr = SyntaxFactory.ParseStatement($"var obj = {input};");
            var searcher = Is.SimpleMemberAccessExpression
                .WithName(Is.IdentifierName.WithText("Name"))
                .WithExpression(Is.MemberName.WithName(Is.IdentifierName.WithText("A")));
            NUnit::Assert.That(searcher.Search(expr), NUnit::Has.Exactly(numMatches).Items);
        }
    }
}