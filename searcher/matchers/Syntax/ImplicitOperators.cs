using Microsoft.CodeAnalysis;
using SyntaxSearch.Framework;
using SyntaxSearch.Matchers.Explicit;
using System.Collections.Generic;
using System.Linq;

namespace SyntaxSearch.Matchers.Explicit
{
    public partial class ArgumentMatcher
    {
        public static implicit operator ArgumentMatcher(ExpressionSyntaxMatcher expression) => Is.Argument.WithExpression(expression);
    }

    public partial class ArgumentListMatcher
    {
        public ArgumentListMatcher WithArguments(params ExpressionSyntaxMatcher[] arguments)
        {
            return WithArguments([.. arguments.Select(arg => Is.Argument.WithExpression(arg))]);
        }

        public ArgumentListMatcher WithArgument(ExpressionSyntaxMatcher argument) => Is.ArgumentList.WithArguments(argument);

        public static implicit operator ArgumentListMatcher(ExpressionSyntaxMatcher[] arguments)
        {
            return Is.ArgumentList.WithArguments(arguments);
        }

        public static implicit operator ArgumentListMatcher(ExpressionSyntaxMatcher argument)
        {
            return Is.ArgumentList.WithArguments([argument]);
        }
    }

    public partial class BracketedArgumentListMatcher
    {
        public BracketedArgumentListMatcher WithArguments(params ExpressionSyntaxMatcher[] arguments)
        {
            return WithArguments([.. arguments.Select(arg => Is.Argument.WithExpression(arg))]);
        }

        public BracketedArgumentListMatcher WithArgument(ExpressionSyntaxMatcher argument) => Is.BracketedArgumentList.WithArguments(argument);

        public static implicit operator BracketedArgumentListMatcher(ExpressionSyntaxMatcher[] arguments)
        {
            return Is.BracketedArgumentList.WithArguments(arguments);
        }

        public static implicit operator BracketedArgumentListMatcher(ExpressionSyntaxMatcher argument)
        {
            return Is.BracketedArgumentList.WithArguments([argument]);
        }
    }

    public partial class ElementAccessExpressionMatcher
    {
        public ElementAccessExpressionMatcher WithArgument(ExpressionSyntaxMatcher expression)
        {
            return WithArguments([expression]);
        }

        public ElementAccessExpressionMatcher WithArguments(params ExpressionSyntaxMatcher[] arguments)
        {
            return WithArgumentList(arguments);
        }
    }

    public partial class SimpleNameSyntaxMatcher
    {
        public static implicit operator SimpleNameSyntaxMatcher(string name) => new IdentifierNameMatcher().WithText(name);
    }

    public partial class ExpressionStatementMatcher
    {
        public static implicit operator ExpressionStatementMatcher(ExpressionSyntaxMatcher expressionSyntax) => Is.ExpressionStatement.WithExpression(expressionSyntax);
    }

    public partial class ObjectCreationExpressionMatcher
    {
        public ObjectCreationExpressionMatcher WithArguments(params ExpressionSyntaxMatcher[] arguments)
        {
            return WithArgumentList(arguments);
        }

        public ObjectCreationExpressionMatcher WithArgument(ExpressionSyntaxMatcher argument)
        {
            return WithArguments([argument]);
        }
    }

    public partial class InvocationExpressionMatcher
    {
        public InvocationExpressionMatcher WithArguments(params ExpressionSyntaxMatcher[] arguments)
        {
            return WithArgumentList(arguments);
        }

        public InvocationExpressionMatcher WithArgument(ExpressionSyntaxMatcher argument)
        {
            return WithArguments([argument]);
        }
    }
}
