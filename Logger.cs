using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace ZkPdksApp
{
    public static class Logger
    {
        private static readonly string logFolder;
        private static readonly string sysLogFile;
        private static readonly string crashDumpFile;
        private static readonly object lockObj = new object();

        static Logger()
        {
            try
            {
                // Uygulamanın çalıştığı dizine ve AppData içerisine log tut
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                logFolder = Path.Combine(baseDir, "Logs");
                if (!Directory.Exists(logFolder))
                {
                    Directory.CreateDirectory(logFolder);
                }

                sysLogFile = Path.Combine(logFolder, "syslog.txt");
                crashDumpFile = Path.Combine(logFolder, "crash_dump.txt");

                Log("=== Uygulama Başlatıldı ===");
                Log($"OS: {Environment.OSVersion}, .NET: {Environment.Version}, 64bit: {Environment.Is64BitProcess}");
            }
            catch { }
        }

        public static void Log(string message)
        {
            try
            {
                lock (lockObj)
                {
                    string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
                    File.AppendAllText(sysLogFile, line + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch { }
        }

        public static void LogException(string context, Exception ex)
        {
            try
            {
                lock (lockObj)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("================================================================================");
                    sb.AppendLine($"[CRASH DUMP] - {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                    sb.AppendLine($"Konum / Bağlam : {context}");
                    sb.AppendLine($"Hata Türü      : {ex.GetType().FullName}");
                    sb.AppendLine($"Mesaj          : {ex.Message}");
                    sb.AppendLine($"HResult        : 0x{ex.HResult:X8}");
                    sb.AppendLine($"StackTrace     :\n{ex.StackTrace}");

                    if (ex.InnerException != null)
                    {
                        sb.AppendLine("--- Inner Exception ---");
                        sb.AppendLine($"Tür            : {ex.InnerException.GetType().FullName}");
                        sb.AppendLine($"Mesaj          : {ex.InnerException.Message}");
                        sb.AppendLine($"StackTrace     :\n{ex.InnerException.StackTrace}");
                    }
                    sb.AppendLine("================================================================================");

                    File.AppendAllText(crashDumpFile, sb.ToString(), Encoding.UTF8);
                    File.AppendAllText(sysLogFile, $"[HATA] {context}: {ex.Message}" + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch { }
        }

        public static void SetupExceptionHandlers()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                LogException("AppDomain.CurrentDomain.UnhandledException (Kritik Çökme)", ex ?? new Exception("Bilinmeyen hata nesnesi: " + e.ExceptionObject));
                MessageBox.Show($"Kritik Hata Oluştu!\nDetaylar log dosyasına kaydedildi:\n{crashDumpFile}\n\nHata: {ex?.Message}",
                    "Kritik Hata / Crash Dump", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            Application.Current.DispatcherUnhandledException += (s, e) =>
            {
                LogException("DispatcherUnhandledException (WPF Arayüz Hatası)", e.Exception);
                MessageBox.Show($"WPF Arayüz Hatası:\n{e.Exception.Message}\n\nDetaylar: {crashDumpFile}",
                    "Arayüz Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Handled = true; // Uygulamanın çökmesini engelle
            };
        }
    }
}
