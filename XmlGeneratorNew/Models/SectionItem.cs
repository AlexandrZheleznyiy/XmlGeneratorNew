using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace XmlGeneratorNew.Models
{
    public partial class SectionItem : ObservableObject
    {
        [ObservableProperty]
        private string code = "";

        [ObservableProperty]
        private string name = "";

        [ObservableProperty]
        private string title = "";

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

        private void OnDeserialized(StreamingContext context)
        {
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

        public void AddProperty(PropertyItem property)
        {
            Properties.Add(property);
            Children.Add(property);
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

        public bool RemoveGroup(GroupItem group)
        {
            bool removed = Groups.Remove(group);
            if (removed) Children.Remove(group);
            return removed;
        }

        public bool RemoveProperty(PropertyItem property)
        {
            bool removed = Properties.Remove(property);
            if (removed) Children.Remove(property);
            return removed;
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