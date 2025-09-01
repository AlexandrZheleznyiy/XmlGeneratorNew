using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using XmlGeneratorNew.ViewModels;

namespace XmlGeneratorNew.Views
{
    public partial class MainWindow : Window
    {
        private Point _dragStartPoint;
        private TreeView treeView;
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
            treeView = MainTreeView;
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
        private void TreeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        private void TreeView_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = e.GetPosition(null);
            Vector diff = _dragStartPoint - mousePos;

            if (e.LeftButton == MouseButtonState.Pressed &&
                (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                TreeView treeView = sender as TreeView;
                TreeViewItem treeViewItem =
                    FindAncestor<TreeViewItem>((DependencyObject)e.OriginalSource);

                if (treeViewItem == null)
                    return;

                object draggedItem = treeViewItem.DataContext;
                if (draggedItem == null)
                    return;

                // Инициируем DragDrop операцию и передаём перетаскиваемый объект
                DataObject dragData = new DataObject("myFormat", draggedItem);
                DragDrop.DoDragDrop(treeViewItem, dragData, DragDropEffects.Move);
            }
        }

        private void TreeView_DragOver(object sender, DragEventArgs e)
        {
            object? targetData = GetDataContextTreeViewItem(e);

            if (!e.Data.GetDataPresent("myFormat") || targetData == null)
            {
                e.Effects = DragDropEffects.None;
            }
            else
            {
                e.Effects = DragDropEffects.Move;
            }
            e.Handled = true;
        }

        private void TreeView_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("myFormat"))
            {
                object? draggedData = e.Data.GetData("myFormat");
                object? targetData = GetDataContextTreeViewItem(e);

                if (draggedData != null)
                {
                    if (DataContext is MainViewModel vm)
                    {
                        vm.HandleDrop(draggedData, targetData);
                    }
                }
            }
            e.Handled = true;
        }
        private object? GetDataContextTreeViewItem(DragEventArgs e)
        {
            Point position = e.GetPosition(treeView); // treeView - ваш TreeView (присвойте при InitializeComponent)
            HitTestResult hit = VisualTreeHelper.HitTest(treeView, position);

            if (hit == null) return null;

            DependencyObject? current = hit.VisualHit;

            while (current != null && !(current is TreeViewItem))
            {
                current = VisualTreeHelper.GetParent(current);
            }

            if (current is TreeViewItem tvi)
            {
                return tvi.DataContext;
            }
            return null;
        }
        private void MainTreeView_DragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("myFormat"))
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            object? targetItem = GetDropTargetDataContext(e);

            // Можно добавить логику для определения, можно ли в targetItem переместить draggedItem
            e.Effects = DragDropEffects.Move;

            e.Handled = true;
        }
        private void MainTreeView_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("myFormat")) return;

            object draggedItem = e.Data.GetData("myFormat");
            if (draggedItem == null) return;

            object? targetItem = GetDropTargetDataContext(e);

            if (DataContext is MainViewModel vm)
            {
                if (targetItem == null)
                {
                    // Дроп в корень — перемещаем в RootItems
                    vm.HandleDrop(draggedItem, null);
                }
                else
                {
                    // Дроп на обычный элемент
                    vm.HandleDrop(draggedItem, targetItem);
                }
            }

            e.Handled = true;
        }
        private object? GetDropTargetDataContext(DragEventArgs e)
        {
            var pos = e.GetPosition(MainTreeView);  // заменить TreeViewControl на имя вашего TreeView
            var result = VisualTreeHelper.HitTest(MainTreeView, pos);

            if (result == null)
                return null;

            DependencyObject current = result.VisualHit;
            while (current != null && !(current is TreeViewItem))
            {
                current = VisualTreeHelper.GetParent(current);
            }

            if (current is TreeViewItem tvi)
            {
                return tvi.DataContext;
            }

            // Возвращаем null, если дроп на пустом месте
            return null;
        }
        private void TreeViewControl_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof(object))) // или ваш формат
                return;

            var draggedItem = e.Data.GetData(typeof(object));
            if (draggedItem == null) return;

            var targetItem = GetDropTargetDataContext(e);

            if (DataContext is MainViewModel vm)
            {
                if (targetItem == null)
                {
                    // Дроп на пустое место — добавляем в корень
                    vm.MoveItemToRoot(draggedItem);
                }
                else
                {
                    // Дроп на элемент — нормальная логика с вложением
                    vm.HandleDrop(draggedItem, targetItem);
                }
            }

            e.Handled = true;
        }

        // Вспомогательный метод для поиска TreeViewItem из визуального дерева
        private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T correctlyTyped)
                {
                    return correctlyTyped;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }
    }
}