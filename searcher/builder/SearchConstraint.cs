using Microsoft.CodeAnalysis;
using SyntaxSearch.Matchers;
using System.Collections.Generic;

namespace SyntaxSearch.Framework
{
    public partial class Constraint
    {
        private INodeMatcher _matcher;

        public Constraint()
        {
        }


        public IEnumerable<SearchResult> Search(SyntaxNode node)
        {
            var s = new Searcher(_matcher);
            foreach (var result in s.Search(node))
            {
                yield return result;
            }
        }
    }

    //public static class Does
    //{
    //    public static MatchCapture Match(string name) => new MatchCapture(name);

    //    public static ContainsMatcher Contain => new ContainsMatcher();
    //}

    public static partial class Is
    {
        //public Constraint Not
        //{
        //    get
        //    {
        //        var copy = new Constraint
        //        {
        //            _matcher = new NotMatcher()
        //        };
        //        return copy;
        //    }
        //}
    }
}
