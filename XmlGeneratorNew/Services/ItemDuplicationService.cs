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

            foreach (var oneOf in section.OneOfs)
                newSection.AddOneOf(DuplicateOneOf(oneOf));

            foreach (var table in section.Tables)
                newSection.AddTable(DuplicateTable(table));

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
                VisibleWhen = group.VisibleWhen,
                OdIgnore = group.OdIgnore,
                Semd = group.Semd,
                Uid = group.Uid,
                IsExpanded = group.IsExpanded
            };

            foreach (var subgroup in group.Groups)
                newGroup.AddGroup(DuplicateGroup(subgroup));

            foreach (var prop in group.Properties)
                newGroup.AddProperty(DuplicateProperty(prop));

            foreach (var oneOf in group.OneOfs)
                newGroup.AddOneOf(DuplicateOneOf(oneOf));

            foreach (var table in group.Tables)
                newGroup.AddTable(DuplicateTable(table));

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
                VisibleWhen = prop.VisibleWhen,
                Semd = prop.Semd,
                Uid = prop.Uid
            };
        }

        /// <summary>
        /// Дублирует элемент oneOf (радиогруппу)
        /// </summary>
        public OneOfItem DuplicateOneOf(OneOfItem oneOf)
        {
            var newOneOf = new OneOfItem
            {
                Name = oneOf.Name,
                Caption = oneOf.Caption,
                OdCaption = oneOf.OdCaption,
                Orientation = oneOf.Orientation,
                Separator = oneOf.Separator,
                Suffix = oneOf.Suffix,
                Prefix = oneOf.Prefix,
                OdSeparator = oneOf.OdSeparator,
                OdSuffix = oneOf.OdSuffix,
                OdPrefix = oneOf.OdPrefix,
                OdGroupMode = oneOf.OdGroupMode,
                OdIgnore = oneOf.OdIgnore,
                OdSentenceStart = oneOf.OdSentenceStart,
                OdUnderlined = oneOf.OdUnderlined,
                VisibleWhen = oneOf.VisibleWhen,
                Value = oneOf.Value,
                Type = oneOf.Type,
                Semd = oneOf.Semd,
                Uid = oneOf.Uid,
                IsExpanded = oneOf.IsExpanded
            };

            foreach (var prop in oneOf.Properties)
                newOneOf.AddProperty(DuplicateProperty(prop));

            return newOneOf;
        }

        /// <summary>
        /// Дублирует таблицу
        /// </summary>
        public TableItem DuplicateTable(TableItem table)
        {
            var newTable = new TableItem
            {
                EStyle = table.EStyle,
                ETableStretch = table.ETableStretch,
                OdTableStretch = table.OdTableStretch,
                Uid = table.Uid,
                IsExpanded = table.IsExpanded
            };

            foreach (var row in table.Rows)
                newTable.AddRow(DuplicateRow(row));

            return newTable;
        }

        /// <summary>
        /// Дублирует строку таблицы
        /// </summary>
        public RowItem DuplicateRow(RowItem row)
        {
            var newRow = new RowItem
            {
                RowIndex = row.RowIndex,
                OdJustification = row.OdJustification,
                Uid = row.Uid,
                IsExpanded = row.IsExpanded
            };

            foreach (var cell in row.Cells)
                newRow.AddCell(DuplicateCell(cell));

            return newRow;
        }

        /// <summary>
        /// Дублирует ячейку таблицы
        /// </summary>
        public CellItem DuplicateCell(CellItem cell)
        {
            var newCell = new CellItem
            {
                Col = cell.Col,
                ColSpan = cell.ColSpan,
                RowSpan = cell.RowSpan,
                Text = cell.Text,
                Type = cell.Type,
                ELayout = cell.ELayout,
                EStyle = cell.EStyle,
                OdCaption = cell.OdCaption,
                OdCellWidth = cell.OdCellWidth,
                OdJustification = cell.OdJustification,
                Uid = cell.Uid,
                IsExpanded = cell.IsExpanded
            };

            foreach (var prop in cell.Properties)
                newCell.AddProperty(DuplicateProperty(prop));

            foreach (var group in cell.Groups)
                newCell.AddGroup(DuplicateGroup(group));

            return newCell;
        }
    }
}
