using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using XmlGeneratorNew.Constants;
using XmlGeneratorNew.DTOs;
using XmlGeneratorNew.Models;
using XmlGeneratorNew.Services;
using XmlGeneratorNew.Views;

namespace XmlGeneratorNew.ViewModels
{
    /// <summary>
    /// Главная ViewModel приложения
    /// </summary>
    public partial class MainViewModel : ObservableObject
    {
        #region Поля и свойства

        [ObservableProperty]
        private string templateName = "";

        [ObservableProperty]
        private object? selectedItem;

        public ObservableCollection<object> RootItems { get; } = new();
        public ObservableCollection<string> FooterItems { get; } = new();

        public ObservableCollection<NamespaceItem> Namespaces { get; } = new()
        {
            new NamespaceItem { Prefix = "e", Uri = XmlNamespaces.Editor, IsSelected = true },
            new NamespaceItem { Prefix = "xaml", Uri = XmlNamespaces.Xaml, IsSelected = true },
            new NamespaceItem { Prefix = "x", Uri = XmlNamespaces.XamlX, IsSelected = true },
            new NamespaceItem { Prefix = "od", Uri = XmlNamespaces.OfficeDocument, IsSelected = true },
            new NamespaceItem { Prefix = "precompile", Uri = XmlNamespaces.Precompile, IsSelected = true },
            new NamespaceItem { Prefix = "p", Uri = XmlNamespaces.Programming, IsSelected = false }
        };

        private TypeSettingsViewModel _typeSettings = new();
        private BlocksSettingsViewModel _blocksSettings = new();
        private int _groupIndex = 1;
        private int _propertyIndex = 1;
        private string _currentSavePath = "metadata.xml";

        // Сервисы
        private readonly XmlSerializationService _xmlService;
        private readonly DraftService _draftService;
        private readonly TreeManipulationService _treeService;
        private readonly ItemDuplicationService _duplicationService;
        private readonly DragDropService _dragDropService;

        #endregion

        #region Команды

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
        public IRelayCommand SaveDraftCommand { get; }

        #endregion

        #region Конструктор

        public MainViewModel()
        {
            // Инициализация сервисов
            _xmlService = new XmlSerializationService();
            _draftService = new DraftService();
            _treeService = new TreeManipulationService();
            _duplicationService = new ItemDuplicationService();
            _dragDropService = new DragDropService(_treeService);

            // Инициализация команд
            AddSectionToRootCommand = new RelayCommand(AddSectionToRoot);
            AddGroupToRootCommand = new RelayCommand(AddGroupToRoot);
            AddPropertyToRootCommand = new RelayCommand(AddPropertyToRoot);
            AddGroupCommand = new RelayCommand(AddGroup);
            AddPropertyCommand = new RelayCommand(AddProperty);
            AddSectionCommand = new RelayCommand(AddSection);
            DeleteCommand = new RelayCommand(DeleteSelected, CanDelete);
            ResetCommand = new RelayCommand(ResetAll);
            LoadCommand = new RelayCommand(LoadXml);
            SaveCommand = new RelayCommand(SaveXml);
            OpenNamespaceSettingsCommand = new RelayCommand(OpenNamespaceSettings);
            DuplicateCommand = new RelayCommand<object>(
                execute: obj => DuplicateItem(),
                canExecute: obj => SelectedItem != null
            );
            OpenTypeSettingsCommand = new RelayCommand(OpenTypeSettings);
            OpenBlocksSettingsCommand = new RelayCommand(OpenBlocksSettings);
            SaveDraftCommand = new RelayCommand(async () => await SaveDraftAsync());

            FooterItems.CollectionChanged += FooterItems_CollectionChanged;
            UpdateFooterItemsFromSettings();
        }

        #endregion

        #region Обработка изменений

