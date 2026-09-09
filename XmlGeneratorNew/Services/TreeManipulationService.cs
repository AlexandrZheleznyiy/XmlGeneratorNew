using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using XmlGeneratorNew.Constants;
using XmlGeneratorNew.Models;

namespace XmlGeneratorNew.Services
{
    /// <summary>
    /// Сервис для манипуляций с деревом элементов
    /// </summary>
    public class TreeManipulationService
    {
        /// <summary>
        /// Находит родительский элемент для указанного элемента
        /// </summary>
        public object? FindParent(object item, ObservableCollection<object> rootItems)
        {
            if (rootItems.Contains(item))
                return null;

            foreach (var root in rootItems)
            {
                var parent = FindParentRecursive(item, root);
                if (parent != null) return parent;
            }

            return null;
        }

        /// <summary>
        /// Рекурсивный поиск родителя в контейнере
        /// </summary>
        private object? FindParentRecursive(object item, object container)
        {
            IEnumerable<object>? children = container switch
            {
                SectionItem s => s.Children,
                GroupItem g => g.Children,
                OneOfItem o => o.Children,
                TableItem t => t.Children,
                RowItem r => r.Children,
                CellItem c => c.Children,
                _ => null
            };

            if (children == null) return null;

            foreach (var child in children)
            {
                if (child == item)
                    return container;

                var found = FindParentRecursive(item, child);
                if (found != null) return found;
            }

            return null;
        }

        /// <summary>
        /// Удаляет элемент из дерева
        /// </summary>
        public bool RemoveItem(object item, ObservableCollection<object> rootItems, ObservableCollection<string> footerItems)
        {
            // Проверка для основных элементов в корне
            if (rootItems.Contains(item))
            {
                rootItems.Remove(item);
                return true;
            }

            // Проверка для строк футера
            if (item is string itemAsString && footerItems.Contains(itemAsString))
            {
                footerItems.Remove(itemAsString);
                return true;
            }

            // Поиск родителя и удаление из него
            var parent = FindParent(item, rootItems);
            if (parent != null)
            {
                return parent switch
                {
                    SectionItem section => section.RemoveChild(item),
                    GroupItem group => group.RemoveChild(item),
                    OneOfItem oneOf => oneOf.RemoveChild(item),
                    TableItem table => table.RemoveChild(item),
                    RowItem row => row.RemoveChild(item),
                    CellItem cell => cell.RemoveChild(item),
                    _ => false
                };
            }

            return false;
        }

        /// <summary>
        /// Проверяет, является ли элемент потомком другого элемента
        /// </summary>
        public bool IsDescendantOf(object potentialDescendant, object potentialAncestor, ObservableCollection<object> rootItems)
        {
            if (potentialDescendant == null || potentialAncestor == null)
                return false;

            object? currentParent = FindParent(potentialDescendant, rootItems);

            while (currentParent != null)
            {
                if (currentParent == potentialAncestor)
                    return true;

                currentParent = FindParent(currentParent, rootItems);
            }

            return false;
        }

        /// <summary>
        /// Проверяет, является ли строка элементом футера
        /// </summary>
        public bool IsFooterString(string str)
        {
            return str == FooterItemNames.Diagnosis ||
                   str == FooterItemNames.IcfInitial ||
                   str == FooterItemNames.IcfRecurrent ||
                   str == FooterItemNames.IcfFinal ||
                   str == FooterItemNames.Assignments ||
                   str == FooterItemNames.TreatmentActions ||
                   str == FooterItemNames.Attachments ||
                   str == FooterItemNames.Conclusion;
        }
    }
}
