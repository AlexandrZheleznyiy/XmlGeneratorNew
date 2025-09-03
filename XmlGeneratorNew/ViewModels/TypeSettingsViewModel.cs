using CommunityToolkit.Mvvm.ComponentModel;

namespace XmlGeneratorNew.ViewModels
{
    public partial class TypeSettingsViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isConsultation = true;

        [ObservableProperty]
        private bool _isInstrumental;

        [ObservableProperty]
        private bool _isLaboratory;

        public TypeSettingsViewModel(bool isConsultation = false, bool isInstrumental = false, bool isLaboratory = false)
        {
            IsConsultation = isConsultation;
            IsInstrumental = isInstrumental;
            IsLaboratory = isLaboratory;
        }
    }
}