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

        [NUnit::TestCase(10.01, 10.01, true)]
        [NUnit::TestCase(10.01, 10.5, false)]
        public void DoubleNumeric(double value, double matchValue, bool expected)
        {
            var expr = LiteralExpression(SyntaxKind.NumericLiteralExpression).WithToken(
                ParseToken(value.ToString()));

            var matcher = Is.NumericLiteralExpression.WithToken(Is.Number(matchValue));

            NUnit::Assert.That(matcher.IsMatch(expr, default), NUnit::Is.EqualTo(expected));
        }

        [NUnit::Test]
        public void NullExpression()
        {
            var node = LiteralExpression(SyntaxKind.NullLiteralExpression);
            NUnit::Assert.That(Is.NullLiteralExpression.IsMatch(node, default), NUnit::Is.True);
        }

        [NUnit::Test]
        public void TrueExpression()
        {
            var node = LiteralExpression(SyntaxKind.TrueLiteralExpression);
            NUnit::Assert.That(Is.TrueLiteralExpression.IsMatch(node, default), NUnit::Is.True);
        }

        [NUnit::Test]
        public void FalseExpression()
        {
            var node = LiteralExpression(SyntaxKind.FalseLiteralExpression);
            NUnit::Assert.That(Is.FalseLiteralExpression.IsMatch(node, default), NUnit::Is.True);
        }

        [NUnit::Test]
        public void DefaultExpression()
        {
            var node = LiteralExpression(SyntaxKind.DefaultLiteralExpression);
            NUnit::Assert.That(Is.DefaultLiteralExpression.IsMatch(node, default), NUnit::Is.True);
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

        [NUnit::TestCase("public class Test { }", 1)]
        [NUnit::TestCase("private class Test { }", 0)]
        public void Modifier(string unit, int results)
        {
            var node = SyntaxFactory.ParseCompilationUnit(unit);
            var searcher = Is.ClassDeclaration.WithModifiers(Has.Modifiers(Is.Public));
            NUnit::Assert.That(searcher.Search(node), NUnit::Has.Exactly(results).Items);
        }

        [NUnit::TestCase("public class Test { }", 0)]
        [NUnit::TestCase("private static class Test { }", 1)]
        public void CompoundModifier(string unit, int results)
        {
            var node = SyntaxFactory.ParseCompilationUnit(unit);
            var searcher = Is.ClassDeclaration.WithModifiers(Has.Modifiers(Is.Private, Is.Static));
            NUnit::Assert.That(searcher.Search(node), NUnit::Has.Exactly(results).Items);
        }

        [NUnit::TestCase("public class Test { }", 1)]
        [NUnit::TestCase("public static class Test { }", 0)]
        [NUnit::TestCase("private static class Test { }", 0)]
        public void CompoundWithNot(string unit, int results)
        {
            var node = SyntaxFactory.ParseCompilationUnit(unit);
            var searcher = Is.ClassDeclaration.WithModifiers(Has.Modifiers(Is.Public, Not.Static));
            NUnit::Assert.That(searcher.Search(node), NUnit::Has.Exactly(results).Items);
        }

        [NUnit::TestCase("2")]
        [NUnit::TestCase("-2")]
        [NUnit::TestCase("+2")]
        [NUnit::TestCase("2 + 1")]
        [NUnit::TestCase("(2 + 1.5) / -0.5")]
        [NUnit::TestCase("(2e5 - 0.00000001) / +2.00001e-01")]
        [NUnit::TestCase("(2e5 - 0.00000001) / (+2.00001e-01 * 31.25 / 1 - 13 + 43")]
        [NUnit::TestCase("(long)211")]
        [NUnit::TestCase("(float)4 * (int)12.5")]
        public void NumericConstant(string source)
        {
            var expr = SyntaxFactory.ParseExpression(source);
            NUnit::Assert.That(Is.NumericConstantExpression.IsMatch(expr), NUnit::Is.True);
        }
    }
}