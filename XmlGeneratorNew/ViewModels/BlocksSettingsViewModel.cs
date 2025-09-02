using CommunityToolkit.Mvvm.ComponentModel;

namespace XmlGeneratorNew.ViewModels
{
    public partial class BlocksSettingsViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isDiagnosis = false;

        [ObservableProperty]
        private bool _isAssignments = false;

        [ObservableProperty]
        private bool _isTreatmentActions = false;

        [ObservableProperty]
        private bool _isAttachments = false;

        public BlocksSettingsViewModel(
            bool isDiagnosis = false,
            bool isAssignments = false,
            bool isTreatmentActions = false,
            bool isAttachments = false)
        {
            IsDiagnosis = isDiagnosis;
            IsAssignments = isAssignments;
            IsTreatmentActions = isTreatmentActions;
            IsAttachments = isAttachments;
        }
    }
}