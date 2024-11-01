using System;

namespace SyntaxSearch.Matchers
{
    /// <summary>
    /// Excludes the matcher class from being parseable
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class ExcludeAttribute : Attribute;
}
