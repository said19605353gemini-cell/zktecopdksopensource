using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using zkemkeeper;

public class ZkService
{
    private CZKEM axCZKEM;
    private bool isConnected = false;
    private bool isInitialized = false;
    private bool isSimulator = false;

    public bool IsConnected => isConnected;
    public bool IsInitialized => isInitialized;
    public string InitError { get; private set; }
    public int MachineNumber { get; set; } = 1;

    [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
    private static extern bool SetDllDirectory(string lpPathName);

    public ZkService()
    {
        try
        {
            // Önce uygulama dizinini DLL arama sırasının en başına al
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            SetDllDirectory(appDir);
        }
        catch { }

        InitializeDriver();
    }

    private void InitializeDriver()
    {
        try
        {
            ZkPdksApp.Logger.Log("ZkService: Sürücü başlatılıyor...");
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            SetDllDirectory(appDir);

            // HKCU veya ProgID üzerinden zkemkeeper COM nesnesini üret
            Type zkType = Type.GetTypeFromProgID("zkemkeeper.ZKEM") ?? Type.GetTypeFromCLSID(new Guid("00853A19-BD51-419B-9269-2DABE57EB61F"));

            if (zkType != null)
            {
                axCZKEM = (CZKEM)Activator.CreateInstance(zkType);
                isInitialized = true;
            }
        }
        catch (Exception ex)
        {
            isInitialized = false;
            InitError = ex.Message;
            ZkPdksApp.Logger.LogException("ZkService.InitializeDriver", ex);
        }
    }

    public bool Connect(string ipAddress, int port = 4370, int commKey = 0)
    {
        if (ipAddress == "127.0.0.1" || ipAddress.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var req = (HttpWebRequest)WebRequest.Create("http://127.0.0.1:3030/api/device/state");
                req.Timeout = 1500;
                using (var resp = req.GetResponse())
                {
                    isSimulator = true;
                    isConnected = true;
                    ZkPdksApp.Logger.Log("ZkService: Sanal ZKTeco Simülatörüne bağlandı.");
                    return true;
                }
            }
            catch { }
        }

        if (!isInitialized)
        {
            InitializeDriver();
            if (!isInitialized) throw new Exception("ZKTeco Sürücüsü başlatılamadı: " + InitError);
        }

        try
        {
            if (commKey > 0)
            {
                try { axCZKEM.SetCommPassword(commKey); } catch { }
            }
            isConnected = axCZKEM.Connect_Net(ipAddress, port);
            isSimulator = false;
            return isConnected;
        }
        catch (Exception ex)
        {
            ZkPdksApp.Logger.LogException("ZkService.Connect", ex);
            throw new Exception("Bağlantı Hatası: " + ex.Message);
        }
    }

    public void Disconnect()
    {
        if (isConnected)
        {
            if (axCZKEM != null && !isSimulator)
            {
                try { axCZKEM.Disconnect(); } catch { }
            }
            isConnected = false;
            isSimulator = false;
        }
    }

    /// <summary>
    /// Cihazın dahili saatini okur.
    /// </summary>
    public DateTime? GetDeviceTime()
    {
        if (!isConnected) return null;

        if (isSimulator)
        {
            try
            {
                string json = FetchHttp("http://127.0.0.1:3030/api/device/state");
                var m = Regex.Match(json, @"""deviceTime""\s*:\s*""([^""]+)""");
                if (m.Success && DateTime.TryParse(m.Groups[1].Value, out DateTime dt))
                {
                    return dt.ToLocalTime();
                }
            }
            catch { }
            return DateTime.Now;
        }

        if (axCZKEM == null) return null;

        int dwYear = 0, dwMonth = 0, dwDay = 0, dwHour = 0, dwMinute = 0, dwSecond = 0;
        if (axCZKEM.GetDeviceTime(MachineNumber, ref dwYear, ref dwMonth, ref dwDay, ref dwHour, ref dwMinute, ref dwSecond))
        {
            return new DateTime(dwYear, dwMonth, dwDay, dwHour, dwMinute, dwSecond);
        }

        return null;
    }

    /// <summary>
    /// Cihazın saatini bilgisayar saatiyle senkronize eder / ayarlar.
    /// </summary>
    public bool SetDeviceTime(DateTime newTime)
    {
        if (!isConnected) return false;

        if (isSimulator)
        {
            return true;
        }

        if (axCZKEM == null) return false;

        return axCZKEM.SetDeviceTime2(MachineNumber, newTime.Year, newTime.Month, newTime.Day, newTime.Hour, newTime.Minute, newTime.Second);
    }

