using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using XmlGeneratorNew.ViewModels;

namespace XmlGeneratorNew.Views
{
    public partial class MainWindow : Window
    {
        private Point dragStartPoint;
        private TreeView treeView;
        private MainViewModel _vm;

        public MainWindow()
        {
            InitializeComponent();

            // ЕДИНСТВЕННЫЙ экземпляр VM
            _vm = new MainViewModel();
            DataContext = _vm;

            treeView = MainTreeView;

            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // При старте один раз предлагаем восстановить черновик
            await _vm.TryOfferRestoreDraftAsync();
        }
        private async void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            await _vm.SaveDraftAsync();
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
            dragStartPoint = e.GetPosition(null);
        }

        private void TreeView_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var position = e.GetPosition(null);
                if (Math.Abs(position.X - dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(position.Y - dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    StartDrag(e);
                }
            }
        }
        private void StartDrag(MouseEventArgs e)
        {
            if (treeView.SelectedItem == null)
                return;

            var draggedItem = treeView.SelectedItem;
            if (draggedItem == null)
                return;

            var dragData = new DataObject("treeViewItem", draggedItem);
            DragDrop.DoDragDrop(treeView, dragData, DragDropEffects.Move);
        }

        private void TreeView_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }
        private void TreeView_DropOnRoot(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("treeViewItem"))
                return;

            var draggedItem = e.Data.GetData("treeViewItem");
            if (draggedItem == null)
                return;

            if (DataContext is MainViewModel vm)
            {
                vm.MoveItemToRoot(draggedItem);
            }
        }

        private void TreeView_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("treeViewItem"))
                return;

            var draggedItem = e.Data.GetData("treeViewItem");
            if (draggedItem == null)
                return;

            var target = GetNearestContainer(e.OriginalSource as UIElement);
            object? targetItem = target?.DataContext;

            if (DataContext is MainViewModel vm)
            {
                vm.HandleDrop(draggedItem, targetItem);
            }
        }
        private TreeViewItem? GetNearestContainer(UIElement? element)
        {
            while (element != null && element != treeView)
            {
                if (element is TreeViewItem item)
                    return item;

                element = VisualTreeHelper.GetParent(element) as UIElement;
            }
            return null;
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
            dragStartPoint = e.GetPosition(null);
        }

        private void FooterListBox_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = e.GetPosition(null);
            Vector diff = dragStartPoint - mousePos;

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