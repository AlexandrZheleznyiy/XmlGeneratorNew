using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace XmlGeneratorNew.Models
{
    public partial class GroupItem : ObservableObject
    {
        [ObservableProperty]
        private string name = "";

        [ObservableProperty]
        private string caption = "";

        [ObservableProperty]
        private string odCaption = "";

        [ObservableProperty]
        private string layout = "DockPanel";

        [ObservableProperty]
        private string separator = "";

        [ObservableProperty]
        private string suffix = "";

        [ObservableProperty]
        private string odSeparator = "";

        [ObservableProperty]
        private string odSuffix = "";

        [ObservableProperty]
        private string odGroupMode = "";

        [ObservableProperty]
        private bool odGroupModeIsParagraph = false;

        [ObservableProperty]
        private string eCaptionStyle = "";

        [ObservableProperty]
        private bool eCaptionStyleIsGroupHeader = false;

        [ObservableProperty]
        private string odGroupStyle = "";

        [ObservableProperty]
        private bool odGroupStyleIsNewParagraphBoldHeader = false;

        [ObservableProperty]
        private string visibleWhen = "";

        [ObservableProperty]
        private bool odIgnore = false;

        [ObservableProperty]
        private bool isExpanded;

        [ObservableProperty]
        private bool isSelected;

        [ObservableProperty]
        private string semd = "";

        [ObservableProperty]
        private string _uid = "";


        public ObservableCollection<GroupItem> Groups { get; } = new();
        public ObservableCollection<PropertyItem> Properties { get; } = new();
        public ObservableCollection<OneOfItem> OneOfs { get; } = new();
        public ObservableCollection<TableItem> Tables { get; } = new();
        public ObservableCollection<object> Children { get; } = new();

        public GroupItem()
        {
            // Подписка не обязательна, так как добавление идёт через специальные методы
        }
        private void OnDeserialized(StreamingContext context)
        {
            // гарантируем, что Children синхронизирован
            Children.Clear();
            foreach (var g in Groups)
                Children.Add(g);
            foreach (var p in Properties)
                Children.Add(p);
            foreach (var o in OneOfs)
                Children.Add(o);
            foreach (var t in Tables)
                Children.Add(t);
        }

        public void AddGroup(GroupItem group)
        {
            Groups.Add(group);
            Children.Add(group);
            IsExpanded = true;
        }

        public void AddProperty(PropertyItem prop)
        {
            Properties.Add(prop);
            Children.Add(prop);
            IsExpanded = true;
        }

        public void AddOneOf(OneOfItem oneOf)
        {
            OneOfs.Add(oneOf);
            Children.Add(oneOf);
            IsExpanded = true;
        }

        public void AddTable(TableItem table)
        {
            Tables.Add(table);
            Children.Add(table);
            IsExpanded = true;
        }

        public bool RemoveChild(object child)
        {
            bool removed = false;
            if (child is GroupItem g) removed = Groups.Remove(g);
            else if (child is PropertyItem p) removed = Properties.Remove(p);
            else if (child is OneOfItem o) removed = OneOfs.Remove(o);
            else if (child is TableItem t) removed = Tables.Remove(t);
            if (removed) Children.Remove(child);
            return removed;
        }
    }
}