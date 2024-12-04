using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace SyntaxSearcher.Generators
{
    public static class Extensions
    {
        public static IEnumerable<ISymbol> GetAllMembers(this ITypeSymbol type)
        {
            if (type is null)
                yield break;

            foreach (var member in type.GetMembers())
            {
                yield return member;
            }

            foreach (var member in type.BaseType.GetAllMembers())
            {
                yield return member;
            }
        }

        public static IEnumerable<ISymbol> GetAllMembers(this ITypeSymbol type, string name)
        {
            if (type is null)
                yield break;

            foreach (var member in type.GetMembers(name))
            {
                yield return member;
            }

            foreach (var member in type.BaseType.GetAllMembers(name))
            {
                yield return member;
            }
        }
    }
}