using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace SyntaxSearch.Framework
{
    [AttributeUsage(AttributeTargets.Class)]
    public abstract class MethodAttribute(string name = null) : Attribute
    {
        public string Name { get; } = name;
    }

    public sealed class MaybeAttribute(string name = null) : MethodAttribute(name);
}
