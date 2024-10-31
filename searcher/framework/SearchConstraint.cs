using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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

    /// <summary>
    /// Creates a helper property in <see cref="Is"/> for this type
    /// </summary>
    /// <param name="name"></param>
    public sealed class IsAttribute(string name = null) : MethodAttribute(name);
    /// <summary>
    /// Creates a helper property in <see cref="Does"/> for this type
    /// </summary>
    /// <param name="name"></param>
    public sealed class DoesAttribute(string name = null) : MethodAttribute(name);

    public sealed class HasAttribute(string name = null) : MethodAttribute(name);

    public static partial class Is
    {
        //public static IsOneOfMatcher OneOf(INodeMatcher matcher, params INodeMatcher[] remaining)
        //{
        //    var m = new IsOneOfMatcher();
        //    m.AddChild(matcher);
        //    foreach (var r in remaining)
        //    {
        //        m.AddChild(r);
        //    }
        //    return m;
        //}

        public static NotMatcher Not(INodeMatcher matcher) => new NotMatcher(matcher);

        public static TokenMatcher Identifier => TokenMatcher.Default.WithKind(SyntaxKind.IdentifierToken);
    }

    public static partial class Does
    {
        public static MatchCapture Match(string name) => new MatchCapture(name);
    }
}
