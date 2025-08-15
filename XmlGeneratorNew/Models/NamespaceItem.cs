namespace XmlGeneratorNew.Models
{
    public class NamespaceItem
    {
        public string Prefix { get; set; } = "";
        public string Uri { get; set; } = "";
        public bool IsSelected { get; set; }
        public string DisplayName => $" {Prefix} - {Uri} ";
    }
}