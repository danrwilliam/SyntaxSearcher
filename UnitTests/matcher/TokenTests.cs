using Microsoft.CodeAnalysis.CSharp;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using NUnit = NUnit.Framework;
using SyntaxSearch;
using SyntaxSearch.Framework;

namespace SyntaxSearchUnitTests.Matcher
{
    [NUnit::TestFixture]
    public class TokenTests
    {
        [NUnit::Test]
        public void TestIdentifier()
        {
            var matcher = Is.Identifier.WithKind(SyntaxKind.IdentifierToken);
            var token = Identifier("Test");

            NUnit::Assert.That(matcher.IsMatch(token, default), NUnit::Is.True);
        }

        [NUnit::TestCase(10.01)]
        public void DoubleToken(double value)
        {
            var token = ParseToken(value.ToString());
            var matcher = Is.Number(value);
            NUnit::Assert.That(matcher.IsMatch(token, default), NUnit::Is.True);
        }

        [NUnit::TestCase(10)]
        public void IntToken(int value)
        {
            var token = ParseToken(value.ToString());
            var matcher = Is.Number(value);
            NUnit::Assert.That(matcher.IsMatch(token, default), NUnit::Is.True);
        }
    }
}