using System.Windows;
using XmlGeneratorNew.ViewModels;

namespace XmlGeneratorNew.Views
{
    public partial class BlocksSettingsWindow : Window
    {
        public BlocksSettingsViewModel ViewModel { get; private set; }

        public BlocksSettingsWindow(BlocksSettingsViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = ViewModel;
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