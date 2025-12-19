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

            foreach (var section in rootItems.OfType<SectionItem>())
            {
                if (section.Groups.Contains(item) || section.Properties.Contains(item))
                    return section;

                var parent = FindParentInGroup(item, section.Groups);
                if (parent != null) return parent;
            }

            foreach (var group in rootItems.OfType<GroupItem>())
            {
                if (group.Groups.Contains(item) || group.Properties.Contains(item))
                    return group;

                var parent = FindParentInGroup(item, group.Groups);
                if (parent != null) return parent;
            }

            return null;
        }

        /// <summary>
        /// Рекурсивный поиск родителя в группах
        /// </summary>
        private GroupItem? FindParentInGroup(object item, ObservableCollection<GroupItem> groups)
        {
            foreach (var group in groups)
            {
                if (group.Groups.Contains(item) || group.Properties.Contains(item))
                    return group;

                var found = FindParentInGroup(item, group.Groups);
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

            // Обработка PropertyItem
            if (item is PropertyItem prop)
            {
                var parent = FindParent(item, rootItems);
                if (parent is SectionItem section)
                {
                    return section.RemoveProperty(prop);
                }
                else if (parent is GroupItem group)
                {
                    return group.RemoveChild(prop);
                }
            }

            // Обработка GroupItem
            if (item is GroupItem groupItem)
            {
                var parent = FindParent(item, rootItems);
                if (parent is SectionItem section)
                {
                    return section.RemoveGroup(groupItem);
                }
                else if (parent is GroupItem group)
                {
                    return group.RemoveChild(groupItem);
                }
            }

            // Обработка SectionItem
            if (item is SectionItem sectionItem)
            {
                return rootItems.Remove(sectionItem);
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
