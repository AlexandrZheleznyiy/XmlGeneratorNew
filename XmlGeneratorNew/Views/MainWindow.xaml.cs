using System.Windows;
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

        // Опционально: если хотите обработать SelectedItemChanged из TreeView с MVVM, можно связать здесь,
        // или использовать готовые расширения для двунаправленной привязки SelectedItem.
        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.SelectedItem = e.NewValue;
            }
        }
    }
}
