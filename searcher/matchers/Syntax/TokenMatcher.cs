using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SyntaxSearch.Matchers
{
    public sealed class TokenMatcher : ITokenMatcher
    {
        private readonly Optional<SyntaxKind> _kind;
        private readonly string _text;
        private readonly string _valueText;
        private readonly object _value;

        private TokenMatcher(Optional<SyntaxKind> kind, string text, string valueText, object value)
        {
            _kind = kind;
            _text = text;
            _valueText = valueText;
            _value = value;
        }

        public static TokenMatcher Default { get; } = new(default, default, default, default);

        public TokenMatcher WithKind(SyntaxKind kind) => new(kind, _text, _valueText, _value);
        public TokenMatcher WithText(string text) => new(_kind, text, _valueText, _value);
        public TokenMatcher WithValueText(string valueText) => new(_kind, _text, valueText, _value);
        public TokenMatcher WithValue(object value) => new(_kind, _text, _valueText, value);

        public bool IsMatch(SyntaxToken token, CaptureStore store)
        {
            if (_kind.HasValue && !token.IsKind(_kind.Value))
            {
                return false;
            }
            if (_text is not null && token.Text != _text)
            {
                return false;
            }
            if (_valueText is not null && token.ValueText != _valueText)
            {
                return false;
            }
            if (_value is not null && !token.Value.Equals(_value))
            {
                return false;
            }
            return true;
        }

        public static implicit operator TokenMatcher(SyntaxKind kind) => Default.WithKind(kind);

        public static implicit operator TokenMatcher(string name) => Default.WithText(name);
    }
}
