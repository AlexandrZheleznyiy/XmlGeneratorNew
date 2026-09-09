using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace XmlGeneratorNew.Models
{
    /// <summary>
    /// Модель для элемента <row> внутри <table>
    /// </summary>
    public partial class RowItem : ObservableObject
    {
        [ObservableProperty]
        private string rowIndex = "";

        [ObservableProperty]
        private string odJustification = "";

        [ObservableProperty]
        private bool isExpanded;

        [ObservableProperty]
        private bool isSelected;

        [ObservableProperty]
        private string _uid = "";

        public ObservableCollection<CellItem> Cells { get; } = new();
        public ObservableCollection<object> Children { get; } = new();

        private void OnDeserialized(StreamingContext context)
        {
            Children.Clear();
            foreach (var c in Cells)
                Children.Add(c);
        }

        public void AddCell(CellItem cell)
        {
            Cells.Add(cell);
            Children.Add(cell);
            IsExpanded = true;
        }

        public bool RemoveCell(CellItem cell)
        {
            bool removed = Cells.Remove(cell);
            if (removed) Children.Remove(cell);
            return removed;
        }

        public bool RemoveChild(object child)
        {
            if (child is CellItem c) return RemoveCell(c);
            return false;
        }
    }
}
