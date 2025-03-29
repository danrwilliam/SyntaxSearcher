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
        public void Descendant(string source, bool expected)
        {
            var matcher = Is.CompilationUnit.HasDescendant(
                Is.ClassDeclaration.WithMembers(SyntaxList.NotEmpty()));
            var expr = SyntaxFactory.ParseCompilationUnit(source);
            NUnit::Assert.That(matcher.IsMatch(expr), NUnit::Is.EqualTo(expected));
        }

        [NUnit::TestCase("""
            public void Test(int[] array, int[] dest, int offset)
            {
                dest[0] = array[0];

                for (int i = 1; i < array.Length; i++)
                {
                    dest[i] = array[i];
                }
            }
            """, 1
            )]
        [NUnit::TestCase("""
            public void Test(int[] array, int[] dest, int offset)
            {
                dest[0] = array[0];
            }
            """, 0
            )]
        public void Ancestor(string source, int matches)
        {
            var expr = SyntaxFactory.ParseMemberDeclaration(source);
            var matcher = Is.SimpleAssignmentExpression
                .WithLeft(
                    Is.ElementAccessExpression
                        .WithArgumentList(Is.BracketedArgumentList.WithArguments(
                            Is.Anything.Capture("index")
                            )))
                .WithRight(
                    Is.ElementAccessExpression
                        .WithArgumentList(Is.BracketedArgumentList.WithArguments(
                            Does.Match("index")
                        )))
                .HasAncestor(Is.ForStatement);

            NUnit::Assert.That(matcher.Search(expr), NUnit::Has.Exactly(matches).Items);
        }
    }
}