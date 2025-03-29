using Microsoft.CodeAnalysis.CSharp;
using SyntaxSearch;
using SyntaxSearch.Framework;
using SyntaxSearch.Matchers;
using System.Linq;
using NUnit = NUnit.Framework;

namespace SyntaxSearchUnitTests.Matcher
{
    [NUnit::TestFixture]
    public class DescendantTest
    {
        [NUnit::TestCase("public class Test { private int _field; }", true)]
        [NUnit::TestCase("public class Test { }", false)]
        [NUnit::TestCase("struct Test { public int _field; }", false)]
        public void ContainsField(string source, bool expected)
        {
            var matcher = Is.CompilationUnit.HasDescendant(
                Is.ClassDeclaration.WithMembers(SyntaxList.NotEmpty()));
            var expr = SyntaxFactory.ParseCompilationUnit(source);
            NUnit::Assert.That(matcher.IsMatch(expr), NUnit::Is.EqualTo(expected));
        }
    }
}