using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

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

        public ObservableCollection<GroupItem> Groups { get; } = new();
        public ObservableCollection<object> Children { get; } = new();

        public SectionItem()
        {
            // Нет автоматики, добавление через метод
        }

        public void AddGroup(GroupItem group)
        {
            Groups.Add(group);
            Children.Add(group);
            IsExpanded = true;
        }

        public bool RemoveGroup(GroupItem group)
        {
            bool removed = Groups.Remove(group);
            if (removed) Children.Remove(group);
            return removed;
        }
    }
}
