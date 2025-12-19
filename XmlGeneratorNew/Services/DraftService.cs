using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using XmlGeneratorNew.DTOs;
using XmlGeneratorNew.Models;
using XmlGeneratorNew.ViewModels;

namespace XmlGeneratorNew.Services
{
    /// <summary>
    /// Сервис для работы с черновиками проекта
    /// </summary>
    public class DraftService
    {
        private const string DRAFT_FILE_NAME = "draft.json";

        /// <summary>
        /// Получает путь к файлу черновика
        /// </summary>
        private string GetDraftPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DRAFT_FILE_NAME);
        }

        /// <summary>
        /// Сохраняет черновик проекта
        /// </summary>
        public async Task SaveDraftAsync(DraftData draftData)
        {
            try
            {
                string json = JsonConvert.SerializeObject(
                    draftData,
                    Newtonsoft.Json.Formatting.Indented,
                    new JsonSerializerSettings
                    {
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                    });

                string path = GetDraftPath();
                await File.WriteAllTextAsync(path, json);
                System.Diagnostics.Debug.WriteLine($"Черновик сохранён в {path}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения черновика: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Загружает черновик проекта
        /// </summary>
        public async Task<DraftData?> LoadDraftAsync()
        {
            string path = GetDraftPath();
            if (!File.Exists(path))
                return null;

            try
            {
                string json = await File.ReadAllTextAsync(path);
                var draftData = JsonConvert.DeserializeObject<DraftData>(json);

                if (draftData != null)
                {
                    // Восстанавливаем структуру Children для всех элементов
                    foreach (var dto in draftData.RootItems)
                    {
                        switch (dto.ItemType)
                        {
                            case "section":
                                var s = JsonConvert.DeserializeObject<SectionItem>(dto.Json);
                                if (s != null) RebuildChildren(s);
                                break;
                            case "group":
                                var g = JsonConvert.DeserializeObject<GroupItem>(dto.Json);
                                if (g != null) RebuildChildren(g);
                                break;
                        }
                    }
                }

                return draftData;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки черновика: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Удаляет файл черновика
        /// </summary>
        public void DeleteDraft()
        {
            try
            {
                string path = GetDraftPath();
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка удаления черновика: {ex.Message}");
            }
        }

        /// <summary>
        /// Проверяет существование файла черновика
        /// </summary>
        public bool DraftExists()
        {
            return File.Exists(GetDraftPath());
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
        }
    }
}
