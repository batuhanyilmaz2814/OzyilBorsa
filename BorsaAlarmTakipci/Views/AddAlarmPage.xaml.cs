using BorsaAlarmTakipci.ViewModels;

namespace BorsaAlarmTakipci.Views; // Namespace'in Views i�erdi�ine dikkat edin

public partial class AddAlarmPage : ContentPage
{
    public AddAlarmPage(AddAlarmPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
