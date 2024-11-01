using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using NUnit = NUnit.Framework;
using SyntaxSearch;
using SyntaxSearch.Framework;
using System.Collections.Generic;

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

        private static IEnumerable<SyntaxKind> BuiltInTypeKinds()
        {
            return [
                SyntaxKind.BoolKeyword,
                SyntaxKind.IntKeyword,
                SyntaxKind.UIntKeyword,
                SyntaxKind.ULongKeyword,
                SyntaxKind.LongKeyword,
                SyntaxKind.StringKeyword,
                SyntaxKind.UShortKeyword,
                SyntaxKind.ShortKeyword,
                SyntaxKind.CharKeyword,
                SyntaxKind.ByteKeyword,
                SyntaxKind.SByteKeyword
                ];
        }

        [NUnit::Test]
        public void BuiltInType(
            [NUnit::ValueSource(nameof(BuiltInTypeKinds))]
            SyntaxKind matchKind, 
            [NUnit::ValueSource(nameof(BuiltInTypeKinds))]
            SyntaxKind typeKind)
        {
            var matcher = Is.PropertyDeclaration.WithType(
                Is.PredefinedType.WithKeyword(matchKind));

            var property = PropertyDeclaration(PredefinedType(Token(typeKind)), Identifier("Test"));

            bool expected = matchKind == typeKind;

            NUnit::Assert.That(matcher.IsMatch(property), NUnit::Is.EqualTo(expected));

        }
    }
}