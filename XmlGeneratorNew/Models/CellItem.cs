using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace XmlGeneratorNew.Models
{
    /// <summary>
    /// Модель для элемента <cell> внутри <row>
    /// </summary>
    public partial class CellItem : ObservableObject
    {
        [ObservableProperty]
        private string col = "";

        [ObservableProperty]
        private string colSpan = "";

        [ObservableProperty]
        private string rowSpan = "";

        [ObservableProperty]
        private string text = "";

        [ObservableProperty]
        private string type = "";

        [ObservableProperty]
        private string eLayout = "";

        [ObservableProperty]
        private string eStyle = "";

        [ObservableProperty]
        private string odCaption = "";

        [ObservableProperty]
        private string odCellWidth = "";

        [ObservableProperty]
        private string odJustification = "";

        [ObservableProperty]
        private bool isExpanded;

        [ObservableProperty]
        private bool isSelected;

        [ObservableProperty]
        private string _uid = "";

        public ObservableCollection<PropertyItem> Properties { get; } = new();
        public ObservableCollection<GroupItem> Groups { get; } = new();
        public ObservableCollection<object> Children { get; } = new();

        private void OnDeserialized(StreamingContext context)
        {
            Children.Clear();
            foreach (var g in Groups)
                Children.Add(g);
            foreach (var p in Properties)
                Children.Add(p);
        }

        public void AddProperty(PropertyItem prop)
        {
            Properties.Add(prop);
            Children.Add(prop);
            IsExpanded = true;
        }

        public void AddGroup(GroupItem group)
        {
            Groups.Add(group);
            Children.Add(group);
            IsExpanded = true;
        }

        public bool RemoveChild(object child)
        {
            bool removed = false;
            if (child is PropertyItem p) removed = Properties.Remove(p);
            else if (child is GroupItem g) removed = Groups.Remove(g);
            if (removed) Children.Remove(child);
            return removed;
        }
    }
}
