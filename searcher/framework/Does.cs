using SyntaxSearch.Matchers;

namespace SyntaxSearch.Framework
{
    /// <summary>
    /// Creates a helper property in <see cref="Does"/> for this type
    /// </summary>
    /// <param name="name"></param>
    public sealed class DoesAttribute(string name = null) : MethodAttribute(name);

    public static partial class Does
    {
        public static MatchCapture Match(string name) => new MatchCapture(name);
    }
}
