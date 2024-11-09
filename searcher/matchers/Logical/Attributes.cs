using System;

namespace SyntaxSearch.Matchers
{
    /// <summary>
    /// Excludes the matcher class from being parseable
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class ExcludeAttribute : Attribute;

    /// <summary>
    /// Use the marked constructor when creating the Is/Does static methods
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor)]
    internal sealed class UseConstructorAttribute : Attribute;
}
