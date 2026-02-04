using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using XmlGeneratorNew.Constants;
using XmlGeneratorNew.Models;
using XmlGeneratorNew.ViewModels;

namespace XmlGeneratorNew.Services
{
    /// <summary>
    /// Сервис для сериализации и десериализации XML
    /// </summary>
    public class XmlSerializationService
    {
        private int _groupIndex = 1;
        private int _propertyIndex = 1;
        private Dictionary<string, UnparsedElement> _unparsedElements = new();

        /// <summary>
        /// Загружает XML файл и преобразует в модель данных
        /// </summary>
        public (ObservableCollection<object> RootItems, string TemplateName, TypeSettingsViewModel TypeSettings, BlocksSettingsViewModel BlockSettings) LoadFromFile(string filePath)
        {
            _groupIndex = 1;
            _propertyIndex = 1;
            _unparsedElements.Clear();

            System.Diagnostics.Debug.WriteLine($"[LoadXml] Начало загрузки: {filePath}");

            var rootItems = new ObservableCollection<object>();
            var doc = XDocument.Load(filePath);
            var root = doc.Root;

            if (root == null || root.Name.LocalName != "consultation")
                throw new Exception("Некорректный XML: корневой элемент должен быть <consultation>");

            string templateName = (string?)root.Attribute("name") ?? "";

            var typeSettings = DetectTypeSettings(root);
            var blockSettings = DetectBlockSettings(root);

            foreach (var element in root.Elements())
            {
                var item = ParseElement(element);
                if (item != null)
                    rootItems.Add(item);
            }

            System.Diagnostics.Debug.WriteLine($"[LoadXml] Нераспознанных элементов: {_unparsedElements.Count}");

            // Сохраняем нераспознанные элементы в файл
            if (_unparsedElements.Count > 0)
            {
                SaveUnparsedElementsToFile(filePath);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[LoadXml] Все элементы распознаны успешно");
            }

            return (rootItems, templateName, typeSettings, blockSettings);
        }

        private object? ParseElement(XElement element)
        {
            object? result = element.Name.LocalName switch
            {
                "section" => ParseSection(element),
                "group" => ParseGroup(element),
                "property" => ParseProperty(element),
                "consultantDefaultConclusion" => FooterItemNames.Conclusion,
                "instrumentalProbeConclusion" => FooterItemNames.Conclusion,
                "labProbeConclusion" => FooterItemNames.Conclusion,
                "probeGenericResultSelection" => FooterItemNames.Conclusion,
                "diagnosisSelection" => FooterItemNames.Diagnosis,
                "icfSectionInitial" => FooterItemNames.IcfInitial,
                "icfSectionRecurrent" => ParseIcfRecurrent(element),
                "assignmentsView" => FooterItemNames.Assignments,
                "treatmentActions" => FooterItemNames.TreatmentActions,
                "attachments" => FooterItemNames.Attachments,
                _ => null
            };

            // Если элемент не распознан, добавляем в список
            if (result == null)
            {
                AddUnparsedElement(element);
            }

            return result;
        }

        /// <summary>
        /// Добавляет нераспознанный элемент в словарь
        /// </summary>
        private void AddUnparsedElement(XElement element)
        {
            string tagName = element.Name.LocalName;
            string fullXml = element.ToString();

            System.Diagnostics.Debug.WriteLine($"[AddUnparsed] Найден нераспознанный элемент: <{tagName}>");

            // Если элемент уже есть, увеличиваем счётчик
            if (_unparsedElements.ContainsKey(tagName))
            {
                _unparsedElements[tagName].Count++;
                System.Diagnostics.Debug.WriteLine($"[AddUnparsed] Увеличен счётчик для <{tagName}>: {_unparsedElements[tagName].Count}");
            }
            else
            {
                var unparsed = new UnparsedElement
                {
                    TagName = tagName,
                    InnerXml = element.Value,
                    FullXml = fullXml
                };

                // Собираем атрибуты
                foreach (var attr in element.Attributes())
                {
                    unparsed.Attributes[attr.Name.ToString()] = attr.Value;
                }

                _unparsedElements[tagName] = unparsed;
                System.Diagnostics.Debug.WriteLine($"[AddUnparsed] Добавлен новый элемент <{tagName}>");
            }
        }

        /// <summary>
        /// Сохраняет нераспознанные элементы в файл
        /// </summary>
        private void SaveUnparsedElementsToFile(string originalXmlPath)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[SaveUnparsed] Начало сохранения. Путь: {originalXmlPath}");

