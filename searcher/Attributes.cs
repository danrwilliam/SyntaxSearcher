using System;
using System.Collections.Generic;
using System.Text;

namespace SyntaxSearch.Matchers
{
    public sealed class TagNameAttribute : Attribute
    {
        public string Name { get; }
    }

    public sealed class OnlyOneChildAttribute : Attribute
    {

    }
}
