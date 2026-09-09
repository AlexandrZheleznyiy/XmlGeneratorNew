using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace XmlGeneratorNew.Models
{
    /// <summary>
    /// Модель для элемента <oneOf> — радиогруппа
    /// </summary>
    public partial class OneOfItem : ObservableObject
    {
        [ObservableProperty]
        private string name = "";

        [ObservableProperty]
        private string caption = "";

        [ObservableProperty]
        private string odCaption = "";

        [ObservableProperty]
        private string orientation = "";

        [ObservableProperty]
        private string separator = "";

        [ObservableProperty]
        private string suffix = "";

        [ObservableProperty]
        private string prefix = "";

        [ObservableProperty]
        private string odSeparator = "";

        [ObservableProperty]
        private string odSuffix = "";

        [ObservableProperty]
        private string odPrefix = "";

        [ObservableProperty]
        private string odGroupMode = "";

        [ObservableProperty]
        private bool odIgnore = false;

        [ObservableProperty]
        private string visibleWhen = "";

        [ObservableProperty]
        private string value = "";

        [ObservableProperty]
        private string type = "string";

        [ObservableProperty]
        private bool odSentenceStart = false;

        [ObservableProperty]
        private bool odUnderlined = false;

        [ObservableProperty]
        private bool isExpanded;

        [ObservableProperty]
        private bool isSelected;

        [ObservableProperty]
        private string semd = "";

        [ObservableProperty]
        private string _uid = "";

        public ObservableCollection<PropertyItem> Properties { get; } = new();
        public ObservableCollection<object> Children { get; } = new();

        private void OnDeserialized(StreamingContext context)
        {
            Children.Clear();
            foreach (var p in Properties)
                Children.Add(p);
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
            if (child is PropertyItem p) removed = Properties.Remove(p);
            if (removed) Children.Remove(child);
            return removed;
        }
    }
}
