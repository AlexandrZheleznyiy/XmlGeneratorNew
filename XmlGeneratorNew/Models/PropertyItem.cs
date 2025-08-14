using CommunityToolkit.Mvvm.ComponentModel;

namespace XmlGeneratorNew.Models
{
    public enum PropertyType
    {
        String,
        Bool,
        Const
    }

    public partial class PropertyItem : ObservableObject
    {
        [ObservableProperty]
        private string name = "";

        [ObservableProperty]
        private string caption = "";

        [ObservableProperty]
        private string odCaption = "";

        [ObservableProperty]
        private string separator = "";

        [ObservableProperty]
        private string suffix = "";

        [ObservableProperty]
        private string odSeparator = "";

        [ObservableProperty]
        private string odSuffix = "";

        [ObservableProperty]
        private string minWidth = "";

        [ObservableProperty]
        private string minLines = "";

        [ObservableProperty]
        private string autoSuggestName = "";

        [ObservableProperty]
        private string value = "";

        [ObservableProperty]
        private PropertyType type = PropertyType.String;
    }
}
