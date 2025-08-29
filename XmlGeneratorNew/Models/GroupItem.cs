using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

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
        private bool isExpanded;

        [ObservableProperty]
        private bool isSelected;

        [ObservableProperty]
        private string semd = "";


        public ObservableCollection<GroupItem> Groups { get; } = new();
        public ObservableCollection<PropertyItem> Properties { get; } = new();
        public ObservableCollection<object> Children { get; } = new();

        public GroupItem()
        {
            // Подписка не обязательна, так как добавление идёт через специальные методы
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

        public bool RemoveChild(object child)
        {
            bool removed = false;
            if (child is GroupItem g) removed = Groups.Remove(g);
            else if (child is PropertyItem p) removed = Properties.Remove(p);
            if (removed) Children.Remove(child);
            return removed;
        }
    }
}