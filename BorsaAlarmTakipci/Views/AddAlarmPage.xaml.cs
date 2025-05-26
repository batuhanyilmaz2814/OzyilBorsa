using BorsaAlarmTakipci.ViewModels;
using Microsoft.Maui.Controls;

namespace BorsaAlarmTakipci.Views
{
    public partial class AddAlarmPage : ContentPage
    {
        public AddAlarmPage(AddAlarmPageViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
