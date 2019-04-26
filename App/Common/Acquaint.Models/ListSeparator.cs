using System;
using Acquaint.Models;

namespace Acquaint.XForms
{
    public class ListSeparator : ListItem
    {
        public ListSeparator(string title)
        {
            this.Title = title;
        }

        public string Title { get; set; }
    }
}