        partial void OnSelectedItemChanged(object? oldValue, object? newValue)
        {
            System.Diagnostics.Debug.WriteLine($"[VM] SelectedItem changed from {oldValue?.GetType().Name ?? "null"} to {newValue?.GetType().Name ?? "null"}");

            // Уведомляем команды о изменении
            DeleteCommand.NotifyCanExecuteChanged();
            AddGroupCommand.NotifyCanExecuteChanged();
            AddPropertyCommand.NotifyCanExecuteChanged();
            AddSectionCommand.NotifyCanExecuteChanged();

            // Сбрасываем выделение старого элемента
            if (oldValue is SectionItem oldSection) oldSection.IsSelected = false;
            else if (oldValue is GroupItem oldGroup) oldGroup.IsSelected = false;
            else if (oldValue is PropertyItem oldProp) oldProp.IsSelected = false;

            // Устанавливаем выделение нового элемента
            if (newValue is SectionItem newSection) newSection.IsSelected = true;
            else if (newValue is GroupItem newGroup) newGroup.IsSelected = true;
            else if (newValue is PropertyItem newProp) newProp.IsSelected = true;
        }

        private void FooterItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Обработка изменений в футере при необходимости
        }

        #endregion

        #region Работа с черновиками

        /// <summary>
        /// Сохраняет текущее состояние как черновик
        /// </summary>
        public async Task SaveDraftAsync()
        {
            try
            {
                var draftData = new DraftData
                {
                    TemplateName = TemplateName,
                    FooterItems = new List<string>(FooterItems),
                    TypeSettings = _typeSettings,
                    BlockSettings = _blocksSettings
                };

                foreach (var item in RootItems)
                {
                    DraftItemDto? dto = item switch
                    {
                        SectionItem s => new DraftItemDto
                        {
                            ItemType = "section",
                            Json = JsonConvert.SerializeObject(s)
                        },
                        GroupItem g => new DraftItemDto
                        {
                            ItemType = "group",
                            Json = JsonConvert.SerializeObject(g)
                        },
                        PropertyItem p => new DraftItemDto
                        {
                            ItemType = "property",
                            Json = JsonConvert.SerializeObject(p)
                        },
                        string footer when _treeService.IsFooterString(footer) => new DraftItemDto
                        {
                            ItemType = "footer",
                            Json = JsonConvert.SerializeObject(footer)
                        },
                        _ => null
                    };

                    if (dto != null)
                        draftData.RootItems.Add(dto);
                }

                await _draftService.SaveDraftAsync(draftData);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения черновика: {ex.Message}");
                MessageBox.Show("Не удалось сохранить черновик.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Загружает черновик
        /// </summary>
        private async Task<bool> LoadDraftAsync()
        {
            try
            {
                var draftData = await _draftService.LoadDraftAsync();
                if (draftData == null)
                    return false;

                RootItems.Clear();
                FooterItems.Clear();

                TemplateName = draftData.TemplateName;

                foreach (var dto in draftData.RootItems)
                {
                    switch (dto.ItemType)
                    {
                        case "section":
                            var s = JsonConvert.DeserializeObject<SectionItem>(dto.Json);
                            if (s != null)
                            {
                                RebuildChildren(s); // ← ДОБАВЛЕНО
                                RootItems.Add(s);
                            }
                            break;
                        case "group":
                            var g = JsonConvert.DeserializeObject<GroupItem>(dto.Json);
                            if (g != null)
                            {
                                RebuildChildren(g); // ← ДОБАВЛЕНО
                                RootItems.Add(g);
                            }
                            break;
                        case "property":
                            var p = JsonConvert.DeserializeObject<PropertyItem>(dto.Json);
                            if (p != null) RootItems.Add(p);
                            break;
                        case "footer":
                            var f = JsonConvert.DeserializeObject<string>(dto.Json);
                            if (!string.IsNullOrEmpty(f)) FooterItems.Add(f);
                            break;
                    }
                }

                foreach (var item in draftData.FooterItems)
                {
                    if (!FooterItems.Contains(item))
                        FooterItems.Add(item);
                }

                _typeSettings = draftData.TypeSettings ?? new TypeSettingsViewModel();
                _blocksSettings = draftData.BlockSettings ?? new BlocksSettingsViewModel();

                UpdateFooterItemsFromSettings();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки черновика: {ex.Message}");
                return false;
            }
        }
        /// <summary>
        /// Восстанавливает коллекцию Children для секции
        /// </summary>
        private void RebuildChildren(SectionItem section)
        {
            section.Children.Clear();
            foreach (var g in section.Groups)
            {
                section.Children.Add(g);
                RebuildChildren(g); // Рекурсивно восстанавливаем вложенные группы
            }
            foreach (var p in section.Properties)
                section.Children.Add(p);
        }

        /// <summary>
        /// Восстанавливает коллекцию Children для группы
        /// </summary>
        private void RebuildChildren(GroupItem group)
        {
            group.Children.Clear();
            foreach (var g in group.Groups)
            {
                group.Children.Add(g);
                RebuildChildren(g); // Рекурсивно восстанавливаем вложенные группы
            }
            foreach (var p in group.Properties)
                group.Children.Add(p);
        }

        /// <summary>
        /// Предлагает восстановить черновик при запуске
        /// </summary>
        public async Task<bool> TryOfferRestoreDraftAsync()
        {
            if (!_draftService.DraftExists())
                return false;

            var result = MessageBox.Show(
                "Найден сохранённый проект. Хотите продолжить работу с ним?",
                "Продолжить работу",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                bool loaded = await LoadDraftAsync();
                if (!loaded)
                {
                    MessageBox.Show("Не удалось загрузить сохранённый проект.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    _draftService.DeleteDraft();
                }
                return loaded;
            }
            else
            {
                _draftService.DeleteDraft();
                RootItems.Clear();
                FooterItems.Clear();
                TemplateName = string.Empty;
                UpdateFooterItemsFromSettings();
                return false;
            }
        }

        /// <summary>
        /// Спрашивает о сохранении черновика при выходе
        /// </summary>
        public async Task<bool> AskSaveDraftOnExitAsync()
        {
            var result = MessageBox.Show(
                "Сохранить текущий проект как черновик перед выходом?",
                "Сохранить черновик",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Cancel)
                return false;

            if (result == MessageBoxResult.Yes)
                await SaveDraftAsync();

            return true;
        }

        #endregion

        #region Команды добавления элементов

        private async void AddSection()
        {
            var newSection = new SectionItem
            {
                Name = $"Секция {RootItems.OfType<SectionItem>().Count() + 1}",
                IsExpanded = true,
                IsSelected = true
            };

            RootItems.Add(newSection);
            SelectedItem = newSection;
            await SaveDraftAsync();
        }

        private async void AddGroup()
        {
            if (SelectedItem is SectionItem selectedSection)
            {
                var newGroup = new GroupItem { Name = $"Группа {_groupIndex++}", IsExpanded = true, IsSelected = true };
                selectedSection.AddGroup(newGroup);
                SelectedItem = newGroup;
            }
            else if (SelectedItem is GroupItem selectedGroup)
            {
                var newSubGroup = new GroupItem { Name = $"Группа {_groupIndex++}", IsExpanded = true, IsSelected = true };
                selectedGroup.AddGroup(newSubGroup);
                SelectedItem = newSubGroup;
            }
            else
            {
                var newGroup = new GroupItem { Name = $"Группа {_groupIndex++}", IsExpanded = true, IsSelected = true };
                RootItems.Add(newGroup);
                SelectedItem = newGroup;
            }

            await SaveDraftAsync();
        }

        private async void AddProperty()
        {
            var prop = new PropertyItem { Name = $"Свойство {_propertyIndex++}" };

            if (SelectedItem is GroupItem group)
            {
                group.AddProperty(prop);
            }
            else if (SelectedItem is SectionItem section)
            {
                section.AddProperty(prop);
            }
            else
            {
                RootItems.Add(prop);
            }

            SelectedItem = prop;
            await SaveDraftAsync();
        }

        private void AddSectionToRoot()
        {
            var newSection = new SectionItem
            {
                Name = $"Секция_{RootItems.OfType<SectionItem>().Count() + 1}",
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
                Name = $"Группа_{_groupIndex++}",
                IsExpanded = true,
                IsSelected = true
            };
            RootItems.Add(newGroup);
            SelectedItem = newGroup;
        }

        private void AddPropertyToRoot()
        {
            var newProperty = new PropertyItem { Name = $"Свойство_{_propertyIndex++}" };
            RootItems.Add(newProperty);
            SelectedItem = newProperty;
        }

        #endregion

        #region Команды удаления и сброса

        private bool CanDelete() => SelectedItem != null;

        private async void DeleteSelected()
        {
            if (SelectedItem == null) return;

            object itemToRemove = SelectedItem;
            bool removed = _treeService.RemoveItem(itemToRemove, RootItems, FooterItems);

            if (removed)
            {
                // Сбрасываем IsSelected у удаленного элемента
                if (itemToRemove is SectionItem si) si.IsSelected = false;
                else if (itemToRemove is GroupItem gi) gi.IsSelected = false;

                SelectedItem = null;
                DeleteCommand.NotifyCanExecuteChanged();
                AddGroupCommand.NotifyCanExecuteChanged();
                AddPropertyCommand.NotifyCanExecuteChanged();
                AddSectionCommand.NotifyCanExecuteChanged();

                await SaveDraftAsync();
            }
        }

        private async void ResetAll()
        {
            var result = MessageBox.Show(
                "Очистить всё? Это сбросит дерево элементов, настройки типов, блоков и имя шаблона.",
                "Сброс",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                // Очищаем дерево элементов
                RootItems.Clear();
                FooterItems.Clear();
                SelectedItem = null;

                // Сбрасываем имя шаблона
                TemplateName = string.Empty;

                // Сбрасываем настройки типов
                _typeSettings = new TypeSettingsViewModel();

                // Сбрасываем настройки блоков
                _blocksSettings = new BlocksSettingsViewModel();

                // Обновляем футер на основе сброшенных настроек
                UpdateFooterItemsFromSettings();

                // Уведомляем команды
                DeleteCommand.NotifyCanExecuteChanged();

                // Сохраняем пустой черновик
                await SaveDraftAsync();

                MessageBox.Show(
                    "Все данные успешно сброшены.",
                    "Сброс завершён",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        #endregion

        #region Работа с XML

        private async void LoadXml()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "XML файлы (*.xml)|*.xml|Все файлы (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    _groupIndex = 1;
                    _propertyIndex = 1;

                    var (rootItems, templateName) = _xmlService.LoadFromFile(openFileDialog.FileName);

                    RootItems.Clear();
                    FooterItems.Clear();

                    foreach (var item in rootItems)
                    {
                        if (item is string strItem && _treeService.IsFooterString(strItem))
                            FooterItems.Add(strItem);
                        else
                            RootItems.Add(item);
                    }

                    TemplateName = templateName;
                    UpdateFooterItemsFromSettings();

                    MessageBox.Show("XML успешно загружен.", "Загрузка XML", MessageBoxButton.OK, MessageBoxImage.Information);
                    await SaveDraftAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки XML: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveXml()
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "XML файлы (*.xml)|*.xml|Все файлы (*.*)|*.*",
                FileName = _currentSavePath
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                _currentSavePath = saveFileDialog.FileName;
                try
                {
                    _xmlService.SaveToFile(
                        _currentSavePath,
                        TemplateName,
                        RootItems,
                        FooterItems,
                        Namespaces,
                        _typeSettings);

                    MessageBox.Show($"XML успешно сохранён в:\n{_currentSavePath}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении XML:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region Настройки

        private void OpenNamespaceSettings()
        {
            var settingsWindow = new NamespaceSettingsWindow(Namespaces, TemplateName);
            settingsWindow.Owner = Application.Current.MainWindow;
            if (settingsWindow.ShowDialog() == true)
            {
                TemplateName = settingsWindow.TemplateName ?? "";
            }
        }

        private void OpenTypeSettings()
        {
            var settingsWindow = new TypeSettingsWindow(_typeSettings);
            settingsWindow.Owner = Application.Current.MainWindow;
            if (settingsWindow.ShowDialog() == true)
            {
                _typeSettings = settingsWindow.ViewModel;
                UpdateFooterItemsFromSettings();
            }
        }

        private void OpenBlocksSettings()
        {
            var settingsWindow = new BlocksSettingsWindow(_blocksSettings);
            settingsWindow.Owner = Application.Current.MainWindow;
            if (settingsWindow.ShowDialog() == true)
            {
                _blocksSettings = settingsWindow.ViewModel;
                UpdateFooterItemsFromSettings();
            }
        }

        private void UpdateFooterItemsFromSettings()
        {
            var newFooterItems = new List<string>();

            // Диагнозы
            if (_blocksSettings.IsDiagnosis)
                newFooterItems.Add(FooterItemNames.Diagnosis);

            if (_blocksSettings.IsIcfInitial)
                newFooterItems.Add(FooterItemNames.IcfInitial);

            if (_blocksSettings.IsIcfRecurrent)
                newFooterItems.Add(FooterItemNames.IcfRecurrent);

            if (_blocksSettings.IsIcfFinal)
                newFooterItems.Add(FooterItemNames.IcfFinal);

            // Лечение
            if (_blocksSettings.IsAssignments)
                newFooterItems.Add(FooterItemNames.Assignments);

            if (_blocksSettings.IsTreatmentActions)
                newFooterItems.Add(FooterItemNames.TreatmentActions);

            if (_blocksSettings.IsAttachments)
                newFooterItems.Add(FooterItemNames.Attachments);

            // Заключение из настроек типов
            if (_typeSettings.IsConsultation || _typeSettings.IsLaboratory || _typeSettings.IsInstrumental)
                newFooterItems.Add(FooterItemNames.Conclusion);

            // Обновляем коллекцию FooterItems
            if (!FooterItems.SequenceEqual(newFooterItems))
            {
                FooterItems.Clear();
                foreach (var item in newFooterItems)
                {
                    FooterItems.Add(item);
                }
            }
        }


        #endregion

        #region Дублирование элементов

        private void DuplicateItem()
        {
            if (SelectedItem == null) return;

            object? duplicatedItem = SelectedItem switch
            {
                SectionItem section => _duplicationService.DuplicateSection(section),
                GroupItem group => _duplicationService.DuplicateGroup(group),
                PropertyItem prop => _duplicationService.DuplicateProperty(prop),
                _ => null
            };

            if (duplicatedItem != null)
            {
                var parent = _treeService.FindParent(SelectedItem, RootItems);
                InsertAfter(parent, SelectedItem, duplicatedItem);
                SelectedItem = duplicatedItem;
                _ = SaveDraftAsync();
            }
        }

        private void InsertAfter(object? parent, object reference, object newItem)
        {
            if (parent == null)
            {
                int index = RootItems.IndexOf(reference);
                if (index >= 0)
                    RootItems.Insert(index + 1, newItem);
                return;
            }

            if (parent is SectionItem section)
            {
                if (newItem is GroupItem g) section.AddGroup(g);
                else if (newItem is PropertyItem p) section.AddProperty(p);
            }
            else if (parent is GroupItem group)
            {
                if (newItem is GroupItem g) group.AddGroup(g);
                else if (newItem is PropertyItem p) group.AddProperty(p);
            }
        }

        #endregion

        #region Drag & Drop

        /// <summary>
        /// Обрабатывает операцию Drag & Drop
        /// </summary>
        public void HandleDrop(object draggedItem, object? targetItem)
        {
            _dragDropService.HandleDrop(draggedItem, targetItem, RootItems, FooterItems);
            SelectedItem = draggedItem;
            _ = SaveDraftAsync();
        }

        /// <summary>
        /// Перемещает элемент в корень
        /// </summary>
        public void MoveItemToRoot(object draggedItem)
        {
            if (draggedItem == null) return;

            _treeService.RemoveItem(draggedItem, RootItems, FooterItems);
            RootItems.Add(draggedItem);
            SelectedItem = draggedItem;
        }

        #endregion
    }
}
