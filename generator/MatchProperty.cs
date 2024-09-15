using Microsoft.CodeAnalysis;

namespace SyntaxSearcher.Generators
{
    internal record struct MatchProperty(IPropertySymbol Property, bool IsList)
    {
        public static implicit operator (IPropertySymbol, bool)(MatchProperty value)
        {
            return (value.Property, value.IsList);
        }

        public static implicit operator MatchProperty((IPropertySymbol, bool) value)
        {
            return new MatchProperty(value.Item1, value.Item2);
        }
    }
}