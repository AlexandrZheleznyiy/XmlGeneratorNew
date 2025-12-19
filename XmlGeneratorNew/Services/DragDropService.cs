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

            // Запрет вложения группы в саму себя
            if (draggedItem is GroupItem draggedGroup && targetItem is GroupItem targetGroup)
            {
                if (_treeService.IsDescendantOf(targetGroup, draggedGroup, rootItems))
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[DragDrop] Drop rejected: Cannot drop group '{draggedGroup.Name}' into itself or its descendant.");
                    return;
                }
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
                    if (draggedItem is GroupItem g)
                        section.AddGroup(g);
                    else if (draggedItem is PropertyItem p)
                        section.AddProperty(p);
                    break;

                case GroupItem group:
                    if (draggedItem is GroupItem gr)
                        group.AddGroup(gr);
                    else if (draggedItem is PropertyItem p)
                        group.AddProperty(p);
                    break;

                case PropertyItem _:
                    var parent = _treeService.FindParent(targetItem, rootItems);
                    InsertAfter(parent, targetItem, draggedItem);
                    break;

                default:
                    rootItems.Add(draggedItem);
                    break;
            }
        }

        /// <summary>
        /// Вставляет элемент после указанного reference элемента
        /// </summary>
        private void InsertAfter(object? parent, object reference, object newItem)
        {
            if (parent == null)
                return;

            if (parent is SectionItem section)
            {
                InsertIntoSection(section, reference, newItem);
            }
            else if (parent is GroupItem group)
            {
                InsertIntoGroup(group, reference, newItem);
            }
        }

        /// <summary>
        /// Вставляет элемент в секцию после reference
        /// </summary>
        private void InsertIntoSection(SectionItem section, object reference, object newItem)
        {
            if (newItem is GroupItem newGroup)
            {
                section.AddGroup(newGroup);
                ReorderInChildren(section.Children, reference, newGroup);
            }
            else if (newItem is PropertyItem newProp)
            {
                section.AddProperty(newProp);
                ReorderInChildren(section.Children, reference, newProp);
            }
        }

        /// <summary>
        /// Вставляет элемент в группу после reference
        /// </summary>
        private void InsertIntoGroup(GroupItem group, object reference, object newItem)
        {
            if (newItem is GroupItem newSubGroup)
            {
                group.AddGroup(newSubGroup);
                ReorderInChildren(group.Children, reference, newSubGroup);
            }
            else if (newItem is PropertyItem newProp)
            {
                group.AddProperty(newProp);
                ReorderInChildren(group.Children, reference, newProp);
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
