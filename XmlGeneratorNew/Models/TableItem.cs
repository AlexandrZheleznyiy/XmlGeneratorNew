using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace XmlGeneratorNew.Models
{
    /// <summary>
    /// Модель для элемента <table>
    /// </summary>
    public partial class TableItem : ObservableObject
    {
        [ObservableProperty]
        private string eStyle = "";

        [ObservableProperty]
        private string eTableStretch = "";

        [ObservableProperty]
        private string odTableStretch = "";

        [ObservableProperty]
        private bool isExpanded;

        [ObservableProperty]
        private bool isSelected;

        [ObservableProperty]
        private string _uid = "";

        public ObservableCollection<RowItem> Rows { get; } = new();
        public ObservableCollection<object> Children { get; } = new();

        private void OnDeserialized(StreamingContext context)
        {
            Children.Clear();
            foreach (var r in Rows)
                Children.Add(r);
        }

        public void AddRow(RowItem row)
        {
            Rows.Add(row);
            Children.Add(row);
            IsExpanded = true;
        }

        public bool RemoveRow(RowItem row)
        {
            bool removed = Rows.Remove(row);
            if (removed) Children.Remove(row);
            return removed;
        }

        public bool RemoveChild(object child)
        {
            if (child is RowItem r) return RemoveRow(r);
            return false;
        }
    }
}
