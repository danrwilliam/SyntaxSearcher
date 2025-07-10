using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SyntaxSearcher.Generators
{
    [AttributeUsage(AttributeTargets.Class)]
    sealed class HelperNameAttribute(string name) : Attribute
    {
        public string Name { get; } = name;
    }

    static class HelperCollector
    {
        public static ImmutableDictionary<string, IHelperMethodGenerator> Collect()
        {
            Dictionary<string, IHelperMethodGenerator> generators = [];

            foreach (var type in typeof(IHelperMethodGenerator).Assembly.GetTypes())
            {
                if (type.IsAssignableTo(typeof(IHelperMethodGenerator)))
                {
                    HelperNameAttribute nameAttr = type.GetCustomAttributes().OfType<HelperNameAttribute>().FirstOrDefault();

                    if (nameAttr is { Name: not null, Name: not "" })
                    {
                        generators[nameAttr.Name] = (IHelperMethodGenerator)Activator.CreateInstance(type);
                    }
                }
            }

            return generators.ToImmutableDictionary();
        }
    }

    /// <summary>
    /// Generates additional convienence methods if a type has a property with a specific name
    /// </summary>
    interface IHelperMethodGenerator
    {
        void Generate(StringBuilder builder, string matcherClassName, PropertyWrapper namedProp);
    }

    [HelperName("Modifiers")]
    sealed class ModifierHelper : IHelperMethodGenerator
    {
        static readonly IReadOnlyList<string> Modifiers =
        [
            "Static",
            "Public",
            "Private",
            "Internal",
            "Protected",
            "Abstract",
            "ReadOnly",
            "Ref",
            "File",
            "Override",
            "Virtual"
        ];

        public void Generate(
            StringBuilder builderMethods,
            string matcherClassName,
            PropertyWrapper namedProp)
        {

            foreach (var modifier in Modifiers)
            {
                builderMethods.AppendLine($@"
                    public {matcherClassName} Is{modifier}() => With{namedProp.Name}({namedProp.Name}.Merge(Is.{modifier}));
");
            }
        }
    }
}