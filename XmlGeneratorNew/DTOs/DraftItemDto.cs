using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XmlGeneratorNew.DTOs
{
    public class DraftItemDto
    {
        public string ItemType { get; set; } = "";  // "section", "group", "property", "oneOf", "table", "footer"
        public string Json { get; set; } = "";
    }
}
