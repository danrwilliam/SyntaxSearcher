using Microsoft.CodeAnalysis.CSharp;
using SyntaxSearch;
using SyntaxSearch.Framework;
using SyntaxSearch.Matchers;
using NUnit = NUnit.Framework;

namespace SyntaxSearchUnitTests.Matcher
{
    [NUnit::TestFixture]
    public class PropertyDeclarationTests
    {
        [NUnit::TestCase("static bool Value { get; }", true)]
        [NUnit::TestCase("bool Value { get; }", false)]
        public void IsStatic(string source, bool expected)
        {
            var matcher = Is.PropertyDeclaration.IsStatic();
            var node = SyntaxFactory.ParseMemberDeclaration(source);

            NUnit::Assert.That(matcher.IsMatch(node), NUnit::Is.EqualTo(expected));
        }

        [NUnit::TestCase("public bool Value { get; }", true)]
        [NUnit::TestCase("bool Value { get; }", false)]
        public void IsPublic(string source, bool expected)
        {
            var matcher = Is.PropertyDeclaration.IsPublic();
            var node = SyntaxFactory.ParseMemberDeclaration(source);

            NUnit::Assert.That(matcher.IsMatch(node), NUnit::Is.EqualTo(expected));
        }

        [NUnit::TestCase("private bool Value { get; }", true)]
        [NUnit::TestCase("bool Value { get; }", false)]
        public void IsPrivate(string source, bool expected)
        {
            var matcher = Is.PropertyDeclaration.IsPrivate();
            var node = SyntaxFactory.ParseMemberDeclaration(source);

            NUnit::Assert.That(matcher.IsMatch(node), NUnit::Is.EqualTo(expected));
        }

        [NUnit::TestCase("internal bool Value { get; }", true)]
        [NUnit::TestCase("bool Value { get; }", false)]
        public void IsInternal(string source, bool expected)
        {
            var matcher = Is.PropertyDeclaration.IsInternal();
            var node = SyntaxFactory.ParseMemberDeclaration(source);

            NUnit::Assert.That(matcher.IsMatch(node), NUnit::Is.EqualTo(expected));
        }

        [NUnit::TestCase("abstract bool Value { get; }", true)]
        [NUnit::TestCase("bool Value { get; }", false)]
        public void IsAbstract(string source, bool expected)
        {
            var matcher = Is.PropertyDeclaration.IsAbstract();
            var node = SyntaxFactory.ParseMemberDeclaration(source);
            NUnit::Assert.That(matcher.IsMatch(node), NUnit::Is.EqualTo(expected));
        }

        [NUnit::TestCase("public static bool Value { get; }", true)]
        [NUnit::TestCase("public bool Value { get; }", false)]
        [NUnit::TestCase("static bool Value { get; }", false)]
        public void IsStaticPublic(string source, bool expected)
        {
            var matcher = Is.PropertyDeclaration.IsStatic().IsPublic();
            var node = SyntaxFactory.ParseMemberDeclaration(source);
            NUnit::Assert.That(matcher.IsMatch(node), NUnit::Is.EqualTo(expected));
        }

        [NUnit::TestCase("protected internal abstract bool Value { get; }", true)]
        [NUnit::TestCase("internal abstract bool Value { get; }", false)]
        [NUnit::TestCase("protected abstract bool Value { get; }", false)]
        [NUnit::TestCase("bool Value { get; }", false)]
        public void IsProtectedInternalAbstract(string source, bool expected)
        {
            var matcher = Is.PropertyDeclaration.IsProtected().IsInternal().IsAbstract();
            var node = SyntaxFactory.ParseMemberDeclaration(source);
            NUnit::Assert.That(matcher.IsMatch(node), NUnit::Is.EqualTo(expected));
        }
    }
}