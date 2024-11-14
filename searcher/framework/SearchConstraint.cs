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

    public sealed class MaybeAttribute(string name = null) : MethodAttribute(name);

    public sealed class NotAttribute(string name = null) : MethodAttribute(name);

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

        public static ISyntaxTokenListMatcher Public => HasSyntaxTokenListMatcher.Default.Has(SyntaxKind.PublicKeyword);
        public static ISyntaxTokenListMatcher Private => HasSyntaxTokenListMatcher.Default.Has(SyntaxKind.PrivateKeyword);
        public static ISyntaxTokenListMatcher Protected => HasSyntaxTokenListMatcher.Default.Has(SyntaxKind.ProtectedKeyword);
        public static ISyntaxTokenListMatcher Static => HasSyntaxTokenListMatcher.Default.Has(SyntaxKind.StaticKeyword);
        public static ISyntaxTokenListMatcher ReadOnly => HasSyntaxTokenListMatcher.Default.Has(SyntaxKind.ReadOnlyKeyword);
        public static ISyntaxTokenListMatcher Abstract => HasSyntaxTokenListMatcher.Default.Has(SyntaxKind.AbstractKeyword);
        public static ISyntaxTokenListMatcher Internal => HasSyntaxTokenListMatcher.Default.Has(SyntaxKind.InternalKeyword);
        public static ISyntaxTokenListMatcher Record => HasSyntaxTokenListMatcher.Default.Has(SyntaxKind.RecordKeyword);
    }

    public static partial class Not
    {
        public static ISyntaxTokenListMatcher Public => DoesNotHaveSyntaxTokenListMatcher.Default.NoneAre(SyntaxKind.PublicKeyword);
        public static ISyntaxTokenListMatcher Private => DoesNotHaveSyntaxTokenListMatcher.Default.NoneAre(SyntaxKind.PrivateKeyword);
        public static ISyntaxTokenListMatcher Protected => DoesNotHaveSyntaxTokenListMatcher.Default.NoneAre(SyntaxKind.ProtectedKeyword);
        public static ISyntaxTokenListMatcher Static => DoesNotHaveSyntaxTokenListMatcher.Default.NoneAre(SyntaxKind.StaticKeyword);
        public static ISyntaxTokenListMatcher ReadOnly => DoesNotHaveSyntaxTokenListMatcher.Default.NoneAre(SyntaxKind.ReadOnlyKeyword);
        public static ISyntaxTokenListMatcher Abstract => DoesNotHaveSyntaxTokenListMatcher.Default.NoneAre(SyntaxKind.AbstractKeyword);
        public static ISyntaxTokenListMatcher Internal => DoesNotHaveSyntaxTokenListMatcher.Default.NoneAre(SyntaxKind.InternalKeyword);
        public static ISyntaxTokenListMatcher Record => DoesNotHaveSyntaxTokenListMatcher.Default.NoneAre(SyntaxKind.RecordKeyword);
    }

    public static partial class Has
    {
        /// <summary>
        /// Creates a SyntaxTokenList matcher that requires all given matchers to mtach
        /// </summary>
        /// <param name="matchers"></param>
        /// <returns></returns>
        public static AndSyntaxTokenListMatcher Modifiers(params ISyntaxTokenListMatcher[] matchers) 
            => new AndSyntaxTokenListMatcher().With(matchers);
    }

    public static partial class Does
    {
        public static MatchCapture Match(string name) => new MatchCapture(name);
    }
}
