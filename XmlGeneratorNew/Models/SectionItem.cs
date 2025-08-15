using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace XmlGeneratorNew.Models
{
    public partial class SectionItem : ObservableObject
    {
        [ObservableProperty] private string code = "";
        [ObservableProperty] private string name = "";
        [ObservableProperty] private string title = "";
        [ObservableProperty] private bool isExpanded;
        [ObservableProperty] private bool isSelected;

        public ObservableCollection<GroupItem> Groups { get; } = new();
        public ObservableCollection<PropertyItem> Properties { get; } = new();
        public ObservableCollection<object> Children { get; } = new();

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
    }
}