using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace NugetAuditor.Lib
{
    public static class Extensions
    {
        public static string GetAttributeValue(this XElement element, XName name, string defaultValue)
        {
            var attribute = element.Attribute(name);

            return (attribute == null) ? defaultValue : attribute.Value;
        }      
    }
}
