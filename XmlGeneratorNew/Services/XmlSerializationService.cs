using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

        /// <summary>
        /// Загружает XML файл и преобразует в модель данных
        /// </summary>
        public (ObservableCollection<object> RootItems, string TemplateName, TypeSettingsViewModel TypeSettings, BlocksSettingsViewModel BlockSettings) LoadFromFile(string filePath)
        {
            _groupIndex = 1;
            _propertyIndex = 1;

            var rootItems = new ObservableCollection<object>();
            var doc = XDocument.Load(filePath);
            var root = doc.Root;

            if (root == null || root.Name.LocalName != "consultation")
                throw new Exception("Некорректный XML: корневой элемент должен быть <consultation>");

            string templateName = (string?)root.Attribute("name") ?? "";

            // Определяем настройки типов и блоков из содержимого XML
            var typeSettings = DetectTypeSettings(root);
            var blockSettings = DetectBlockSettings(root);

            foreach (var element in root.Elements())
            {
                var item = ParseElement(element);
                if (item != null)
                    rootItems.Add(item);
            }

            return (rootItems, templateName, typeSettings, blockSettings);
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

        private object? ParseElement(XElement element)
        {
            return element.Name.LocalName switch
            {
                "section" => ParseSection(element),
                "group" => ParseGroup(element),
                "property" => ParseProperty(element),
                "consultantDefaultConclusion" or "instrumentalProbeConclusion" or
                "labProbeConclusion" or "probeGenericResultSelection" => FooterItemNames.Conclusion,
                "diagnosisSelection" => FooterItemNames.Diagnosis,
                "icfSectionInitial" => FooterItemNames.IcfInitial,
                "icfSectionRecurrent" => ParseIcfRecurrent(element),
                "assignmentsView" => FooterItemNames.Assignments,
                "treatmentActions" => FooterItemNames.TreatmentActions,
                "attachments" => FooterItemNames.Attachments,
                _ => null
            };
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
                        writer.WriteAttributeString("autoSuggestName", "specC");
                        writer.WriteEndElement();
                    }
                    else if (typeSettings.IsInstrumental)
                    {
                        writer.WriteStartElement("e", "instrumentalProbeConclusion", XmlNamespaces.Editor);
                        writer.WriteAttributeString("noPathologyCounter", "True");
                        writer.WriteAttributeString("autoSuggestName", "ИИ.заключение");
                        writer.WriteStartElement("e", "recommendations", XmlNamespaces.Editor);
                        writer.WriteAttributeString("autoSuggestName", "ИИ.рекомендации");
                        writer.WriteEndElement();
                        writer.WriteEndElement();
                    }
                    else if (typeSettings.IsLaboratory)
                    {
                        writer.WriteStartElement("e", "probeGenericResultSelection", XmlNamespaces.Editor);
                        writer.WriteEndElement();
                        writer.WriteStartElement("e", "labProbeConclusion", XmlNamespaces.Editor);
                        writer.WriteAttributeString("autoSuggestName", "labProbeC");
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
