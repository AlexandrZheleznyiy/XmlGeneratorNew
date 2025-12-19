using System.Collections.Generic;

namespace XmlGeneratorNew.Models
{
    /// <summary>
    /// Модель для нераспознанного XML элемента
    /// </summary>
    public class UnparsedElement
    {
        /// <summary>
        /// Имя тега
        /// </summary>
        public string TagName { get; set; } = "";

        /// <summary>
        /// Словарь атрибутов (имя -> значение)
        /// </summary>
        public Dictionary<string, string> Attributes { get; set; } = new();

        /// <summary>
        /// Внутренний XML контент (если есть дочерние элементы)
        /// </summary>
        public string InnerXml { get; set; } = "";

        /// <summary>
        /// Полный XML элемента
        /// </summary>
        public string FullXml { get; set; } = "";

        /// <summary>
        /// Количество встреч этого элемента
        /// </summary>
        public int Count { get; set; } = 1;
    }
}
