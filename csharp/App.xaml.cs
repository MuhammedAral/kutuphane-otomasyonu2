using System.Windows;

namespace KutuphaneOtomasyon
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                MessageBox.Show($"Beklenmeyen hata: {args.ExceptionObject}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            };
            
            DispatcherUnhandledException += (s, args) =>
            {
                MessageBox.Show($"UI Hatası: {args.Exception.Message}\n\n{args.Exception.StackTrace}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };
            
            
            // Not: Veritabanı tabloları API tarafından oluşturulur
            // DatabaseHelper sadece API çalışmadığında doğrudan erişim için kullanılır
        }
    }
}
