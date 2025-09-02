using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Xml;
using System.Xml.Linq;
using XmlGeneratorNew.Models;
using XmlGeneratorNew.Views;
using System.Collections.Generic;

namespace XmlGeneratorNew.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private string templateName = "";

        [ObservableProperty]
        private object? selectedItem;
        public ObservableCollection<object> RootItems { get; } = new();
        private TypeSettingsViewModel _typeSettings = new();
        private BlocksSettingsViewModel _blocksSettings = new();

        private int groupIndex = 1;
        private int propertyIndex = 1;
        private string currentSavePath = "metadata.xml";

        public IRelayCommand AddSectionCommand { get; }
        public IRelayCommand AddGroupCommand { get; }
        public IRelayCommand AddPropertyCommand { get; }
        public IRelayCommand DeleteCommand { get; }
        public IRelayCommand ResetCommand { get; }
        public IRelayCommand LoadCommand { get; }
        public IRelayCommand SaveCommand { get; }
        public IRelayCommand OpenNamespaceSettingsCommand { get; }
        public IRelayCommand AddSectionToRootCommand { get; }
        public IRelayCommand AddGroupToRootCommand { get; }
        public IRelayCommand AddPropertyToRootCommand { get; }
        public IRelayCommand<object> DuplicateCommand { get; }
        public IRelayCommand OpenTypeSettingsCommand { get; }
        public IRelayCommand OpenBlocksSettingsCommand { get; }
        public MainViewModel()
        {
            AddSectionToRootCommand = new RelayCommand(AddSectionToRoot);
            AddGroupToRootCommand = new RelayCommand(AddGroupToRoot);
            AddPropertyToRootCommand = new RelayCommand(AddPropertyToRoot);
            // ---  Команды   для   редактора  ( добавляют   в   выбранный   элемент ) ---
            AddGroupCommand = new RelayCommand(AddGroup);
            AddPropertyCommand = new RelayCommand(AddProperty);
            AddSectionCommand = new RelayCommand(AddSection);
            //  Остальные   команды
            DeleteCommand = new RelayCommand(DeleteSelected, CanDelete);
            ResetCommand = new RelayCommand(ResetAll);
            LoadCommand = new RelayCommand(LoadXml);
            SaveCommand = new RelayCommand(SaveXml);
            OpenNamespaceSettingsCommand = new RelayCommand(OpenNamespaceSettings); DuplicateCommand = new RelayCommand<object>(
    execute: obj => DuplicateItem(),
    canExecute: obj => SelectedItem != null
);
            OpenTypeSettingsCommand = new RelayCommand(OpenTypeSettings);
            OpenBlocksSettingsCommand = new RelayCommand(OpenBlocksSettings);
        }
        public ObservableCollection<NamespaceItem> Namespaces { get; } = new()
        {
            new NamespaceItem { Prefix = "e", Uri = "http://www.sanatorium-is.ru/editor", IsSelected = true },
            new NamespaceItem { Prefix = "xaml", Uri = "http://schemas.microsoft.com/winfx/2006/xaml/presentation", IsSelected = true },
            new NamespaceItem { Prefix = "x", Uri = "http://schemas.microsoft.com/winfx/2006/xaml", IsSelected = true },
            new NamespaceItem { Prefix = "od", Uri = "http://www.sanatorium-is.ru/officeDocument", IsSelected = true },
            new NamespaceItem { Prefix = "precompile", Uri = "http://www.sanatorium-is.ru/premetadata", IsSelected = true },
            new NamespaceItem { Prefix = "p", Uri = "clr-namespace:Markup.Programming;assembly=Markup.Programming", IsSelected = false }
        };

        private void OpenBlocksSettings()
        {
            var settingsWindow = new Views.BlocksSettingsWindow(_blocksSettings);
            settingsWindow.Owner = Application.Current.MainWindow;
            if (settingsWindow.ShowDialog() == true)
            {
                _blocksSettings = settingsWindow.ViewModel;
                UpdateBlocksInTree(); // Обновляем отображение в дереве
            }
        }
        private void UpdateBlocksInTree()
        {
            // Собираем все блоки, которые нужно удалить
            var itemsToRemove = RootItems
                .Where(item => item is string block && block.StartsWith(""))
                .ToList();

            // Удаляем их по одному
            foreach (var item in itemsToRemove)
            {
                RootItems.Remove(item);
            }

            // Добавляем новые, если выбраны
            if (_blocksSettings.IsDiagnosis)
                RootItems.Add("Диагнозы");
            if (_blocksSettings.IsAssignments)
                RootItems.Add("Назначения");
            if (_blocksSettings.IsTreatmentActions)
                RootItems.Add("Лечебные действия");
            if (_blocksSettings.IsAttachments)
                RootItems.Add("Вложения");
        }


        private void OpenTypeSettings()
        {
            var settingsWindow = new Views.TypeSettingsWindow(_typeSettings);
            settingsWindow.Owner = Application.Current.MainWindow;
            if (settingsWindow.ShowDialog() == true)
            {
                _typeSettings = settingsWindow.ViewModel; // Сохраняем настройки
            }
        }

        partial void OnSelectedItemChanged(object? oldValue, object? newValue)
        {
            System.Diagnostics.Debug.WriteLine($"[VM] SelectedItem changed from {oldValue?.GetType().Name ?? "null"} to {newValue?.GetType().Name ?? "null"}");

            // Уведомляем команды о изменении
            DeleteCommand.NotifyCanExecuteChanged();
            AddGroupCommand.NotifyCanExecuteChanged();
            AddPropertyCommand.NotifyCanExecuteChanged();
            AddSectionCommand.NotifyCanExecuteChanged();

            // Если старое значение было выделено, сбрасываем его выделение
            if (oldValue is ObservableObject oldObj)
            {
                if (oldObj is SectionItem oldSection) oldSection.IsSelected = false;
                if (oldObj is GroupItem oldGroup) oldGroup.IsSelected = false;
                if (oldObj is PropertyItem oldProp) oldProp.IsSelected = false;
            }

            // Если новое значение не null, устанавливаем его выделение
            if (newValue is ObservableObject newObj)
            {
                if (newObj is SectionItem newSection) newSection.IsSelected = true;
                if (newObj is GroupItem newGroup) newGroup.IsSelected = true;
                if (newObj is PropertyItem newProp) newProp.IsSelected = true;
            }
        }

        private void AddSection()
        {
            var newSection = new SectionItem
            {
                Name = $"Секция _{RootItems.OfType<SectionItem>().Count() + 1}",
                IsExpanded = true,
                IsSelected = true
            };
            RootItems.Add(newSection);
            SelectedItem = newSection;
        }

        private void AddGroup()
        {
            if (SelectedItem is SectionItem selectedSection)
            {
                var newGroup = new GroupItem { Name = $"Группа _{groupIndex++}", IsExpanded = true, IsSelected = true };
                selectedSection.AddGroup(newGroup);
                SelectedItem = newGroup;
            }
            else if (SelectedItem is GroupItem selectedGroup)
            {
                var newSubGroup = new GroupItem { Name = $"Подгруппа _{groupIndex++}", IsExpanded = true, IsSelected = true };
                selectedGroup.AddGroup(newSubGroup);
                SelectedItem = newSubGroup;
            }
            else
            {
                var newGroup = new GroupItem { Name = $"Группа _{groupIndex++}", IsExpanded = true, IsSelected = true };
                RootItems.Add(newGroup);
                SelectedItem = newGroup;
            }
        }

        private void AddProperty()
        {
            if (SelectedItem is GroupItem group)
            {
                var prop = new PropertyItem { Name = $"Свойство _{propertyIndex++}" };
                group.AddProperty(prop);
                SelectedItem = prop;
            }
            else if (SelectedItem is SectionItem section)
            {
                var prop = new PropertyItem { Name = $"Свойство _{propertyIndex++}" };
                section.AddProperty(prop);
                SelectedItem = prop;
            }
            else
            {
                var prop = new PropertyItem { Name = $"Свойство _{propertyIndex++}" };
                RootItems.Add(prop);
                SelectedItem = prop;
            }
        }

        // === Методы для добавления строго в корень ===
        private void AddSectionToRoot()
        {
            var newSection = new SectionItem
            {
                Name = $"Секция _{RootItems.OfType<SectionItem>().Count() + 1}",
                IsExpanded = true,
                IsSelected = true
            };
            RootItems.Add(newSection);
            SelectedItem = newSection;
        }

        private void AddGroupToRoot()
        {
            var newGroup = new GroupItem
            {
                Name = $"Группа _{groupIndex++}",
                IsExpanded = true,
                IsSelected = true
            };
            RootItems.Add(newGroup);
            SelectedItem = newGroup;
        }

        private void AddPropertyToRoot()
        {
            var newProperty = new PropertyItem
            {
                Name = $"Свойство _{propertyIndex++}"
            };
            RootItems.Add(newProperty);
            SelectedItem = newProperty;
        }

        private bool CanDelete() => SelectedItem != null;

        private void DeleteSelected()
        {
            if (SelectedItem == null) return;

            object? itemToRemove = SelectedItem; // Сохраняем ссылку на удаляемый элемент
            bool removed = false;
            object? parentContainer = null; // Для отслеживания родителя вложенных элементов

            // Проверяем, находится ли элемент непосредственно в RootItems
            if (RootItems.Contains(SelectedItem))
            {
                removed = RootItems.Remove(SelectedItem);

                // ВАЖНО: После удаления элемента верхнего уровня сбрасываем IsSelected у всех оставшихся элементов верхнего уровня
                if (removed)
                {
                    foreach (var item in RootItems)
                    {
                        if (item is SectionItem si) si.IsSelected = false;
                        if (item is GroupItem gi) gi.IsSelected = false;
                        if (item is PropertyItem pi) pi.IsSelected = false;
                    }
                }
            }
            else
            {
                // Элемент вложен. Ищем его родителя и удаляем оттуда.
                if (SelectedItem is PropertyItem prop)
                {
                    // Ищем свойство в группах (включая вложенные) и запоминаем родителя
                    parentContainer = FindParentOfPropertyAndRemove(prop, out removed);
                }
                else if (SelectedItem is GroupItem group)
                {
                    // Ищем группу и запоминаем родителя
                    parentContainer = FindParentOfGroupAndRemove(group, out removed);
                }
                else if (SelectedItem is SectionItem section)
                {
                    // Этот случай уже должен был быть покрыт RootItems.Contains(section)
                    removed = RootItems.Remove(section);
                    // ВАЖНО: Сбрасываем IsSelected у оставшихся элементов верхнего уровня
                    if (removed)
                    {
                        foreach (var item in RootItems)
                        {
                            if (item is SectionItem si) si.IsSelected = false;
                            if (item is GroupItem gi) gi.IsSelected = false;
                            if (item is PropertyItem pi) pi.IsSelected = false;
                        }
                    }
                }
            }

            if (removed)
            {
                // ВАЖНО: Сбрасываем IsSelected у удаленного элемента
                if (itemToRemove is ObservableObject observableItem)
                {
                    if (observableItem is SectionItem si) si.IsSelected = false;
                    if (observableItem is GroupItem gi) gi.IsSelected = false;
                    // PropertyItem не имеет IsSelected в текущей модели
                }

                // ВАЖНО: Если удаляли из контейнера, сбрасываем IsSelected у родителя и "тормошим" его состояние
                if (parentContainer is ObservableObject parentObservable)
                {
                    if (parentObservable is SectionItem parentSection)
                    {
                        parentSection.IsSelected = false;
                        // Принудительно "тормошим" состояние для обновления UI
                        parentSection.IsExpanded = !parentSection.IsExpanded;
                        parentSection.IsExpanded = !parentSection.IsExpanded;
                    }
                    if (parentObservable is GroupItem parentGroup)
                    {
                        parentGroup.IsSelected = false;
                        // Принудительно "тормошим" состояние для обновления UI
                        parentGroup.IsExpanded = !parentGroup.IsExpanded;
                        parentGroup.IsExpanded = !parentGroup.IsExpanded;
                    }
                }

                // Сброс SelectedItem во ViewModel
                SelectedItem = null;

                // Уведомляем команды
                DeleteCommand.NotifyCanExecuteChanged();
                AddGroupCommand.NotifyCanExecuteChanged();
                AddPropertyCommand.NotifyCanExecuteChanged();
                AddSectionCommand.NotifyCanExecuteChanged();
            }
        }
        private object? FindParentOfPropertyAndRemove(PropertyItem target, out bool removed)
        {
            removed = false;

            // Поиск в группах внутри секций
            foreach (var section in RootItems.OfType<SectionItem>())
            {
                foreach (var group in section.Groups)
                {
                    if (group.Properties.Contains(target))
                    {
                        removed = group.RemoveChild(target);
                        return group; // Возвращаем родителя (GroupItem)
                    }
                    var foundParent = FindPropertyInGroupAndRemove(target, group);
                    if (foundParent != null)
                    {
                        return foundParent; // Родитель уже определен внутри рекурсии
                    }
                }
            }

            // Поиск в корневых группах
            foreach (var group in RootItems.OfType<GroupItem>())
            {
                if (group.Properties.Contains(target))
                {
                    removed = group.RemoveChild(target);
                    return group; // Возвращаем родителя (GroupItem)
                }
                var foundParent = FindPropertyInGroupAndRemove(target, group);
                if (foundParent != null)
                {
                    return foundParent; // Родитель уже определен внутри рекурсии
                }
            }

            // Поиск в секциях (свойства напрямую в секциях)
            foreach (var section in RootItems.OfType<SectionItem>())
            {
                if (section.Properties.Contains(target))
                {
                    removed = section.RemoveProperty(target);
                    return section; // Возвращаем родителя (SectionItem)
                }
            }

            // Поиск в корне
            if (RootItems.Contains(target))
            {
                removed = RootItems.Remove(target);
                return null; // Родитель корневого элемента - это сам RootItems
            }

            return null;
        }

        private bool RemovePropertyFromSections(IEnumerable<SectionItem> sections, PropertyItem target)
        {
            foreach (var section in sections)
            {
                if (section.Properties.Contains(target))
                {
                    return section.RemoveProperty(target);
                }
            }
            return false;
        }

        // Вспомогательный метод для поиска и удаления свойства с определением родителя
        private object? FindAndRemoveProperty(PropertyItem target, out bool removed)
        {
            removed = false;

            // Поиск в группах внутри секций
            foreach (var section in RootItems.OfType<SectionItem>())
            {
                foreach (var group in section.Groups)
                {
                    if (group.Properties.Contains(target))
                    {
                        removed = group.RemoveChild(target);
                        return group; // Возвращаем родителя (GroupItem)
                    }
                    var foundParent = FindPropertyInGroupAndRemove(target, group);
                    if (foundParent != null)
                    {
                        return foundParent; // Родитель уже определен внутри рекурсии
                    }
                }
            }

            // Поиск в корневых группах
            foreach (var group in RootItems.OfType<GroupItem>())
            {
                if (group.Properties.Contains(target))
                {
                    removed = group.RemoveChild(target);
                    return group; // Возвращаем родителя (GroupItem)
                }
                var foundParent = FindPropertyInGroupAndRemove(target, group);
                if (foundParent != null)
                {
                    return foundParent; // Родитель уже определен внутри рекурсии
                }
            }

            // Поиск в секциях (свойства напрямую в секциях)
            foreach (var section in RootItems.OfType<SectionItem>())
            {
                if (section.Properties.Contains(target))
                {
                    removed = section.RemoveProperty(target);
                    return section; // Возвращаем родителя (SectionItem)
                }
            }

            // Поиск в корне
            if (RootItems.Contains(target))
            {
                removed = RootItems.Remove(target);
                return null; // Родитель корневого элемента - это сам RootItems
            }

            return null;
        }

        // Рекурсивный поиск и удаление свойства в группе с определением родителя
        private GroupItem? FindPropertyInGroupAndRemove(PropertyItem target, GroupItem group)
        {
            foreach (var subGroup in group.Groups)
            {
                if (subGroup.Properties.Contains(target))
                {
                    subGroup.RemoveChild(target);
                    return subGroup; // Возвращаем родителя
                }

                var found = FindPropertyInGroupAndRemove(target, subGroup);
                if (found != null)
                    return found;
            }
            return null;
        }
        private object? FindParentOfGroupAndRemove(GroupItem target, out bool removed)
        {
            removed = false;

            // Поиск в секциях
            foreach (var section in RootItems.OfType<SectionItem>())
            {
                if (section.Groups.Contains(target))
                {
                    removed = section.RemoveGroup(target);
                    return section; // Возвращаем родителя (SectionItem)
                }

                var foundParent = FindGroupInGroupsAndRemove(target, section.Groups);
                if (foundParent != null)
                {
                    return foundParent; // Родитель уже определен внутри рекурсии
                }
            }

            // Поиск в группах
            foreach (var group in RootItems.OfType<GroupItem>())
            {
                if (group.Groups.Contains(target))
                {
                    removed = group.RemoveChild(target);
                    return group; // Возвращаем родителя (GroupItem)
                }

                var foundParent = FindGroupInGroupsAndRemove(target, group.Groups);
                if (foundParent != null)
                {
                    return foundParent; // Родитель уже определен внутри рекурсии
                }
            }

            return null;
        }


        // Вспомогательный метод для поиска и удаления группы с определением родителя
        private object? FindAndRemoveGroup(GroupItem target, out bool removed)
        {
            removed = false;

            // Поиск в секциях
            foreach (var section in RootItems.OfType<SectionItem>())
            {
                if (section.Groups.Contains(target))
                {
                    removed = section.RemoveGroup(target);
                    return section; // Возвращаем родителя (SectionItem)
                }

                var foundParent = FindGroupInGroupsAndRemove(target, section.Groups);
                if (foundParent != null)
                {
                    return foundParent; // Родитель уже определен внутри рекурсии
                }
            }

            // Поиск в группах
            foreach (var group in RootItems.OfType<GroupItem>())
            {
                if (group.Groups.Contains(target))
                {
                    removed = group.RemoveChild(target);
                    return group; // Возвращаем родителя (GroupItem)
                }

                var foundParent = FindGroupInGroupsAndRemove(target, group.Groups);
                if (foundParent != null)
                {
                    return foundParent; // Родитель уже определен внутри рекурсии
                }
            }

            return null;
        }

        // Рекурсивный поиск и удаление группы в группах с определением родителя
        private GroupItem? FindGroupInGroupsAndRemove(GroupItem target, ObservableCollection<GroupItem> groups)
        {
            foreach (var group in groups)
            {
                if (group.Groups.Contains(target))
                {
                    group.RemoveChild(target);
                    return group; // Возвращаем родителя
                }

                var found = FindGroupInGroupsAndRemove(target, group.Groups);
                if (found != null)
                    return found;
            }
            return null;
        }

        private bool RemoveGroup(IEnumerable<SectionItem> sections, GroupItem target)
        {
            foreach (var section in sections)
            {
                if (section.RemoveGroup(target))
                {
                    return true;
                }
                foreach (var group in section.Groups)
                {
                    if (RemoveGroup(group.Groups, target)) return true;
                }
            }
            return false;
        }

        private bool RemoveGroup(IEnumerable<GroupItem> groups, GroupItem target)
        {
            foreach (var group in groups)
            {
                if (group.RemoveChild(target))
                {
                    return true;
                }
                if (RemoveGroup(group.Groups, target)) return true;
            }
            return false;
        }

        private bool RemovePropertyFromGroups(IEnumerable<SectionItem> sections, PropertyItem target)
        {
            foreach (var section in sections)
            {
                foreach (var group in section.Groups)
                {
                    if (RemovePropertyFromGroup(group, target)) return true;
                }
            }
            return false;
        }

        private bool RemovePropertyFromGroups(IEnumerable<GroupItem> groups, PropertyItem target)
        {
            foreach (var group in groups)
            {
                if (RemovePropertyFromGroup(group, target)) return true;
            }
            return false;
        }

        private bool RemovePropertyFromGroup(GroupItem group, PropertyItem target)
        {
            if (group.RemoveChild(target))
                return true;
            foreach (var subgroup in group.Groups)
            {
                if (RemovePropertyFromGroup(subgroup, target)) return true;
            }
            return false;
        }

        private void ResetAll()
        {
            var result = MessageBox.Show("Вы уверены, что хотите очистить все данные и начать с нуля?",
                                         "Подтверждение сброса",
                                         MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                RootItems.Clear();
                SelectedItem = null;
                DeleteCommand.NotifyCanExecuteChanged();
            }
        }

        private void LoadXml()
        {
            var openFileDialog = new OpenFileDialog { Filter = "XML файлы (*.xml)|*.xml|Все файлы (*.*)|*.*" };
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    groupIndex = 1;
                    propertyIndex = 1;
                    var loadedItems = LoadXmlToModel(openFileDialog.FileName);
                    RootItems.Clear();
                    foreach (var item in loadedItems)
                        RootItems.Add(item);
                    MessageBox.Show("XML шаблон успешно загружен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке XML: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private ObservableCollection<object> LoadXmlToModel(string filePath)
        {
            var rootItems = new ObservableCollection<object>();
            var doc = XDocument.Load(filePath);
            var root = doc.Root;
            if (root == null || root.Name.LocalName != "consultation")
                throw new System.Exception("Некорректный XML: корневой элемент должен быть <consultation>");
            TemplateName = (string?)root.Attribute("name") ?? "";
            foreach (var element in root.Elements())
            {
                switch (element.Name.LocalName)
                {
                    case "section":
                        rootItems.Add(ParseSection(element));
                        break;
                    case "group":
                        rootItems.Add(ParseGroup(element));
                        break;
                    case "property":
                        rootItems.Add(ParseProperty(element));
                        break;
                }
            }
            return rootItems;
        }

        private SectionItem ParseSection(XElement element)
        {
            var section = new SectionItem
            {
                Code = (string?)element.Attribute("code") ?? "",
                Name = (string?)element.Attribute("name") ?? "",
                Title = (string?)element.Attribute("title") ?? "",
                Semd = (string?)element.Attribute("semd") ?? ""
            };
            foreach (var groupElem in element.Elements("group"))
            {
                var group = ParseGroup(groupElem);
                section.AddGroup(group);
            }
            // Добавляем поддержку свойств в секции
            foreach (var propElem in element.Elements("property"))
            {
                var prop = ParseProperty(propElem);
                section.AddProperty(prop);
            }
            return section;
        }

        private GroupItem ParseGroup(XElement element)
        {
            string caption = (string?)element.Attribute(XName.Get("caption", "http://www.sanatorium-is.ru/editor")) ?? "";
            string nameAttr = (string?)element.Attribute("name") ?? "";
            var group = new GroupItem
            {
                Name = !string.IsNullOrEmpty(nameAttr) ? nameAttr : (!string.IsNullOrEmpty(caption) ? caption : $"Группа_{groupIndex++}"),
                Caption = caption,
                OdCaption = (string?)element.Attribute(XName.Get("caption", "http://www.sanatorium-is.ru/officeDocument")) ?? "",
                Layout = (string?)element.Attribute(XName.Get("layout", "http://www.sanatorium-is.ru/editor")) ?? "DockPanel",
                Separator = (string?)element.Attribute(XName.Get("separator", "http://www.sanatorium-is.ru/editor")) ?? "",
                Suffix = (string?)element.Attribute(XName.Get("suffix", "http://www.sanatorium-is.ru/editor")) ?? "",
                OdSeparator = (string?)element.Attribute(XName.Get("separator", "http://www.sanatorium-is.ru/officeDocument")) ?? "",
                OdSuffix = (string?)element.Attribute(XName.Get("suffix", "http://www.sanatorium-is.ru/officeDocument")) ?? "",
                OdGroupMode = (string?)element.Attribute(XName.Get("groupMode", "http://www.sanatorium-is.ru/officeDocument")) ?? "",
                ECaptionStyle = (string?)element.Attribute(XName.Get("captionStyle", "http://www.sanatorium-is.ru/editor")) ?? "",
                OdGroupStyle = (string?)element.Attribute(XName.Get("groupStyle", "http://www.sanatorium-is.ru/officeDocument")) ?? ""
            };

            // Добавляем проверку для новых свойств
            group.OdGroupModeIsParagraph = !string.IsNullOrEmpty((string?)element.Attribute(XName.Get("groupMode", "http://www.sanatorium-is.ru/officeDocument"))) &&
                                           ((string?)element.Attribute(XName.Get("groupMode", "http://www.sanatorium-is.ru/officeDocument")) == "paragraph");

            group.ECaptionStyleIsGroupHeader = !string.IsNullOrEmpty((string?)element.Attribute(XName.Get("captionStyle", "http://www.sanatorium-is.ru/editor"))) &&
                                               ((string?)element.Attribute(XName.Get("captionStyle", "http://www.sanatorium-is.ru/editor")) == "GroupHeader");

            group.OdGroupStyleIsNewParagraphBoldHeader = !string.IsNullOrEmpty((string?)element.Attribute(XName.Get("groupStyle", "http://www.sanatorium-is.ru/officeDocument"))) &&
                                                         ((string?)element.Attribute(XName.Get("groupStyle", "http://www.sanatorium-is.ru/officeDocument")) == "NewParagraphBoldHeader");

            foreach (var child in element.Elements())
            {
                switch (child.Name.LocalName)
                {
                    case "group":
                        var subGroup = ParseGroup(child);
                        group.AddGroup(subGroup);
                        break;
                    case "property":
                        var prop = ParseProperty(child);
                        group.AddProperty(prop);
                        break;
                }
            }
            return group;
        }

        private PropertyItem ParseProperty(XElement element)
        {
            string caption = (string?)element.Attribute(XName.Get("caption", "http://www.sanatorium-is.ru/editor")) ?? "";
            string nameAttr = (string?)element.Attribute("name") ?? "";
            var prop = new PropertyItem
            {
                Name = !string.IsNullOrEmpty(nameAttr) ? nameAttr : (!string.IsNullOrEmpty(caption) ? caption : $"Свойство_{propertyIndex++}"),
                Caption = caption,
                OdCaption = (string?)element.Attribute(XName.Get("caption", "http://www.sanatorium-is.ru/officeDocument")) ?? "",
                Separator = (string?)element.Attribute(XName.Get("separator", "http://www.sanatorium-is.ru/editor")) ?? "",
                Suffix = (string?)element.Attribute(XName.Get("suffix", "http://www.sanatorium-is.ru/editor")) ?? "",
                OdSeparator = (string?)element.Attribute(XName.Get("separator", "http://www.sanatorium-is.ru/officeDocument")) ?? "",
                OdSuffix = (string?)element.Attribute(XName.Get("suffix", "http://www.sanatorium-is.ru/officeDocument")) ?? "",
                MinWidth = (string?)element.Attribute(XName.Get("MinWidth", "http://schemas.microsoft.com/winfx/2006/xaml/presentation")) ?? "",
                MinLines = (string?)element.Attribute(XName.Get("MinLines", "http://schemas.microsoft.com/winfx/2006/xaml/presentation")) ?? "",
                AutoSuggestName = (string?)element.Attribute(XName.Get("autoSuggestName", "http://www.sanatorium-is.ru/editor")) ?? "",
                Value = (string?)element.Attribute("value") ?? "",
                Semd = (string?)element.Attribute("semd") ?? ""
            };
            string typeStr = (string?)element.Attribute("type") ?? "string";
            prop.Type = typeStr switch
            {
                "bool" => PropertyType.Bool,
                "const" => PropertyType.Const,
                _ => PropertyType.String
            };
            return prop;
        }

        private void SaveXml()
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "XML файлы (*.xml)|*.xml| Все файлы (*.*)|*.*",
                FileName = currentSavePath
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                currentSavePath = saveFileDialog.FileName;
                try
                {
                    var settings = new XmlWriterSettings
                    {
                        Indent = true,
                        IndentChars = "  ",
                        NewLineChars = "\r\n",
                        Encoding = System.Text.Encoding.UTF8
                    };
                    using var writer = XmlWriter.Create(currentSavePath, settings);
                    writer.WriteStartDocument();
                    writer.WriteStartElement("consultation");
                    foreach (var ns in Namespaces.Where(n => n.IsSelected))
                    {
                        if (string.IsNullOrEmpty(ns.Prefix))
                            writer.WriteAttributeString("xmlns", ns.Uri);
                        else
                            writer.WriteAttributeString("xmlns", ns.Prefix, null, ns.Uri);
                    }
                    if (!string.IsNullOrWhiteSpace(templateName))
                        writer.WriteAttributeString("name", templateName);
                    foreach (var item in RootItems)
                    {
                        switch (item)
                        {
                            case SectionItem section:
                                WriteSection(writer, section);
                                break;
                            case GroupItem group:
                                WriteGroup(writer, group);
                                break;
                            case PropertyItem prop:
                                WriteProperty(writer, prop);
                                break;
                        }
                    }
                    // --- Вставка тегов в зависимости от типа ---
                    if (_typeSettings.IsConsultation)
                    {
                        writer.WriteStartElement("e", "consultantDefaultConclusion", "http://www.sanatorium-is.ru/editor");
                        writer.WriteAttributeString("autoSuggestName", "specC");
                        writer.WriteEndElement();
                    }

                    if (_typeSettings.IsInstrumental)
                    {
                        writer.WriteStartElement("e", "instrumentalProbeConclusion", "http://www.sanatorium-is.ru/editor");
                        writer.WriteAttributeString("noPathologyCounter", "True");
                        writer.WriteAttributeString("autoSuggestName", "ИИ.заключение");

                        writer.WriteStartElement("e", "recommendations", "http://www.sanatorium-is.ru/editor");
                        writer.WriteAttributeString("autoSuggestName", "ИИ.рекомендации");
                        writer.WriteEndElement();

                        writer.WriteEndElement(); // instrumentalProbeConclusion
                    }

                    if (_typeSettings.IsLaboratory)
                    {
                        writer.WriteStartElement("e", "probeGenericResultSelection", "http://www.sanatorium-is.ru/editor");
                        writer.WriteEndElement();

                        writer.WriteStartElement("e", "labProbeConclusion", "http://www.sanatorium-is.ru/editor");
                        writer.WriteAttributeString("autoSuggestName", "labProbeC");
                        writer.WriteEndElement();
                    }
                    // --- Конец вставки ---
                    // --- Вставка блоков ---
                    if (_blocksSettings.IsDiagnosis)
                    {
                        writer.WriteStartElement("e", "diagnosisSelection", "http://www.sanatorium-is.ru/editor");
                        writer.WriteEndElement();
                    }

                    if (_blocksSettings.IsAssignments)
                    {
                        writer.WriteStartElement("e", "assignmentsView", "http://www.sanatorium-is.ru/editor");
                        writer.WriteEndElement();
                    }

                    if (_blocksSettings.IsTreatmentActions)
                    {
                        writer.WriteStartElement("e", "treatmentActions", "http://www.sanatorium-is.ru/editor");
                        writer.WriteEndElement();
                    }

                    if (_blocksSettings.IsAttachments)
                    {
                        writer.WriteStartElement("e", "attachments", "http://www.sanatorium-is.ru/editor");
                        writer.WriteEndElement();
                    }
                    // --- Конец вставки блоков ---
                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                    MessageBox.Show($"XML успешно сохранён в:\n{currentSavePath}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении XML:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void WriteGroup(XmlWriter writer, GroupItem group)
        {
            writer.WriteStartElement("group");
            if (!string.IsNullOrEmpty(group.Caption))
                writer.WriteAttributeString("e", "caption", null, group.Caption);
            if (!string.IsNullOrEmpty(group.OdCaption))
                writer.WriteAttributeString("od", "caption", null, group.OdCaption);
            if (!string.IsNullOrEmpty(group.Layout) && group.Layout != "")
                writer.WriteAttributeString("e", "layout", null, group.Layout);
            if (!string.IsNullOrEmpty(group.Separator))
                writer.WriteAttributeString("e", "separator", null, group.Separator);
            if (!string.IsNullOrEmpty(group.Suffix))
                writer.WriteAttributeString("e", "suffix", null, group.Suffix);
            if (!string.IsNullOrEmpty(group.OdSeparator))
                writer.WriteAttributeString("od", "separator", null, group.OdSeparator);
            if (!string.IsNullOrEmpty(group.OdSuffix))
                writer.WriteAttributeString("od", "suffix", null, group.OdSuffix);
            if (group.OdGroupModeIsParagraph)
                writer.WriteAttributeString("od", "groupMode", null, "paragraph");
            if (group.ECaptionStyleIsGroupHeader)
                writer.WriteAttributeString("e", "captionStyle", null, "GroupHeader");
            if (group.OdGroupStyleIsNewParagraphBoldHeader)
                writer.WriteAttributeString("od", "groupStyle", null, "NewParagraphBoldHeader");

            // Добавлено: запись атрибута semd
            if (!string.IsNullOrEmpty(group.Semd))
                writer.WriteAttributeString("semd", null, group.Semd);

            foreach (var prop in group.Properties)
                WriteProperty(writer, prop);
            foreach (var subgroup in group.Groups)
                WriteGroup(writer, subgroup);
            writer.WriteEndElement();
        }

        private void WriteProperty(XmlWriter writer, PropertyItem prop)
        {
            writer.WriteStartElement("property");
            if (!string.IsNullOrEmpty(prop.Caption))
                writer.WriteAttributeString("e", "caption", null, prop.Caption);
            if (!string.IsNullOrEmpty(prop.OdCaption))
                writer.WriteAttributeString("od", "caption", null, prop.OdCaption);
            string typeStr = prop.Type switch
            {
                PropertyType.Bool => "bool",
                PropertyType.Const => "const",
                _ => "string"
            };
            writer.WriteAttributeString("type", typeStr);
            writer.WriteAttributeString("value", prop.Value ?? "");
            if (!string.IsNullOrEmpty(prop.Separator))
                writer.WriteAttributeString("e", "separator", null, prop.Separator);
            if (!string.IsNullOrEmpty(prop.Suffix))
                writer.WriteAttributeString("e", "suffix", null, prop.Suffix);
            if (!string.IsNullOrEmpty(prop.OdSeparator))
                writer.WriteAttributeString("od", "separator", null, prop.OdSeparator);
            if (!string.IsNullOrEmpty(prop.OdSuffix))
                writer.WriteAttributeString("od", "suffix", null, prop.OdSuffix);
            if (!string.IsNullOrEmpty(prop.MinWidth))
                writer.WriteAttributeString("xaml", "MinWidth", null, prop.MinWidth);
            if (!string.IsNullOrEmpty(prop.MinLines))
                writer.WriteAttributeString("xaml", "MinLines", null, prop.MinLines);
            if (!string.IsNullOrEmpty(prop.AutoSuggestName))
                writer.WriteAttributeString("e", "autoSuggestName", null, prop.AutoSuggestName);
            if (!string.IsNullOrEmpty(prop.Semd))
                writer.WriteAttributeString("semd", null, prop.Semd);
            writer.WriteEndElement();
        }
        private void WriteSection(XmlWriter writer, SectionItem section)
        {
            writer.WriteStartElement("section");
            if (!string.IsNullOrEmpty(section.Code))
                writer.WriteAttributeString("code", section.Code);
            writer.WriteAttributeString("name", section.Name ?? "");
            if (!string.IsNullOrEmpty(section.Title))
                writer.WriteAttributeString("title", section.Title);
            if (!string.IsNullOrEmpty(section.Semd))
                writer.WriteAttributeString("semd", section.Semd);
            foreach (var group in section.Groups)
                WriteGroup(writer, group);
            foreach (var prop in section.Properties)
                WriteProperty(writer, prop);
            writer.WriteEndElement();
        }

        private void OpenNamespaceSettings()
        {
            var settingsWindow = new NamespaceSettingsWindow(Namespaces, TemplateName);
            settingsWindow.Owner = Application.Current.MainWindow;
            if (settingsWindow.ShowDialog() == true)
            {
                TemplateName = settingsWindow.TemplateName ?? "";
            }
        }
        private void DuplicateItem()
        {
            if (SelectedItem == null) return;

            object? duplicatedItem = null;

            switch (SelectedItem)
            {
                case SectionItem section:
                    duplicatedItem = DuplicateSection(section);
                    break;
                case GroupItem group:
                    duplicatedItem = DuplicateGroup(group);
                    break;
                case PropertyItem prop:
                    duplicatedItem = DuplicateProperty(prop);
                    break;
            }

            if (duplicatedItem != null)
            {
                var parent = FindParent(SelectedItem);
                InsertAfter(parent, SelectedItem, duplicatedItem);
                SelectedItem = duplicatedItem;
            }
        }

        private SectionItem DuplicateSection(SectionItem section)
        {
            var newSection = new SectionItem
            {
                Code = section.Code,
                Name = $"{section.Name}",
                Title = section.Title,
                Semd = section.Semd,
                IsExpanded = section.IsExpanded
            };

            foreach (var group in section.Groups)
                newSection.AddGroup(DuplicateGroup(group));

            foreach (var prop in section.Properties)
                newSection.AddProperty(DuplicateProperty(prop));

            return newSection;
        }

        private GroupItem DuplicateGroup(GroupItem group)
        {
            var newGroup = new GroupItem
            {
                Name = $"{group.Name}",
                Caption = group.Caption,
                OdCaption = group.OdCaption,
                Layout = group.Layout,
                Separator = group.Separator,
                Suffix = group.Suffix,
                OdSeparator = group.OdSeparator,
                OdSuffix = group.OdSuffix,
                OdGroupMode = group.OdGroupMode,
                OdGroupModeIsParagraph = group.OdGroupModeIsParagraph,
                ECaptionStyle = group.ECaptionStyle,
                ECaptionStyleIsGroupHeader = group.ECaptionStyleIsGroupHeader,
                OdGroupStyle = group.OdGroupStyle,
                OdGroupStyleIsNewParagraphBoldHeader = group.OdGroupStyleIsNewParagraphBoldHeader,
                Semd = group.Semd,
                IsExpanded = group.IsExpanded
            };

            foreach (var subgroup in group.Groups)
                newGroup.AddGroup(DuplicateGroup(subgroup));

            foreach (var prop in group.Properties)
                newGroup.AddProperty(DuplicateProperty(prop));

            return newGroup;
        }

        private PropertyItem DuplicateProperty(PropertyItem prop)
        {
            return new PropertyItem
            {
                Name = $"{prop.Name}",
                Caption = prop.Caption,
                OdCaption = prop.OdCaption,
                Separator = prop.Separator,
                Suffix = prop.Suffix,
                OdSeparator = prop.OdSeparator,
                OdSuffix = prop.OdSuffix,
                MinWidth = prop.MinWidth,
                MinLines = prop.MinLines,
                AutoSuggestName = prop.AutoSuggestName,
                Value = prop.Value,
                Type = prop.Type,
                Semd = prop.Semd
            };
        }

        private object? FindParent(object item)
        {
            if (RootItems.Contains(item))
                return null;

            foreach (var section in RootItems.OfType<SectionItem>())
            {
                if (section.Groups.Contains(item) || section.Properties.Contains(item))
                    return section;

                var parent = FindParentInGroup(item, section.Groups);
                if (parent != null) return parent;
            }

            foreach (var group in RootItems.OfType<GroupItem>())
            {
                if (group.Groups.Contains(item) || group.Properties.Contains(item))
                    return group;

                var parent = FindParentInGroup(item, group.Groups);
                if (parent != null) return parent;
            }

            return null;
        }

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
        public void HandleDrop(object draggedItem, object? targetItem)
        {
            if (draggedItem == null || draggedItem == targetItem)
                return;

            // Запретить вложение секции в секцию
            if (draggedItem is SectionItem && targetItem is SectionItem)
            {
                // Игнорируем или можно просто не менять вложенность
                return;
            }

            // Удаляем draggedItem из текущего места
            RemoveItem(draggedItem);

            if (targetItem == null)
            {
                // Перемещение в корень
                RootItems.Add(draggedItem);
                SelectedItem = draggedItem;
                return;
            }

            // Остальная логика вложения

            switch (targetItem)
            {
                case SectionItem section:
                    if (draggedItem is GroupItem g) section.AddGroup(g);
                    else if (draggedItem is PropertyItem p) section.AddProperty(p);
                    else if (draggedItem is SectionItem)
                    {
                        // Не должно сюда попадать из-за проверки выше
                    }
                    break;

                case GroupItem group:
                    if (draggedItem is GroupItem gr) group.AddGroup(gr);
                    else if (draggedItem is PropertyItem p) group.AddProperty(p);
                    break;

                case PropertyItem _:
                    var parent = FindParent(targetItem);
                    InsertAfter(parent, targetItem, draggedItem);
                    break;

                default:
                    RootItems.Add(draggedItem);
                    break;
            }

            SelectedItem = draggedItem;
        }



        private void RemoveItem(object item)
        {
            if (RootItems.Contains(item))
            {
                RootItems.Remove(item);
                return;
            }

            if (item is PropertyItem prop)
            {
                var parent = FindParent(item);
                if (parent is SectionItem section)
                    section.RemoveProperty(prop);
                else if (parent is GroupItem group)
                    group.RemoveChild(prop);
            }
            else if (item is GroupItem group)
            {
                var parent = FindParent(item);
                if (parent is SectionItem section)
                    section.RemoveGroup(group);
                else if (parent is GroupItem groupParent)
                    groupParent.RemoveChild(group);
            }
            else if (item is SectionItem section)
            {
                RootItems.Remove(section);
            }
        }

        private void InsertAfter(object? parent, object reference, object newItem)
        {
            if (parent == null)
            {
                int index = RootItems.IndexOf(reference);
                if (index >= 0)
                    RootItems.Insert(index + 1, newItem);
            }
            else if (parent is SectionItem section)
            {
                int groupIndex = section.Groups.IndexOf(reference as GroupItem);
                if (groupIndex >= 0)
                {
                    section.Groups.Insert(groupIndex + 1, (GroupItem)newItem);
                    section.Children.Insert(section.Children.IndexOf(reference) + 1, newItem);
                    return;
                }

                int propIndex = section.Properties.IndexOf(reference as PropertyItem);
                if (propIndex >= 0)
                {
                    section.Properties.Insert(propIndex + 1, (PropertyItem)newItem);
                    section.Children.Insert(section.Children.IndexOf(reference) + 1, newItem);
                    return;
                }
            }
            else if (parent is GroupItem group)
            {
                int subGroupIndex = group.Groups.IndexOf(reference as GroupItem);
                if (subGroupIndex >= 0)
                {
                    group.Groups.Insert(subGroupIndex + 1, (GroupItem)newItem);
                    group.Children.Insert(group.Children.IndexOf(reference) + 1, newItem);
                    return;
                }

                int propIndex = group.Properties.IndexOf(reference as PropertyItem);
                if (propIndex >= 0)
                {
                    group.Properties.Insert(propIndex + 1, (PropertyItem)newItem);
                    group.Children.Insert(group.Children.IndexOf(reference) + 1, newItem);
                    return;
                }
            }
        }
        public void MoveItemToRoot(object draggedItem)
        {
            if (draggedItem == null)
                return;

            // Удаляем из текущего места
            RemoveItem(draggedItem);

            // Добавляем в корень последний элемент
            RootItems.Add(draggedItem);

            SelectedItem = draggedItem;
        }

    }
}