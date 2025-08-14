using System.Collections.ObjectModel;
using System.Windows;
using XmlGeneratorNew.Models;

namespace XmlGeneratorNew.Views
{
    public partial class NamespaceSettingsWindow : Window
    {
        public ObservableCollection<NamespaceItem> Namespaces { get; }
        public string TemplateName { get; set; } = "";

        public NamespaceSettingsWindow(ObservableCollection<NamespaceItem> namespaces, string templateName)
        {
            InitializeComponent();
            Namespaces = namespaces;
            NamespacesList.ItemsSource = Namespaces;
            TemplateName = templateName;
            DataContext = this;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