    public void GetAttendanceLog(Action<string, string, string, string> onLogRead)
    {
        if (!isConnected) return;

        if (isSimulator)
        {
            FetchSimulatorAttendance(onLogRead);
            return;
        }

        if (axCZKEM == null) return;

        axCZKEM.EnableDevice(MachineNumber, false);
        int logCount = 0;
        try
        {
            if (axCZKEM.ReadGeneralLogData(MachineNumber))
            {
                string sEnrollNumber = "";
                int dwVerifyMode = 0;
                int dwInOutMode = 0;
                int dwYear = 0, dwMonth = 0, dwDay = 0, dwHour = 0, dwMinute = 0, dwSecond = 0;
                int dwWorkCode = 0;

                while (axCZKEM.SSR_GetGeneralLogData(MachineNumber, out sEnrollNumber, out dwVerifyMode, out dwInOutMode, 
                       out dwYear, out dwMonth, out dwDay, out dwHour, out dwMinute, out dwSecond, ref dwWorkCode))
                {
                    string logTime = $"{dwYear}-{dwMonth:D2}-{dwDay:D2} {dwHour:D2}:{dwMinute:D2}:{dwSecond:D2}";
                    string inOutStr = (dwInOutMode == 0) ? "Giriş" : (dwInOutMode == 1 ? "Çıkış" : dwInOutMode.ToString());
                    string verifyStr = FormatVerifyMode(dwVerifyMode);

                    onLogRead?.Invoke(sEnrollNumber, inOutStr, logTime, verifyStr);
                    logCount++;
                }

                if (logCount == 0)
                {
                    int dwMachineNumber = 0;
                    int dwEnrollNumber = 0;

                    while (axCZKEM.GetGeneralLogData(MachineNumber, ref dwMachineNumber, ref dwEnrollNumber, ref dwVerifyMode, 
                           ref dwInOutMode, ref dwYear, ref dwMonth, ref dwDay, ref dwHour, ref dwMinute, ref dwSecond))
                    {
                        string logTime = $"{dwYear}-{dwMonth:D2}-{dwDay:D2} {dwHour:D2}:{dwMinute:D2}:{dwSecond:D2}";
                        string inOutStr = (dwInOutMode == 0) ? "Giriş" : (dwInOutMode == 1 ? "Çıkış" : dwInOutMode.ToString());
                        string verifyStr = FormatVerifyMode(dwVerifyMode);

                        onLogRead?.Invoke(dwEnrollNumber.ToString(), inOutStr, logTime, verifyStr);
                        logCount++;
                    }
                }
            }
        }
        finally
        {
            axCZKEM.EnableDevice(MachineNumber, true);
        }
    }

    public void GetAllUserInfo(Action<string, string, string> onUserRead)
    {
        if (!isConnected) return;

        if (isSimulator)
        {
            FetchSimulatorUsers(onUserRead);
            return;
        }

        if (axCZKEM == null) return;

        axCZKEM.EnableDevice(MachineNumber, false);
        int userCount = 0;
        try
        {
            if (axCZKEM.ReadAllUserID(MachineNumber))
            {
                string sEnrollNumber = "";
                string sName = "";
                string sPassword = "";
                int iPrivilege = 0;
                bool bEnabled = false;

                while (axCZKEM.SSR_GetAllUserInfo(MachineNumber, out sEnrollNumber, out sName, out sPassword, out iPrivilege, out bEnabled))
                {
                    string sCardnumber = "";
                    axCZKEM.GetStrCardNumber(out sCardnumber);
                    onUserRead?.Invoke(sEnrollNumber, sName, sCardnumber);
                    userCount++;
                }

                if (userCount == 0)
                {
                    int dwEnrollNumber = 0;
                    string name = "";
                    string password = "";

                    while (axCZKEM.GetAllUserInfo(MachineNumber, ref dwEnrollNumber, ref name, ref password, ref iPrivilege, ref bEnabled))
                    {
                        string card = "";
                        axCZKEM.GetStrCardNumber(out card);
                        onUserRead?.Invoke(dwEnrollNumber.ToString(), name, card);
                        userCount++;
                    }
                }
            }
        }
        finally
        {
            axCZKEM.EnableDevice(MachineNumber, true);
        }
    }

    private string FormatVerifyMode(int mode)
    {
        switch (mode)
        {
            case 1: return "Parmak İzi";
            case 2: return "PIN / Şifre";
            case 4: return "Kart (RFID)";
            case 15: return "Yüz Tanıma";
            default: return "Şifre / Diğer (" + mode + ")";
        }
    }

    private void FetchSimulatorAttendance(Action<string, string, string, string> onLogRead)
    {
        try
        {
            string json = FetchHttp("http://127.0.0.1:3030/api/attendance");
            var regex = new Regex(@"\{[^{}]*""deviceUserId""\s*:\s*""([^""]+)""[^{}]*""verifyType""\s*:\s*(\d+)[^{}]*""state""\s*:\s*(\d+)[^{}]*""time""\s*:\s*""([^""]+)""[^{}]*\}");
            var matches = regex.Matches(json);

            foreach (Match m in matches)
            {
                string userId = m.Groups[1].Value;
                int verify = int.Parse(m.Groups[2].Value);
                int state = int.Parse(m.Groups[3].Value);
                string timeStr = m.Groups[4].Value;

                DateTime dt;
                if (DateTime.TryParse(timeStr, out dt))
                {
                    timeStr = dt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                }

                string inOut = (state == 0) ? "Giriş" : (state == 1 ? "Çıkış" : state.ToString());
                string verifyStr = FormatVerifyMode(verify);
                onLogRead?.Invoke(userId, inOut, timeStr, verifyStr);
            }
        }
        catch (Exception ex)
        {
            ZkPdksApp.Logger.LogException("FetchSimulatorAttendance", ex);
        }
    }

    private void FetchSimulatorUsers(Action<string, string, string> onUserRead)
    {
        try
        {
            string json = FetchHttp("http://127.0.0.1:3030/api/users");
            var regex = new Regex(@"\{[^{}]*""name""\s*:\s*""([^""]+)""[^{}]*""userId""\s*:\s*""([^""]+)""[^{}]*\}");
            var matches = regex.Matches(json);

            foreach (Match m in matches)
            {
                string name = m.Groups[1].Value;
                string userId = m.Groups[2].Value;
                string card = "0";
                var cardMatch = Regex.Match(m.Value, @"""card""\s*:\s*(\d+)");
                if (cardMatch.Success) card = cardMatch.Groups[1].Value;

                onUserRead?.Invoke(userId, name, card);
            }
        }
        catch (Exception ex)
        {
            ZkPdksApp.Logger.LogException("FetchSimulatorUsers", ex);
        }
    }

    private string FetchHttp(string url)
    {
        using (var wc = new WebClient())
        {
            wc.Encoding = System.Text.Encoding.UTF8;
            return wc.DownloadString(url);
        }
    }
}
