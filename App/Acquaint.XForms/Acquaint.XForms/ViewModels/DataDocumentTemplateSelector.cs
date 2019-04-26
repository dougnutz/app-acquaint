using System;
using Acquaint.Models;
using Acquaint.XForms;
using Xamarin.Forms;

namespace Acquaint.XForms
{
    public class DataDocumentTemplateSelector : DataTemplateSelector
    {
        public DataTemplate HeaderTemplate { get; set; }
        public DataTemplate UserTemplate { get; set; }
        public DataTemplate AppTemplate { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            if (item is Acquaintance)
            {
                return UserTemplate;
            }
            else if (item is ListSeparator)
            {
                return HeaderTemplate;
            }
            else
            {
                return AppTemplate;
            }
        }
    }
}
