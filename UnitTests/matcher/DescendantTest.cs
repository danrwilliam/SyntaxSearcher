using Microsoft.CodeAnalysis.CSharp;
using SyntaxSearch.Framework;
using System.Linq;
using NUnit = NUnit.Framework;

namespace SyntaxSearchUnitTests.Matcher
{
    //[NUnit::TestFixture]
    //public class DescendantTest
    //{
    //    [NUnit::TestCase("public class Test { private int _field; }", true)]
    //    [NUnit::TestCase("public class Test { }", false)]
    //    [NUnit::TestCase("struct Test { public int _field; }", false)]
    //    public void ContainsField(string source, bool expected)
    //    {
    //        var matcher = Is.CompilationUnit.Contains()
    //        var unit = SyntaxFactory.ParseCompilationUnit(source).DescendantNodes(f => true).First();
    //        NUnit::Assert.That(matcher.IsMatch(unit, null), NUnit::Is.EqualTo(expected));
    //    }
    //}
}