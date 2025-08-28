using System.Windows;
using System.Windows.Input;
using XmlGeneratorNew.ViewModels;

namespace XmlGeneratorNew.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.SelectedItem = e.NewValue;
            }
        }
        private void TreeView_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Delete && DataContext is MainViewModel vm)
            {
                if (vm.DeleteCommand.CanExecute(null))
                {
                    vm.DeleteCommand.Execute(null);
                }
                e.Handled = true; // Подавляем дальнейшее распространение события
            }
        }

        private void Expander_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
        }
    }
}