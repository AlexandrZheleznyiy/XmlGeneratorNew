using System.Collections.Generic;
using XmlGeneratorNew.DTOs;
using XmlGeneratorNew.ViewModels;

namespace XmlGeneratorNew.Models
{
    public class DraftData
    {
        public string TemplateName { get; set; } = "";
        public List<DraftItemDto> RootItems { get; set; } = new();      // <- вместо List<object>
        public List<string> FooterItems { get; set; } = new();
        public TypeSettingsViewModel TypeSettings { get; set; } = new();
        public BlocksSettingsViewModel BlockSettings { get; set; } = new();
    }
}