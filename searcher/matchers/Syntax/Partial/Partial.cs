using Microsoft.CodeAnalysis;
using SyntaxSearch.Framework;
using System.Collections.Generic;
using System.Linq;

namespace SyntaxSearch.Matchers.Explicit
{
    public partial class ArgumentMatcher
    {
        public static implicit operator ArgumentMatcher(ExpressionSyntaxMatcher expression) => new ArgumentMatcher().WithExpression(expression);
    }

    public partial class IdentifierNameMatcher
    {
        public static implicit operator IdentifierNameMatcher(string name) => new IdentifierNameMatcher().WithText(name);
    }

    public partial class ArgumentListMatcher
    {
        public ArgumentListMatcher WithArguments(params ExpressionSyntaxMatcher[] arguments)
        {
            return WithArguments([.. arguments.Select(arg => Is.Argument.WithExpression(arg))]);
        }
    }

    public partial class BracketedArgumentListMatcher
    {
        public BracketedArgumentListMatcher WithArguments(params ExpressionSyntaxMatcher[] arguments)
        {
            return WithArguments([.. arguments.Select(arg => Is.Argument.WithExpression(arg))]);
        }
    }

    public partial class SimpleNameSyntaxMatcher
    {
        public static implicit operator SimpleNameSyntaxMatcher(string name) => new IdentifierNameMatcher().WithText(name);
    }
}
