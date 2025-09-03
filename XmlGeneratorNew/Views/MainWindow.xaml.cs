using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using XmlGeneratorNew.ViewModels;
using System;

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
                e.Handled = true;  //  Подавляем   дальнейшее   распространение   события
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

                //  Инициируем  DragDrop  операцию   и   передаём   перетаскиваемый   объект
                DataObject dragData = new DataObject("myFormat", draggedItem);
                DragDrop.DoDragDrop(treeViewItem, dragData, DragDropEffects.Move);
            }
        }

        private void TreeView_DragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("myFormat"))
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }
            object? targetData = GetDataContextTreeViewItem(e);
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        private void TreeView_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("myFormat")) return;
            object? draggedData = e.Data.GetData("myFormat");
            object? targetData = GetDataContextTreeViewItem(e);
            if (draggedData == null) return;
            if (DataContext is MainViewModel vm)
            {
                vm.HandleDrop(draggedData, targetData);
            }
            e.Handled = true;
        }

        private object? GetDataContextTreeViewItem(DragEventArgs e)
        {
            Point position = e.GetPosition(treeView);
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

        // --- Обработчики для FooterListBox ---
        private void FooterListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        private void FooterListBox_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = e.GetPosition(null);
            Vector diff = _dragStartPoint - mousePos;
            if (e.LeftButton == MouseButtonState.Pressed &&
                (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                 Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                ListBox listBox = sender as ListBox;
                ListBoxItem listBoxItem = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource);

                if (listBoxItem == null)
                    return;

                object draggedItem = listBoxItem.DataContext;
                if (draggedItem == null)
                    return;

                DataObject dragData = new DataObject("myFormat", draggedItem);
                DragDrop.DoDragDrop(listBoxItem, dragData, DragDropEffects.Move);
            }
        }

        private void FooterListBox_DragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("myFormat"))
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        private void FooterListBox_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("myFormat")) return;
            object draggedItem = e.Data.GetData("myFormat");
            if (draggedItem == null) return;

            // Определяем, куда был сброшен элемент
            // Пытаемся получить ListBoxItem под курсором
            var pos = e.GetPosition(FooterListBox);
            var result = VisualTreeHelper.HitTest(FooterListBox, pos);
            object? targetItem = null;
            if (result != null)
            {
                DependencyObject current = result.VisualHit;
                while (current != null && !(current is ListBoxItem))
                {
                    current = VisualTreeHelper.GetParent(current);
                }
                if (current is ListBoxItem lbi)
                {
                    targetItem = lbi.DataContext;
                }
            }

            if (DataContext is MainViewModel vm)
            {
                // Передаем null как targetItem, если дроп на пустое место футера
                // HandleDrop должен обработать это правильно
                vm.HandleDrop(draggedItem, targetItem);
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