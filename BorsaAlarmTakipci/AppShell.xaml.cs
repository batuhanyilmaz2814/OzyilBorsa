using BorsaAlarmTakipci.Views;

namespace BorsaAlarmTakipci
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(AddAlarmPage), typeof(AddAlarmPage)); // AddAlarmPage için rota kaydı
        }
    }
}
