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
        // В MainWindow.xaml.cs - этот код уже должен быть таким
        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DataContext is MainViewModel vm)
            {
                // Всегда устанавливаем новое значение, даже если оно null
                // Это гарантирует, что VM получит уведомление о любом изменении
                vm.SelectedItem = e.NewValue;
            }
        }

        private void Expander_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
        }
    }
}