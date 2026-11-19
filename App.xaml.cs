using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;

namespace ZkPdksApp
{
    public partial class App : Application
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        public App()
        {
            // Native ve Unmanaged DLL'lerin 'dller' klasöründen yüklenmesi için Windows DLL arama yolunu ekle
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string dllsDir = Path.Combine(baseDir, "dller");
                if (Directory.Exists(dllsDir))
                {
                    SetDllDirectory(dllsDir);
                }

                // SQLite'ın native Interop DLL'ini dller/x86 altından doğrudan belleğe önyükle
                string is64 = IntPtr.Size == 8 ? "x64" : "x86";
                string sqliteDll = Path.Combine(dllsDir, is64, "SQLite.Interop.dll");
                if (!File.Exists(sqliteDll)) sqliteDll = Path.Combine(dllsDir, "SQLite.Interop.dll");
                if (File.Exists(sqliteDll))
                {
                    LoadLibrary(sqliteDll);
                }
                Environment.SetEnvironmentVariable("PreLoadSQLite_BaseDirectory", dllsDir);
            }
            catch { }

            // Managed .NET DLL'leri için fallback çözümleyici
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                try
                {
                    string assemblyName = new AssemblyName(args.Name).Name + ".dll";
                    string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dller", assemblyName);
                    if (File.Exists(dllPath))
                    {
                        return Assembly.LoadFrom(dllPath);
                    }
                }
                catch { }
                return null;
            };
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            // Detaylı çökme ve hata yakalama sistemini ilk anda kur
            Logger.SetupExceptionHandlers();
            Logger.Log("App.OnStartup tamamlandı, MainWindow başlatılıyor...");

            base.OnStartup(e);
        }
    }
}
