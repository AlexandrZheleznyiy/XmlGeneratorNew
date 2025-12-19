using XmlGeneratorNew.Models;

namespace XmlGeneratorNew.Services
{
    /// <summary>
    /// Сервис для дублирования элементов дерева
    /// </summary>
    public class ItemDuplicationService
    {
        /// <summary>
        /// Дублирует секцию со всеми вложенными элементами
        /// </summary>
        public SectionItem DuplicateSection(SectionItem section)
        {
            var newSection = new SectionItem
            {
                Code = section.Code,
                Name = section.Name,
                Title = section.Title,
                Semd = section.Semd,
                Uid = section.Uid,
                IsExpanded = section.IsExpanded
            };

            foreach (var group in section.Groups)
                newSection.AddGroup(DuplicateGroup(group));

            foreach (var prop in section.Properties)
                newSection.AddProperty(DuplicateProperty(prop));

            return newSection;
        }

        /// <summary>
        /// Дублирует группу со всеми вложенными элементами
        /// </summary>
        public GroupItem DuplicateGroup(GroupItem group)
        {
            var newGroup = new GroupItem
            {
                Name = group.Name,
                Caption = group.Caption,
                OdCaption = group.OdCaption,
                Layout = group.Layout,
                Separator = group.Separator,
                Suffix = group.Suffix,
                OdSeparator = group.OdSeparator,
                OdSuffix = group.OdSuffix,
                OdGroupMode = group.OdGroupMode,
                OdGroupModeIsParagraph = group.OdGroupModeIsParagraph,
                ECaptionStyle = group.ECaptionStyle,
                ECaptionStyleIsGroupHeader = group.ECaptionStyleIsGroupHeader,
                OdGroupStyle = group.OdGroupStyle,
                OdGroupStyleIsNewParagraphBoldHeader = group.OdGroupStyleIsNewParagraphBoldHeader,
                Semd = group.Semd,
                Uid = group.Uid,
                IsExpanded = group.IsExpanded
            };

            foreach (var subgroup in group.Groups)
                newGroup.AddGroup(DuplicateGroup(subgroup));

            foreach (var prop in group.Properties)
                newGroup.AddProperty(DuplicateProperty(prop));

            return newGroup;
        }

        /// <summary>
        /// Дублирует свойство
        /// </summary>
        public PropertyItem DuplicateProperty(PropertyItem prop)
        {
            return new PropertyItem
            {
                Name = prop.Name,
                Caption = prop.Caption,
                OdCaption = prop.OdCaption,
                Separator = prop.Separator,
                Suffix = prop.Suffix,
                OdSeparator = prop.OdSeparator,
                OdSuffix = prop.OdSuffix,
                MinWidth = prop.MinWidth,
                MinLines = prop.MinLines,
                AutoSuggestName = prop.AutoSuggestName,
                Value = prop.Value,
                Type = prop.Type,
                Semd = prop.Semd,
                Uid = prop.Uid
            };
        }
    }
}
