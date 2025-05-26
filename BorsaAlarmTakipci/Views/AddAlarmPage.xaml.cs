using BorsaAlarmTakipci.ViewModels;

namespace BorsaAlarmTakipci.Views; // Namespace'in Views içerdiðine dikkat edin

public partial class AddAlarmPage : ContentPage
{
    public AddAlarmPage(AddAlarmPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
