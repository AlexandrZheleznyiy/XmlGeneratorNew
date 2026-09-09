using System.Collections.ObjectModel;
using XmlGeneratorNew.Constants;
using XmlGeneratorNew.Models;

namespace XmlGeneratorNew.Services
{
    /// <summary>
    /// Сервис для обработки операций Drag & Drop
    /// </summary>
    public class DragDropService
    {
        private readonly TreeManipulationService _treeService;

        public DragDropService(TreeManipulationService treeService)
        {
            _treeService = treeService;
        }

        /// <summary>
        /// Обрабатывает операцию перетаскивания элемента
        /// </summary>
        public void HandleDrop(
            object draggedItem,
            object? targetItem,
            ObservableCollection<object> rootItems,
            ObservableCollection<string> footerItems)
        {
            if (draggedItem == null || draggedItem == targetItem)
                return;

            bool isDraggedFooter = draggedItem is string draggedStr && _treeService.IsFooterString(draggedStr);
            bool isTargetFooter = targetItem is string targetStr && _treeService.IsFooterString(targetStr);

            // Обработка перемещения элементов футера
            if (isDraggedFooter)
            {
                HandleFooterDrop((string)draggedItem, targetItem, footerItems);
                return;
            }

            // Обработка перемещения секций между собой
            if (draggedItem is SectionItem && targetItem is SectionItem)
            {
                SwapSections(draggedItem, targetItem, rootItems);
                return;
            }

            // Запрет вложения элемента в самого себя или своих потомков
            if (targetItem != null && _treeService.IsDescendantOf(targetItem, draggedItem, rootItems))
            {
                System.Diagnostics.Debug.WriteLine(
                    "[DragDrop] Drop rejected: Cannot drop item into itself or its descendant.");
                return;
            }

            // Удаляем draggedItem из текущего места
            _treeService.RemoveItem(draggedItem, rootItems, footerItems);

            // Добавляем в новое место
            if (targetItem == null)
            {
                rootItems.Add(draggedItem);
                return;
            }

            InsertIntoTarget(draggedItem, targetItem, rootItems);
        }

        /// <summary>
        /// Обрабатывает перетаскивание элемента футера
        /// </summary>
        private void HandleFooterDrop(string draggedItem, object? targetItem, ObservableCollection<string> footerItems)
        {
            footerItems.Remove(draggedItem);

            if (targetItem == null || !(targetItem is string targetStr) || !_treeService.IsFooterString(targetStr))
            {
                footerItems.Add(draggedItem);
                return;
            }

            int targetIndex = footerItems.IndexOf(targetStr);
            if (targetIndex >= 0)
                footerItems.Insert(targetIndex + 1, draggedItem);
            else
                footerItems.Add(draggedItem);
        }

        /// <summary>
        /// Меняет местами две секции в корне
        /// </summary>
        private void SwapSections(object draggedItem, object targetItem, ObservableCollection<object> rootItems)
        {
            int draggedIndex = rootItems.IndexOf(draggedItem);
            int targetIndex = rootItems.IndexOf(targetItem);

            if (draggedIndex >= 0 && targetIndex >= 0)
            {
                rootItems.Move(draggedIndex, targetIndex);
            }
        }

        /// <summary>
        /// Вставляет элемент в целевой контейнер
        /// </summary>
        private void InsertIntoTarget(object draggedItem, object targetItem, ObservableCollection<object> rootItems)
        {
            switch (targetItem)
            {
                case SectionItem section:
                    AddChildToContainer(section, draggedItem);
                    break;

                case GroupItem group:
                    AddChildToContainer(group, draggedItem);
                    break;

                case OneOfItem oneOf when draggedItem is PropertyItem p:
                    oneOf.AddProperty(p);
                    break;

                case TableItem table when draggedItem is RowItem r:
                    table.AddRow(r);
                    break;

                case RowItem row when draggedItem is CellItem c:
                    row.AddCell(c);
                    break;

                case CellItem cell:
                    AddChildToContainer(cell, draggedItem);
                    break;

                default:
                    var parent = _treeService.FindParent(targetItem, rootItems);
                    if (parent != null)
                        InsertAfter(parent, targetItem, draggedItem);
                    else
                        rootItems.Add(draggedItem);
                    break;
            }
        }

        /// <summary>
        /// Добавляет элемент в контейнер в зависимости от типов
        /// </summary>
        private void AddChildToContainer(object container, object newItem)
        {
            switch (container)
            {
                case SectionItem s:
                    if (newItem is GroupItem sg) s.AddGroup(sg);
                    else if (newItem is PropertyItem sp) s.AddProperty(sp);
                    else if (newItem is OneOfItem so) s.AddOneOf(so);
                    else if (newItem is TableItem st) s.AddTable(st);
                    break;

                case GroupItem gr:
                    if (newItem is GroupItem gg) gr.AddGroup(gg);
                    else if (newItem is PropertyItem gp) gr.AddProperty(gp);
                    else if (newItem is OneOfItem go) gr.AddOneOf(go);
                    else if (newItem is TableItem gt) gr.AddTable(gt);
                    break;

                case OneOfItem oneOf:
                    if (newItem is PropertyItem op) oneOf.AddProperty(op);
                    break;

                case TableItem table:
                    if (newItem is RowItem tr) table.AddRow(tr);
                    break;

                case RowItem row:
                    if (newItem is CellItem rc) row.AddCell(rc);
                    break;

                case CellItem cell:
                    if (newItem is PropertyItem cp) cell.AddProperty(cp);
                    else if (newItem is GroupItem cg) cell.AddGroup(cg);
                    break;
            }
        }

        private ObservableCollection<object>? GetChildren(object container)
        {
            return container switch
            {
                SectionItem s => s.Children,
                GroupItem g => g.Children,
                OneOfItem o => o.Children,
                TableItem t => t.Children,
                RowItem r => r.Children,
                CellItem c => c.Children,
                _ => null
            };
        }

        /// <summary>
        /// Вставляет элемент после указанного reference элемента
        /// </summary>
        private void InsertAfter(object? parent, object reference, object newItem)
        {
            if (parent == null)
                return;

            AddChildToContainer(parent, newItem);
            var children = GetChildren(parent);
            if (children != null)
            {
                ReorderInChildren(children, reference, newItem);
            }
        }

        /// <summary>
        /// Переупорядочивает элементы в коллекции Children
        /// </summary>
        private void ReorderInChildren(ObservableCollection<object> children, object reference, object newItem)
        {
            int currentIndex = children.IndexOf(newItem);
            int targetIndex = children.IndexOf(reference) + 1;

            if (currentIndex >= 0 && targetIndex >= 0 && currentIndex != targetIndex)
            {
                children.RemoveAt(currentIndex);
                if (currentIndex < targetIndex)
                    targetIndex--;
                children.Insert(targetIndex, newItem);
            }
        }
    }
}
