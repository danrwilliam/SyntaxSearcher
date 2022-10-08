namespace SyntaxSearch.Builder
{
    public class TreeBuilderOptions
    {
        /// <summary>
        /// Include modifier information for matcher
        /// </summary>
        public bool Modifiers { get; set; } = false;

        /// <summary>
        /// Include keyword text for matcher
        /// </summary>
        public bool Keywords { get; set; } = true;

        /// <summary>
        /// Include identifier text for matcher
        /// </summary>
        public bool Identifiers { get; set; } = true;

        /// <summary>
        /// Visit syntax tokens in addition to nodes
        /// </summary>
        public bool Tokens { get; set; } = false;

        /// <summary>
        /// Automatically introduce MatchCapture when encountering an identical node
        /// </summary>
        public bool AutomaticCapture { get; set; } = false;

        /// <summary>
        /// When used with <see cref="AutomaticCapture"/>, then use Anything
        /// matcher for captured items.
        /// </summary>
        public bool UseAnythingForAutomaticCapture { get; set; } = false;

        /// <summary>
        /// Xml will create a named node for each child object
        /// <para>Currently unused</para>
        /// </summary>
        public bool NamedChildren { get; set; } = false;
    }
}
