using CommunityToolkit.Mvvm.ComponentModel;

namespace XmlGeneratorNew.ViewModels
{
    /// <summary>
    /// ViewModel для настроек блоков документа
    /// </summary>
    public partial class BlocksSettingsViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool isDiagnosis;

        [ObservableProperty]
        private bool isIcfInitial;

        [ObservableProperty]
        private bool isIcfRecurrent;

        [ObservableProperty]
        private bool isIcfFinal;

        [ObservableProperty]
        private bool isAssignments;

        [ObservableProperty]
        private bool isTreatmentActions;

        [ObservableProperty]
        private bool isAttachments;
    }
}
