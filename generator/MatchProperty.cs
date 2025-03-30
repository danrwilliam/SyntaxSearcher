using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace SyntaxSearcher.Generators
{
    internal record struct PropertyWrapper(
        ITypeSymbol Type,
        string Name,
        ImmutableArray<ITypeSymbol> TypeArguments)
    {
        public PropertyWrapper(IPropertySymbol p)
            : this(p.Type, p.Name, p switch
            {
                INamedTypeSymbol n => n.TypeArguments,
                _ => ImmutableArray<ITypeSymbol>.Empty
            })
        {
        }
    }

    internal record struct MatchProperty(
        PropertyWrapper Property, PropertyKind GeneratorKind)
    {
        public static implicit operator (PropertyWrapper, PropertyKind)(MatchProperty value)
        {
            return (value.Property, value.GeneratorKind);
        }

        public static implicit operator MatchProperty((IPropertySymbol, PropertyKind) value)
        {
            return new MatchProperty(new PropertyWrapper(value.Item1), value.Item2);
        }
    }
}