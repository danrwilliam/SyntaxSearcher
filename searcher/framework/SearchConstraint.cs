using Microsoft.CodeAnalysis;
using SyntaxSearch.Matchers;
using System;
using System.Collections.Generic;

namespace SyntaxSearch.Framework
{
    [AttributeUsage(AttributeTargets.Class)]
    public abstract class MethodAttribute(string name = null) : Attribute
    {
        public string Name { get; } = name;
    }

    public sealed class IsAttribute(string name = null) : MethodAttribute(name);
    public sealed class DoesAttribute(string name = null) : MethodAttribute(name);

    public static partial class Is
    {
    }

    public static partial class Does
    {
        public static MatchCapture Match(string name) => new MatchCapture(name);
    }
}
