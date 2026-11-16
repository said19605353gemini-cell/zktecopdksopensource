using System.Windows;

namespace ZkPdksApp
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Detaylı çökme ve hata yakalama sistemini ilk anda kur
            Logger.SetupExceptionHandlers();
            Logger.Log("App.OnStartup tamamlandı, MainWindow başlatılıyor...");

            base.OnStartup(e);
        }
    }
}
