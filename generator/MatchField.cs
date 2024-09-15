namespace SyntaxSearcher.Generators
{
    internal record struct MatchField(string TypeName, string TokenName, string TokenValue, string FieldName)
    {
        public static implicit operator (string, string, string, string)(MatchField value)
        {
            return (value.TypeName, value.TokenName, value.TokenValue, value.FieldName);
        }

        public static implicit operator MatchField((string, string, string, string) value)
        {
            return new MatchField(value.Item1, value.Item2, value.Item3, value.Item4);
        }
    }
}