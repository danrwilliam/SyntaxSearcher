using Microsoft.CodeAnalysis.CSharp;
using NUnit = NUnit.Framework;
using SyntaxSearch;
using SyntaxSearch.Framework;
using SyntaxSearch.Matchers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace SyntaxSearchUnitTests.Matcher
{
    [NUnit::TestFixture]
    public class SyntaxListTests
    {
        private const string ClassWith3Members = $@"
    public class Test
    {{
        private readonly int _field;
        public int Value => _field;

        public Test(int value)
        {{
            _field = value;
        }}
    }}
";
        private const string EmptyClass = $@"
    public class Test
    {{
    }}
";

        [NUnit::TestCase(1, false)]
        [NUnit::TestCase(2, false)]
        [NUnit::TestCase(3, true)]
        [NUnit::TestCase(4, false)]
        public void Equals(int number, bool expected)
        {
            var unit = SyntaxFactory.ParseCompilationUnit(ClassWith3Members).DescendantNodes(f => true).OfType<ClassDeclarationSyntax>().First();
            var matcher = SyntaxList.Length() == number;

            NUnit::Assert.That(matcher.IsMatch(unit.Members, null), NUnit::Is.EqualTo(expected));
        }

        [NUnit::TestCase(1, true)]
        [NUnit::TestCase(2, true)]
        [NUnit::TestCase(3, false)]
        [NUnit::TestCase(4, true)]
        public void NotEquals(int number, bool expected)
        {
            var unit = SyntaxFactory.ParseCompilationUnit(ClassWith3Members).DescendantNodes(f => true).OfType<ClassDeclarationSyntax>().First();
            var matcher = SyntaxList.Length() != number;

            NUnit::Assert.That(matcher.IsMatch(unit.Members, null), NUnit::Is.EqualTo(expected));
        }

        [NUnit::TestCase(1, false)]
        [NUnit::TestCase(2, false)]
        [NUnit::TestCase(3, false)]
        [NUnit::TestCase(4, true)]
        public void LessThan(int number, bool expected)
        {
            var unit = SyntaxFactory.ParseCompilationUnit(ClassWith3Members).DescendantNodes(f => true).OfType<ClassDeclarationSyntax>().First();
            var matcher = SyntaxList.Length() < number;
            NUnit::Assert.That(matcher.IsMatch(unit.Members, null), NUnit::Is.EqualTo(expected));
        }

        [NUnit::TestCase(1, true)]
        [NUnit::TestCase(2, true)]
        [NUnit::TestCase(3, false)]
        [NUnit::TestCase(4, false)]
        public void GreaterThan(int number, bool expected)
        {
            var unit = SyntaxFactory.ParseCompilationUnit(ClassWith3Members).DescendantNodes(f => true).OfType<ClassDeclarationSyntax>().First();
            var matcher = SyntaxList.Length() > number;
            NUnit::Assert.That(matcher.IsMatch(unit.Members, null), NUnit::Is.EqualTo(expected));
        }

        [NUnit::TestCase(1, false)]
        [NUnit::TestCase(2, false)]
        [NUnit::TestCase(3, true)]
        [NUnit::TestCase(4, true)]
        public void LessThanEqual(int number, bool expected)
        {
            var unit = SyntaxFactory.ParseCompilationUnit(ClassWith3Members).DescendantNodes(f => true).OfType<ClassDeclarationSyntax>().First();
            var matcher = SyntaxList.Length() <= number;
            NUnit::Assert.That(matcher.IsMatch(unit.Members, null), NUnit::Is.EqualTo(expected));
        }

        [NUnit::TestCase(1, true)]
        [NUnit::TestCase(2, true)]
        [NUnit::TestCase(3, true)]
        [NUnit::TestCase(4, false)]
        public void GreaterThanEqual(int number, bool expected)
        {
            var unit = SyntaxFactory.ParseCompilationUnit(ClassWith3Members).DescendantNodes(f => true).OfType<ClassDeclarationSyntax>().First();
            var matcher = SyntaxList.Length() >= number;
            NUnit::Assert.That(matcher.IsMatch(unit.Members, null), NUnit::Is.EqualTo(expected));
        }

        [NUnit::Test]
        public void NotEmpty_3Members()
        {
            var unit = SyntaxFactory.ParseCompilationUnit(ClassWith3Members).DescendantNodes(f => true).OfType<ClassDeclarationSyntax>().First();
            var matcher = SyntaxList.NotEmpty();
            NUnit::Assert.That(matcher.IsMatch(unit.Members, null), NUnit::Is.EqualTo(true));
        }

        [NUnit::Test]
        public void IsEmpty_3Members()
        {
            var unit = SyntaxFactory.ParseCompilationUnit(ClassWith3Members).DescendantNodes(f => true).OfType<ClassDeclarationSyntax>().First();
            var matcher = SyntaxList.IsEmpty();
            NUnit::Assert.That(matcher.IsMatch(unit.Members, null), NUnit::Is.EqualTo(false));
        }

        [NUnit::Test]
        public void NotEmpty_Empty()
        {
            var unit = SyntaxFactory.ParseCompilationUnit(EmptyClass).DescendantNodes(f => true).OfType<ClassDeclarationSyntax>().First();
            var matcher = SyntaxList.NotEmpty();
            NUnit::Assert.That(matcher.IsMatch(unit.Members, null), NUnit::Is.EqualTo(false));
        }

        [NUnit::Test]
        public void IsEmpty_Empty()
        {
            var unit = SyntaxFactory.ParseCompilationUnit(EmptyClass).DescendantNodes(f => true).OfType<ClassDeclarationSyntax>().First();
            var matcher = SyntaxList.IsEmpty();
            NUnit::Assert.That(matcher.IsMatch(unit.Members, null), NUnit::Is.EqualTo(true));
        }

        [NUnit::TestCase(EmptyClass, true)]
        [NUnit::TestCase(ClassWith3Members, false)]
        public void EqualTo_Empty(string source, bool expected)
        {
            var unit = SyntaxFactory.ParseCompilationUnit(source).DescendantNodes(f => true).OfType<ClassDeclarationSyntax>().First();
            var matcher = Is.ClassDeclaration.WithMembers();
            NUnit::Assert.That(matcher.IsMatch(unit, null), NUnit::Is.EqualTo(expected));
        }

        [NUnit::TestCase(EmptyClass, false)]
        [NUnit::TestCase(ClassWith3Members, true)]
        public void EqualTo_3(string source, bool expected)
        {
            var unit = SyntaxFactory.ParseCompilationUnit(source).DescendantNodes(f => true).OfType<ClassDeclarationSyntax>().First();
            var matcher = Is.ClassDeclaration
                .WithMembers(
                    Is.FieldDeclaration,
                    Is.PropertyDeclaration,
                    Is.ConstructorDeclaration
                );
            NUnit::Assert.That(matcher.IsMatch(unit, null), NUnit::Is.EqualTo(expected));
        }

        [NUnit::TestCase(EmptyClass, false)]
        [NUnit::TestCase(ClassWith3Members, true)]
        public void Contains(string source, bool expected)
        {
            var unit = SyntaxFactory.ParseCompilationUnit(source).DescendantNodes(f => true).OfType<ClassDeclarationSyntax>().First();
            var matcher = Is.ClassDeclaration.WithMembers(Does.Contain(Is.PropertyDeclaration));
            NUnit::Assert.That(matcher.IsMatch(unit, null), NUnit::Is.EqualTo(expected));
        }
    }
}