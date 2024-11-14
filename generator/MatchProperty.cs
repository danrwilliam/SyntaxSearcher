using Microsoft.CodeAnalysis;

namespace SyntaxSearcher.Generators
{
    internal record struct MatchProperty(IPropertySymbol Property, PropertyKind GeneratorKind)
    {
        public static implicit operator (IPropertySymbol, PropertyKind)(MatchProperty value)
        {
            return (value.Property, value.GeneratorKind);
        }

        public static implicit operator MatchProperty((IPropertySymbol, PropertyKind) value)
        {
            return new MatchProperty(value.Item1, value.Item2);
        }
    }
}