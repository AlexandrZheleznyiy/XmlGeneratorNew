using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.IO;
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
        public IRelayCommand AddOneOfCommand { get; }
        public IRelayCommand AddTableCommand { get; }
        public IRelayCommand AddRowCommand { get; }
        public IRelayCommand AddCellCommand { get; }
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
            AddOneOfCommand = new RelayCommand(AddOneOf);
            AddTableCommand = new RelayCommand(AddTable);
            AddRowCommand = new RelayCommand(AddRow);
            AddCellCommand = new RelayCommand(AddCell);
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
            else if (oldValue is OneOfItem oldOneOf) oldOneOf.IsSelected = false;
            else if (oldValue is TableItem oldTable) oldTable.IsSelected = false;
            else if (oldValue is RowItem oldRow) oldRow.IsSelected = false;
            else if (oldValue is CellItem oldCell) oldCell.IsSelected = false;

            // Устанавливаем выделение нового элемента
            if (newValue is SectionItem newSection) newSection.IsSelected = true;
            else if (newValue is GroupItem newGroup) newGroup.IsSelected = true;
            else if (newValue is PropertyItem newProp) newProp.IsSelected = true;
            else if (newValue is OneOfItem newOneOf) newOneOf.IsSelected = true;
            else if (newValue is TableItem newTable) newTable.IsSelected = true;
            else if (newValue is RowItem newRow) newRow.IsSelected = true;
            else if (newValue is CellItem newCell) newCell.IsSelected = true;
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
                        OneOfItem o => new DraftItemDto
                        {
                            ItemType = "oneOf",
                            Json = JsonConvert.SerializeObject(o)
                        },
                        TableItem t => new DraftItemDto
                        {
                            ItemType = "table",
                            Json = JsonConvert.SerializeObject(t)
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
                                RebuildChildren(s);
                                RootItems.Add(s);
                            }
                            break;
                        case "group":
                            var g = JsonConvert.DeserializeObject<GroupItem>(dto.Json);
                            if (g != null)
                            {
                                RebuildChildren(g);
                                RootItems.Add(g);
                            }
                            break;
                        case "property":
                            var p = JsonConvert.DeserializeObject<PropertyItem>(dto.Json);
                            if (p != null) RootItems.Add(p);
                            break;
                        case "oneOf":
                            var o = JsonConvert.DeserializeObject<OneOfItem>(dto.Json);
                            if (o != null)
                            {
                                RebuildChildren(o);
                                RootItems.Add(o);
                            }
                            break;
                        case "table":
                            var t = JsonConvert.DeserializeObject<TableItem>(dto.Json);
                            if (t != null)
                            {
                                RebuildChildren(t);
                                RootItems.Add(t);
                            }
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
                RebuildChildren(g);
            }
            foreach (var p in section.Properties)
                section.Children.Add(p);
            foreach (var o in section.OneOfs)
            {
                section.Children.Add(o);
                RebuildChildren(o);
            }
            foreach (var t in section.Tables)
            {
                section.Children.Add(t);
                RebuildChildren(t);
            }
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
                RebuildChildren(g);
            }
            foreach (var p in group.Properties)
                group.Children.Add(p);
            foreach (var o in group.OneOfs)
            {
                group.Children.Add(o);
                RebuildChildren(o);
            }
            foreach (var t in group.Tables)
            {
                group.Children.Add(t);
                RebuildChildren(t);
            }
        }

        private void RebuildChildren(OneOfItem oneOf)
        {
            oneOf.Children.Clear();
            foreach (var p in oneOf.Properties)
                oneOf.Children.Add(p);
        }

        private void RebuildChildren(TableItem table)
        {
            table.Children.Clear();
            foreach (var r in table.Rows)
            {
                table.Children.Add(r);
                RebuildChildren(r);
            }
        }

        private void RebuildChildren(RowItem row)
        {
            row.Children.Clear();
            foreach (var c in row.Cells)
            {
                row.Children.Add(c);
                RebuildChildren(c);
            }
        }

        private void RebuildChildren(CellItem cell)
        {
            cell.Children.Clear();
            foreach (var p in cell.Properties)
                cell.Children.Add(p);
            foreach (var g in cell.Groups)
            {
                cell.Children.Add(g);
                RebuildChildren(g);
            }
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
            var newGroup = new GroupItem { Name = $"Группа {_groupIndex++}", IsExpanded = true, IsSelected = true };
            if (SelectedItem is SectionItem selectedSection)
            {
                selectedSection.AddGroup(newGroup);
            }
            else if (SelectedItem is GroupItem selectedGroup)
            {
                selectedGroup.AddGroup(newGroup);
            }
            else if (SelectedItem is CellItem selectedCell)
            {
                selectedCell.AddGroup(newGroup);
            }
            else
            {
                RootItems.Add(newGroup);
            }

            SelectedItem = newGroup;
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
            else if (SelectedItem is OneOfItem oneOf)
            {
                oneOf.AddProperty(prop);
            }
            else if (SelectedItem is CellItem cell)
            {
                cell.AddProperty(prop);
            }
            else
            {
                RootItems.Add(prop);
            }

            SelectedItem = prop;
            await SaveDraftAsync();
        }

        private async void AddOneOf()
        {
            var oneOf = new OneOfItem { Name = $"OneOf_{_groupIndex++}", IsExpanded = true, IsSelected = true };
            if (SelectedItem is SectionItem section)
                section.AddOneOf(oneOf);
            else if (SelectedItem is GroupItem group)
                group.AddOneOf(oneOf);
            else
                RootItems.Add(oneOf);

            SelectedItem = oneOf;
            await SaveDraftAsync();
        }

        private async void AddTable()
        {
            var table = new TableItem { IsExpanded = true, IsSelected = true };
            if (SelectedItem is SectionItem section)
                section.AddTable(table);
            else if (SelectedItem is GroupItem group)
                group.AddTable(table);
            else
                RootItems.Add(table);

            SelectedItem = table;
            await SaveDraftAsync();
        }

        private async void AddRow()
        {
            if (SelectedItem is TableItem table)
            {
                var row = new RowItem { RowIndex = table.Rows.Count.ToString(), IsExpanded = true, IsSelected = true };
                table.AddRow(row);
                SelectedItem = row;
                await SaveDraftAsync();
            }
        }

        private async void AddCell()
        {
            if (SelectedItem is RowItem row)
            {
                var cell = new CellItem { Col = row.Cells.Count.ToString(), IsExpanded = true, IsSelected = true };
                row.AddCell(cell);
                SelectedItem = cell;
                await SaveDraftAsync();
            }
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

                    var (rootItems, templateName, typeSettings, blockSettings) = _xmlService.LoadFromFile(openFileDialog.FileName);

                    RootItems.Clear();
                    FooterItems.Clear();

                    TemplateName = templateName;
                    _typeSettings = typeSettings;
                    _blocksSettings = blockSettings;

                    foreach (var item in rootItems)
                    {
                        if (item is string strItem && _treeService.IsFooterString(strItem))
                            FooterItems.Add(strItem);
                        else
                            RootItems.Add(item);
                    }

                    UpdateFooterItemsFromSettings();

                    // Проверяем наличие файла NoParsing в папке Загрузки
                    string downloadsFolder = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        "Downloads"
                    );

                    if (!Directory.Exists(downloadsFolder))
                    {
                        downloadsFolder = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                            "Загрузки"
                        );
                    }

                    string fileName = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                    var noParsingFiles = Directory.GetFiles(downloadsFolder, $"{fileName}_NoParsing_*.xml")
                                                  .OrderByDescending(f => File.GetCreationTime(f))
                                                  .FirstOrDefault();

                    if (noParsingFiles != null)
                    {
                        MessageBox.Show(
                            $"XML загружен.\n\n⚠️ Обнаружены нераспознанные элементы!\n\nОтчёт сохранён:\n{noParsingFiles}",
                            "Внимание",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }

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
                OneOfItem oneOf => _duplicationService.DuplicateOneOf(oneOf),
                TableItem table => _duplicationService.DuplicateTable(table),
                RowItem row => _duplicationService.DuplicateRow(row),
                CellItem cell => _duplicationService.DuplicateCell(cell),
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
                else if (newItem is OneOfItem o) section.AddOneOf(o);
                else if (newItem is TableItem t) section.AddTable(t);
            }
            else if (parent is GroupItem group)
            {
                if (newItem is GroupItem g) group.AddGroup(g);
                else if (newItem is PropertyItem p) group.AddProperty(p);
                else if (newItem is OneOfItem o) group.AddOneOf(o);
                else if (newItem is TableItem t) group.AddTable(t);
            }
            else if (parent is OneOfItem oneOf && newItem is PropertyItem prop)
            {
                oneOf.AddProperty(prop);
            }
            else if (parent is TableItem table && newItem is RowItem r)
            {
                table.AddRow(r);
            }
            else if (parent is RowItem row && newItem is CellItem c)
            {
                row.AddCell(c);
            }
            else if (parent is CellItem cell)
            {
                if (newItem is PropertyItem cp) cell.AddProperty(cp);
                else if (newItem is GroupItem cg) cell.AddGroup(cg);
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
