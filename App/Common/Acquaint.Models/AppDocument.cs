using System;
namespace Acquaint.Models
{
    public class AppDocument : ListItem
    {
        public AppDocument()
        {
        }

        public string Title { get; set; }
        public string Description { get; set; }
    }
}
