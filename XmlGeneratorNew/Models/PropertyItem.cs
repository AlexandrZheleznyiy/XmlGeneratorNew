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
        private bool isSelected;

        [ObservableProperty]
        private PropertyType type = PropertyType.String;

        [ObservableProperty]
        private string semd = "";

        [ObservableProperty]
        private string _uid = "";

        // Метод, вызываемый CommunityToolkit.Mvvm после изменения свойства Type
        partial void OnTypeChanged(PropertyType value)
        {
            // Если тип изменен на Bool и текущее значение Value не "True" или "False",
            // устанавливаем значение по умолчанию "False".
            if (value == PropertyType.Bool)
            {
                if (this.value?.ToLowerInvariant() != "true" && this.value?.ToLowerInvariant() != "false")
                {
                    // Используем поле напрямую, чтобы избежать повторного срабатывания OnPropertyChanged/OnTypeChanged
                    // если бы мы использовали свойство Value.
                    this.value = "False";
                    // Поскольку мы изменили поле напрямую, нужно вручную уведомить об изменении свойства.
                    OnPropertyChanged(nameof(Value));
                }
            }
            // Для других типов мы не изменяем Value, оставляя его как есть.
            // Если нужно сбросить Value при изменении типа, добавьте логику здесь.
        }
    }
}