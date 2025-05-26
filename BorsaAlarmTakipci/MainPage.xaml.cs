using BorsaAlarmTakipci.ViewModels;
using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace BorsaAlarmTakipci // NAMESPACE EKLENDİ
{
    public partial class MainPage : ContentPage
    {
        private readonly MainPageViewModel _viewModel;

        public MainPage(MainPageViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.OnPageAppearingAsync();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _viewModel.OnPageDisappearing();
        }
    }
}
