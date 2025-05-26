using Microsoft.Maui.Controls;

namespace BorsaAlarmTakipci
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Rota kayıtlarını burada yapın
            Routing.RegisterRoute("AddAlarmPage", typeof(Views.AddAlarmPage));
        }
    }
}