                // Получаем путь к папке "Загрузки"
                string downloadsFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads"
                );

                // Если папка Downloads не существует (например, на русской Windows это "Загрузки")
                if (!Directory.Exists(downloadsFolder))
                {
                    downloadsFolder = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        "Загрузки"
                    );
                }

                // Если и это не сработало, используем рабочий стол
                if (!Directory.Exists(downloadsFolder))
                {
                    downloadsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    System.Diagnostics.Debug.WriteLine($"[SaveUnparsed] Папка загрузок не найдена, используем Desktop: {downloadsFolder}");
                }

                string fileName = Path.GetFileNameWithoutExtension(originalXmlPath);
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string noParsingFilePath = Path.Combine(downloadsFolder, $"{fileName}_NoParsing_{timestamp}.xml");

                System.Diagnostics.Debug.WriteLine($"[SaveUnparsed] Полный путь к файлу: {noParsingFilePath}");

                // Создаём XML документ
                var xmlDoc = new XDocument(
                    new XDeclaration("1.0", "utf-8", "yes"),
                    new XElement("UnparsedElements",
                        new XAttribute("source", Path.GetFileName(originalXmlPath)),
                        new XAttribute("sourcePath", originalXmlPath),
                        new XAttribute("analysisDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                        new XAttribute("totalCount", _unparsedElements.Count),

                        // Добавляем инструкции как комментарий
                        new XComment(@"
=============================================================
ИНСТРУКЦИЯ ПО ИСПОЛЬЗОВАНИЮ:
1. Изучите нераспознанные теги ниже
2. Добавьте их обработку в XmlSerializationService.ParseElement()
3. При необходимости создайте новые модели данных
4. Обновите константы в FooterItemNames.cs
=============================================================
"),

                        // Добавляем каждый нераспознанный элемент
                        from kvp in _unparsedElements.OrderBy(x => x.Key)
                        let element = kvp.Value
                        select new XElement("UnparsedElement",
                            new XAttribute("tagName", element.TagName),
                            new XAttribute("count", element.Count),

                            // Добавляем атрибуты
                            element.Attributes.Count > 0
                                ? new XElement("Attributes",
                                    from attr in element.Attributes
                                    select new XElement("Attribute",
                                        new XAttribute("name", attr.Key),
                                        new XAttribute("value", attr.Value)
                                    )
                                )
                                : null,

                            // Добавляем полный XML без CDATA - парсим и вставляем как XElement
                            new XElement("FullXml",
                                TryParseXml(element.FullXml)
                            )
                        )
                    )
                );

                // Сохраняем с форматированием
                var settings = new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "  ",
                    NewLineChars = "\r\n",
                    Encoding = Encoding.UTF8
                };

                using (var writer = XmlWriter.Create(noParsingFilePath, settings))
                {
                    xmlDoc.Save(writer);
                }

                System.Diagnostics.Debug.WriteLine($"[SaveUnparsed] ✅ Файл успешно сохранён: {noParsingFilePath}");
                System.Diagnostics.Debug.WriteLine($"[SaveUnparsed] Размер файла: {new FileInfo(noParsingFilePath).Length} байт");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SaveUnparsed] ❌ Ошибка сохранения: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[SaveUnparsed] Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Пытается распарсить XML строку в XElement, если не получается - возвращает как текст
        /// </summary>
        private object TryParseXml(string xmlString)
        {
            try
            {
                return XElement.Parse(xmlString);
            }
            catch
            {
                // Если не удалось распарсить, возвращаем как обычный текст
                return xmlString;
            }
        }



        /// <summary>
        /// Определяет настройки типа документа из XML
        /// </summary>
        private TypeSettingsViewModel DetectTypeSettings(XElement root)
        {
            var settings = new TypeSettingsViewModel();

            foreach (var element in root.Descendants())
            {
                switch (element.Name.LocalName)
                {
                    case "consultantDefaultConclusion":
                        settings.IsConsultation = true;
                        break;
                    case "instrumentalProbeConclusion":
                        settings.IsInstrumental = true;
                        break;
                    case "labProbeConclusion":
                    case "probeGenericResultSelection":
                        settings.IsLaboratory = true;
                        break;
                }
            }

            return settings;
        }

        /// <summary>
        /// Определяет настройки блоков из XML
        /// </summary>
        private BlocksSettingsViewModel DetectBlockSettings(XElement root)
        {
            var settings = new BlocksSettingsViewModel();

            foreach (var element in root.Descendants())
            {
                switch (element.Name.LocalName)
                {
                    case "diagnosisSelection":
                        settings.IsDiagnosis = true;
                        break;
                    case "icfSectionInitial":
                        settings.IsIcfInitial = true;
                        break;
                    case "icfSectionRecurrent":
                        // Проверяем, это повторный или заключительный
                        var hasInitialColumn = element.Attribute(XName.Get("initialIcfValueColumnName", XmlNamespaces.Editor)) != null;
                        var hasCurrentColumn = element.Attribute(XName.Get("currentIcfValueColumnName", XmlNamespaces.Editor)) != null;

                        if (hasInitialColumn && hasCurrentColumn)
                            settings.IsIcfFinal = true;
                        else
                            settings.IsIcfRecurrent = true;
                        break;
                    case "assignmentsView":
                        settings.IsAssignments = true;
                        break;
                    case "treatmentActions":
                        settings.IsTreatmentActions = true;
                        break;
                    case "attachments":
                        settings.IsAttachments = true;
                        break;
                }
            }

            return settings;
        }

        /// <summary>
        /// Сохраняет модель данных в XML файл
        /// </summary>
        public void SaveToFile(
            string filePath,
            string templateName,
            ObservableCollection<object> rootItems,
            ObservableCollection<string> footerItems,
            ObservableCollection<NamespaceItem> namespaces,
            TypeSettingsViewModel typeSettings)
        {
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\r\n",
                Encoding = System.Text.Encoding.UTF8
            };

            using var writer = XmlWriter.Create(filePath, settings);
            writer.WriteStartDocument();
            writer.WriteStartElement("consultation");

            // Записываем namespaces
            foreach (var ns in namespaces.Where(n => n.IsSelected))
            {
                if (string.IsNullOrEmpty(ns.Prefix))
                    writer.WriteAttributeString("xmlns", ns.Uri);
                else
                    writer.WriteAttributeString("xmlns", ns.Prefix, null, ns.Uri);
            }

            if (!string.IsNullOrWhiteSpace(templateName))
                writer.WriteAttributeString("name", templateName);

            // Записываем основные элементы
            foreach (var item in rootItems)
            {
                WriteItem(writer, item);
            }

            // Записываем футер
            foreach (var footerItem in footerItems)
            {
                WriteFooterItem(writer, footerItem, typeSettings);
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        /// <summary>
        /// Определяет тип МКФ элемента (повторный или заключительный)
        /// </summary>
        private string ParseIcfRecurrent(XElement element)
        {
            // Проверяем наличие атрибутов для заключительного диагноза
            var hasInitialColumn = element.Attribute(XName.Get("initialIcfValueColumnName", XmlNamespaces.Editor)) != null;
            var hasCurrentColumn = element.Attribute(XName.Get("currentIcfValueColumnName", XmlNamespaces.Editor)) != null;

            if (hasInitialColumn && hasCurrentColumn)
                return FooterItemNames.IcfFinal;

            return FooterItemNames.IcfRecurrent;
        }

        private SectionItem ParseSection(XElement element)
        {
            var section = new SectionItem
            {
                Code = (string?)element.Attribute("code") ?? "",
                Name = (string?)element.Attribute("name") ?? "",
                Title = (string?)element.Attribute("title") ?? "",
                Semd = (string?)element.Attribute("semd") ?? "",
                Uid = (string?)element.Attribute("uid") ?? ""
            };

            foreach (var groupElem in element.Elements("group"))
            {
                var group = ParseGroup(groupElem);
                section.AddGroup(group);
            }

            foreach (var propElem in element.Elements("property"))
            {
                var prop = ParseProperty(propElem);
                section.AddProperty(prop);
            }

            return section;
        }

        private GroupItem ParseGroup(XElement element)
        {
            string caption = (string?)element.Attribute(XName.Get("caption", XmlNamespaces.Editor)) ?? "";
            string nameAttr = (string?)element.Attribute("name") ?? "";

            var group = new GroupItem
            {
                Name = !string.IsNullOrEmpty(nameAttr) ? nameAttr :
                       (!string.IsNullOrEmpty(caption) ?
                       string.Join(" ", Regex.Split(caption, @"[\s\(\)]+")
                           .Where(s => !string.IsNullOrEmpty(s)).Take(3)) :
                       $"Группа_{_groupIndex++}"),
                Caption = caption,
                OdCaption = (string?)element.Attribute(XName.Get("caption", XmlNamespaces.OfficeDocument)) ?? "",
                Layout = (string?)element.Attribute(XName.Get("layout", XmlNamespaces.Editor)) ?? "DockPanel",
                Separator = (string?)element.Attribute(XName.Get("separator", XmlNamespaces.Editor)) ?? "",
                Suffix = (string?)element.Attribute(XName.Get("suffix", XmlNamespaces.Editor)) ?? "",
                OdSeparator = (string?)element.Attribute(XName.Get("separator", XmlNamespaces.OfficeDocument)) ?? "",
                OdSuffix = (string?)element.Attribute(XName.Get("suffix", XmlNamespaces.OfficeDocument)) ?? "",
                OdGroupMode = (string?)element.Attribute(XName.Get("groupMode", XmlNamespaces.OfficeDocument)) ?? "",
                ECaptionStyle = (string?)element.Attribute(XName.Get("captionStyle", XmlNamespaces.Editor)) ?? "",
                OdGroupStyle = (string?)element.Attribute(XName.Get("groupStyle", XmlNamespaces.OfficeDocument)) ?? "",
                Semd = (string?)element.Attribute("semd") ?? "",
                Uid = (string?)element.Attribute("uid") ?? ""
            };

            group.OdGroupModeIsParagraph = group.OdGroupMode == "paragraph";
            group.ECaptionStyleIsGroupHeader = group.ECaptionStyle == "GroupHeader";
            group.OdGroupStyleIsNewParagraphBoldHeader = group.OdGroupStyle == "NewParagraphBoldHeader";

            foreach (var child in element.Elements())
            {
                switch (child.Name.LocalName)
                {
                    case "group":
                        group.AddGroup(ParseGroup(child));
                        break;
                    case "property":
                        group.AddProperty(ParseProperty(child));
                        break;
                }
            }

            return group;
        }

        private PropertyItem ParseProperty(XElement element)
        {
            string caption = (string?)element.Attribute(XName.Get("caption", XmlNamespaces.Editor)) ?? "";
            string nameAttr = (string?)element.Attribute("name") ?? "";

            var prop = new PropertyItem
            {
                Name = !string.IsNullOrEmpty(nameAttr) ? nameAttr :
                       (!string.IsNullOrEmpty(caption) ?
                       string.Join(" ", Regex.Split(caption, @"[\s\(\)]+")
                           .Where(s => !string.IsNullOrEmpty(s)).Take(3)) :
                       $"Свойство_{_propertyIndex++}"),
                Caption = caption,
                OdCaption = (string?)element.Attribute(XName.Get("caption", XmlNamespaces.OfficeDocument)) ?? "",
                Separator = (string?)element.Attribute(XName.Get("separator", XmlNamespaces.Editor)) ?? "",
                Suffix = (string?)element.Attribute(XName.Get("suffix", XmlNamespaces.Editor)) ?? "",
                OdSeparator = (string?)element.Attribute(XName.Get("separator", XmlNamespaces.OfficeDocument)) ?? "",
                OdSuffix = (string?)element.Attribute(XName.Get("suffix", XmlNamespaces.OfficeDocument)) ?? "",
                MinWidth = (string?)element.Attribute(XName.Get("MinWidth", XmlNamespaces.Xaml)) ?? "",
                MinLines = (string?)element.Attribute(XName.Get("MinLines", XmlNamespaces.Xaml)) ?? "",
                AutoSuggestName = (string?)element.Attribute(XName.Get("autoSuggestName", XmlNamespaces.Editor)) ?? "",
                Value = (string?)element.Attribute("value") ?? "",
                Semd = (string?)element.Attribute("semd") ?? "",
                Uid = (string?)element.Attribute("uid") ?? ""
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

        private void WriteItem(XmlWriter writer, object item)
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
            if (!string.IsNullOrEmpty(section.Uid))
                writer.WriteAttributeString("uid", section.Uid);

            foreach (var child in section.Children)
            {
                WriteItem(writer, child);
            }

            writer.WriteEndElement();
        }

        private void WriteGroup(XmlWriter writer, GroupItem group)
        {
            writer.WriteStartElement("group");
            if (!string.IsNullOrEmpty(group.Caption))
                writer.WriteAttributeString("e", "caption", null, group.Caption);
            if (!string.IsNullOrEmpty(group.OdCaption))
                writer.WriteAttributeString("od", "caption", null, group.OdCaption);
            if (!string.IsNullOrEmpty(group.Layout))
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
            if (!string.IsNullOrEmpty(group.Semd))
                writer.WriteAttributeString("semd", null, group.Semd);
            if (!string.IsNullOrEmpty(group.Uid))
                writer.WriteAttributeString("uid", group.Uid);

            foreach (var child in group.Children)
            {
                WriteItem(writer, child);
            }

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
            if (!string.IsNullOrEmpty(prop.Uid))
                writer.WriteAttributeString("uid", prop.Uid);

            writer.WriteEndElement();
        }

        private void WriteFooterItem(XmlWriter writer, string itemName, TypeSettingsViewModel typeSettings)
        {
            switch (itemName)
            {
                case FooterItemNames.Conclusion:
                    if (typeSettings.IsConsultation)
                    {
                        writer.WriteStartElement("e", "consultantDefaultConclusion", XmlNamespaces.Editor);
                        writer.WriteAttributeString("e", "autoSuggestName", XmlNamespaces.Editor, "specC");
                        writer.WriteEndElement();
                    }
                    else if (typeSettings.IsInstrumental)
                    {
                        writer.WriteStartElement("e", "instrumentalProbeConclusion", XmlNamespaces.Editor);
                        writer.WriteAttributeString("e", "noPathologyCounter", XmlNamespaces.Editor, "True");
                        writer.WriteAttributeString("e", "autoSuggestName", XmlNamespaces.Editor, "ИИ.заключение");
                        writer.WriteStartElement("e", "recommendations", XmlNamespaces.Editor);
                        writer.WriteAttributeString("e", "autoSuggestName", XmlNamespaces.Editor, "ИИ.рекомендации");
                        writer.WriteEndElement();
                        writer.WriteEndElement();
                    }
                    else if (typeSettings.IsLaboratory)
                    {
                        writer.WriteStartElement("e", "probeGenericResultSelection", XmlNamespaces.Editor);
                        writer.WriteEndElement();
                        writer.WriteStartElement("e", "labProbeConclusion", XmlNamespaces.Editor);
                        writer.WriteAttributeString("e", "autoSuggestName", XmlNamespaces.Editor, "labProbeC");
                        writer.WriteEndElement();
                    }
                    break;

                case FooterItemNames.Diagnosis:
                    writer.WriteStartElement("e", "diagnosisSelection", XmlNamespaces.Editor);
                    writer.WriteEndElement();
                    break;

                case FooterItemNames.IcfInitial:
                    writer.WriteStartElement("e", "icfSectionInitial", XmlNamespaces.Editor);
                    writer.WriteAttributeString("e", "emptySectionMessage", null, "Функциональный/реабилитационный диагноз не указан");
                    writer.WriteEndElement();
                    break;

                case FooterItemNames.IcfRecurrent:
                    writer.WriteStartElement("e", "icfSectionRecurrent", XmlNamespaces.Editor);
                    writer.WriteAttributeString("e", "emptySectionMessage", null, "Функциональный/реабилитационный диагноз не изменен");
                    writer.WriteEndElement();
                    break;

                case FooterItemNames.IcfFinal:
                    writer.WriteStartElement("e", "icfSectionRecurrent", XmlNamespaces.Editor);
                    writer.WriteAttributeString("e", "emptySectionMessage", null, "Функциональный/реабилитационный диагноз не изменен");
                    writer.WriteAttributeString("e", "initialIcfValueColumnName", null, "Исходно");
                    writer.WriteAttributeString("e", "currentIcfValueColumnName", null, "Заключительно");
                    writer.WriteEndElement();
                    break;

                case FooterItemNames.Assignments:
                    writer.WriteStartElement("e", "assignmentsView", XmlNamespaces.Editor);
                    writer.WriteEndElement();
                    break;

                case FooterItemNames.TreatmentActions:
                    writer.WriteStartElement("e", "treatmentActions", XmlNamespaces.Editor);
                    writer.WriteEndElement();
                    break;

                case FooterItemNames.Attachments:
                    writer.WriteStartElement("e", "attachments", XmlNamespaces.Editor);
                    writer.WriteEndElement();
                    break;
            }
        }

    }
}
