using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ClosedXML.Excel;
using Microsoft.Win32;

namespace ZkPdksApp
{
    public class AttendanceLogModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string InOutMode { get; set; }
        public string LogTime { get; set; }
        public string VerifyMode { get; set; }
    }

    public partial class MainWindow : Window
    {
        private ZkService zkService;
        private DatabaseService dbService;
        public ObservableCollection<AttendanceLogModel> LogList { get; set; }
        public ObservableCollection<ShiftAttendanceResult> ReportList { get; set; }
        private List<Employee> currentEmployees = new List<Employee>();
        private List<ShiftRule> currentShifts = new List<ShiftRule>();
        private DispatcherTimer rtcTimer;

        public MainWindow()
        {
            Logger.Log("MainWindow ctor: Başladı.");
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                Logger.LogException("MainWindow InitializeComponent", ex);
            }

            try
            {
                LogList = new ObservableCollection<AttendanceLogModel>();
                ReportList = new ObservableCollection<ShiftAttendanceResult>();

                if (dgLogs != null) dgLogs.ItemsSource = LogList;
                if (dgMonthlyReport != null) dgMonthlyReport.ItemsSource = ReportList;

                dbService = new DatabaseService();
                zkService = new ZkService();

                InitDateComboboxes();
                RefreshAllData();

                // RTC saat takip timer'ı
                rtcTimer = new DispatcherTimer();
                rtcTimer.Interval = TimeSpan.FromSeconds(10);
                rtcTimer.Tick += (s, e) => UpdateDeviceTimeDisplay();
                rtcTimer.Start();

                Logger.Log("MainWindow ctor: Tamamlandı.");
            }
            catch (Exception ex)
            {
                Logger.LogException("MainWindow ctor Body", ex);
            }
        }

        private void InitDateComboboxes()
        {
            int currentYear = DateTime.Now.Year;
            for (int y = currentYear - 2; y <= currentYear + 1; y++)
            {
                cbReportYear.Items.Add(y);
            }
            cbReportYear.SelectedItem = currentYear;

            string[] months = {
                "1 - Ocak", "2 - Şubat", "3 - Mart", "4 - Nisan", "5 - Mayıs", "6 - Haziran",
                "7 - Temmuz", "8 - Ağustos", "9 - Eylül", "10 - Ekim", "11 - Kasım", "12 - Aralık"
            };

            foreach (var m in months)
            {
                cbReportMonth.Items.Add(m);
            }
            cbReportMonth.SelectedIndex = DateTime.Now.Month - 1;
        }

        private void RefreshAllData()
        {
            try
            {
                currentEmployees = dbService.GetAllEmployees();
                currentShifts = dbService.GetAllShifts();

                if (txtEmpCount != null) txtEmpCount.Text = $"{currentEmployees.Count} Kişi";

                var logs = dbService.GetAllLogs();
                LogList.Clear();
                foreach (var log in logs)
                {
                    LogList.Add(log);
                }

                CalculateShiftReport();

                if (txtStatus != null) txtStatus.Text = $"{logs.Count} Geçiş Kaydı";
            }
            catch (Exception ex)
            {
                Logger.LogException("RefreshAllData", ex);
            }
        }

        private void CalculateShiftReport()
        {
            if (cbReportYear.SelectedItem == null || cbReportMonth.SelectedIndex < 0) return;

            int year = (int)cbReportYear.SelectedItem;
            int month = cbReportMonth.SelectedIndex + 1;

            var items = AttendanceCalculator.CalculateAdvancedShiftReport(LogList, currentEmployees, currentShifts, year, month);
            ReportList.Clear();
            foreach (var item in items)
            {
                ReportList.Add(item);
            }
        }

        private void UpdateDeviceTimeDisplay()
        {
            if (zkService != null && zkService.IsConnected)
            {
                var dt = zkService.GetDeviceTime();
                if (dt.HasValue)
                {
                    txtDeviceTime.Text = dt.Value.ToString("yyyy-MM-dd HH:mm:ss");
                    TimeSpan diff = DateTime.Now - dt.Value;
                    if (Math.Abs(diff.TotalMinutes) > 2)
                    {
                        txtDeviceTime.Foreground = System.Windows.Media.Brushes.IndianRed;
                        txtDeviceTime.ToolTip = $"Cihaz saati güncel değil! ({Math.Round(diff.TotalMinutes)} dk fark)";
                    }
                    else
                    {
                        txtDeviceTime.Foreground = System.Windows.Media.Brushes.LightGreen;
                        txtDeviceTime.ToolTip = "Cihaz saati güncel";
                    }
                }
            }
            else
            {
                txtDeviceTime.Text = "Bağlantı Yok";
                txtDeviceTime.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private void BtnSyncTime_Click(object sender, RoutedEventArgs e)
        {
            if (!zkService.IsConnected)
            {
                MessageBox.Show("Cihaz bağlı değil.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool ok = zkService.SetDeviceTime(DateTime.Now);
            if (ok)
            {
                UpdateDeviceTimeDisplay();
                MessageBox.Show("Cihaz saati başarıyla bilgisayar saatiyle eşitlendi!", "Senkronizasyon", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Saat eşitlenemedi.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCalculateReport_Click(object sender, RoutedEventArgs e)
        {
            CalculateShiftReport();
            MessageBox.Show($"Gece/Gündüz vardiya ve mesai raporu hesaplandı: {ReportList.Count} satır bulundu.", "Mesai Raporu", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            string ip = txtIpAddress.Text.Trim();
            if (!int.TryParse(txtPort.Text.Trim(), out int port)) port = 4370;

            try
            {
                bool success = zkService.Connect(ip, port);
                if (success)
                {
                    txtConnectionStatus.Text = "Bağlandı (Aktif)";
                    txtConnectionStatus.Foreground = System.Windows.Media.Brushes.LightGreen;
                    UpdateDeviceTimeDisplay();
                    MessageBox.Show("ZKTeco cihazına başarıyla bağlanıldı!", "Bağlantı Kuruldu", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    txtConnectionStatus.Text = "Bağlanamadı";
                    txtConnectionStatus.Foreground = System.Windows.Media.Brushes.IndianRed;
                    MessageBox.Show("Cihaza bağlanılamadı. IP ve ağ erişiminizi kontrol edin.", "Bağlantı Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException("BtnConnect_Click", ex);
                MessageBox.Show(ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnGetLogs_Click(object sender, RoutedEventArgs e)
        {
            if (!zkService.IsConnected)
            {
                MessageBox.Show("Lütfen önce cihaza bağlanın.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            txtStatus.Text = "Loglar Okunuyor...";
            var fetchedList = new List<AttendanceLogModel>();

            try
            {
                await Task.Run(() =>
                {
                    zkService.GetAttendanceLog((userId, inOut, time, verify) =>
                    {
                        var item = new AttendanceLogModel
                        {
                            UserId = userId,
                            InOutMode = inOut,
                            LogTime = time,
                            VerifyMode = verify
                        };
                        fetchedList.Add(item);
                    });

                    dbService.SaveLogsBulk(fetchedList);
                });

                RefreshAllData();
                MessageBox.Show($"{fetchedList.Count} adet geçiş logu çekildi, 12 öncesi/sonrası mesai kuralları hesaplandı.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Logger.LogException("BtnGetLogs_Click", ex);
                txtStatus.Text = "Log Hatası";
                MessageBox.Show("Loglar alınırken hata: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnLoadDbLogs_Click(object sender, RoutedEventArgs e)
        {
            RefreshAllData();
        }

        private void BtnEmployees_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var empWin = new EmployeeWindow(dbService, zkService);
                empWin.Owner = this;
                empWin.ShowDialog();
                RefreshAllData();
            }
            catch (Exception ex)
            {
                Logger.LogException("BtnEmployees_Click", ex);
            }
        }

        private void BtnShifts_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var shiftWin = new ShiftWindow(dbService);
                shiftWin.Owner = this;
                shiftWin.ShowDialog();
                RefreshAllData();
            }
            catch (Exception ex)
            {
                Logger.LogException("BtnShifts_Click", ex);
            }
        }

        private void BtnExportMonthlyReport_Click(object sender, RoutedEventArgs e)
        {
            if (ReportList == null || ReportList.Count == 0)
            {
                MessageBox.Show("Dışa aktarılacak kayıt bulunmuyor.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int year = (int)cbReportYear.SelectedItem;
            int month = cbReportMonth.SelectedIndex + 1;

            var sfd = new SaveFileDialog
            {
                Filter = "Excel Dosyası (*.xlsx)|*.xlsx",
                FileName = $"ZKTeco_Gece_Gunduz_Mesai_Raporu_{year}_{month:D2}.xlsx"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Mesai ve Gece Ayrımı");

                        worksheet.Cell(1, 1).Value = "Personel ID";
                        worksheet.Cell(1, 2).Value = "Ad Soyad";
                        worksheet.Cell(1, 3).Value = "Departman";
                        worksheet.Cell(1, 4).Value = "Vardiya";
                        worksheet.Cell(1, 5).Value = "Tarih";
                        worksheet.Cell(1, 6).Value = "İlk Giriş";
                        worksheet.Cell(1, 7).Value = "Son Çıkış";
                        
                        // Gece 12 ayrımı sütunları
                        worksheet.Cell(1, 8).Value = "12 Öncesi (Saat)";
                        worksheet.Cell(1, 9).Value = "12 Sonrası Gece (Saat)";
                        
                        worksheet.Cell(1, 10).Value = "Net Toplam (Saat)";
                        worksheet.Cell(1, 11).Value = "Onaylanan Mesai (Saat)";
                        worksheet.Cell(1, 12).Value = "İptal/Aşan Mesai (Saat)";
                        worksheet.Cell(1, 13).Value = "Geç Kalma (dk)";
                        worksheet.Cell(1, 14).Value = "Durum";

                        var headerRow = worksheet.Row(1);
                        headerRow.Style.Font.Bold = true;
                        headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#1B5E20");
                        headerRow.Style.Font.FontColor = XLColor.White;

                        int row = 2;
                        foreach (var item in ReportList)
                        {
                            worksheet.Cell(row, 1).Value = item.UserId;
                            worksheet.Cell(row, 2).Value = item.EmployeeName;
                            worksheet.Cell(row, 3).Value = item.Department;
                            worksheet.Cell(row, 4).Value = item.ShiftName;
                            worksheet.Cell(row, 5).Value = item.DateString;
                            worksheet.Cell(row, 6).Value = item.ActualFirstIn;
                            worksheet.Cell(row, 7).Value = item.ActualLastOut;
                            worksheet.Cell(row, 8).Value = item.DayWorkHours;
                            worksheet.Cell(row, 9).Value = item.NightWorkHours;
                            worksheet.Cell(row, 10).Value = item.NetWorkHours;
                            worksheet.Cell(row, 11).Value = item.ApprovedOvertime;
                            worksheet.Cell(row, 12).Value = item.RejectedOvertime;
                            worksheet.Cell(row, 13).Value = item.LateMinutes;
                            worksheet.Cell(row, 14).Value = item.Status;

                            if (item.NightWorkHours > 0)
                            {
                                // Gece 12'den sonraki çalışma hücrelerini belirginleştir (koyu mavi ton)
                                worksheet.Cell(row, 9).Style.Font.FontColor = XLColor.FromHtml("#0D47A1");
                                worksheet.Cell(row, 9).Style.Font.Bold = true;
                            }

                            if (item.ApprovedOvertime > 0)
                            {
                                worksheet.Cell(row, 11).Style.Font.FontColor = XLColor.FromHtml("#1B5E20");
                                worksheet.Cell(row, 11).Style.Font.Bold = true;
                            }

                            if (item.RejectedOvertime > 0)
                            {
                                worksheet.Cell(row, 12).Style.Font.FontColor = XLColor.FromHtml("#C62828");
                                worksheet.Cell(row, 12).Style.Font.Bold = true;
                            }

                            row++;
                        }

                        worksheet.Columns().AdjustToContents();
                        workbook.SaveAs(sfd.FileName);
                    }

                    MessageBox.Show("Gece/Gündüz ayrılmış detaylı mesai raporu Excel olarak kaydedildi!\n" + sfd.FileName, "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    Logger.LogException("BtnExportMonthlyReport_Click", ex);
                    MessageBox.Show("Excel oluşturma hatası: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            if (LogList == null || LogList.Count == 0)
            {
                MessageBox.Show("Dışa aktarılacak kayıt bulunmuyor.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var sfd = new SaveFileDialog
            {
                Filter = "Excel Dosyası (*.xlsx)|*.xlsx",
                FileName = $"ZKTeco_Tum_Gecisler_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Geçiş Kayıtları");

                        worksheet.Cell(1, 1).Value = "Personel ID";
                        worksheet.Cell(1, 2).Value = "Personel Adı";
                        worksheet.Cell(1, 3).Value = "İşlem Tipi";
                        worksheet.Cell(1, 4).Value = "Tarih / Saat";
                        worksheet.Cell(1, 5).Value = "Doğrulama Türü";

                        var headerRow = worksheet.Row(1);
                        headerRow.Style.Font.Bold = true;
                        headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#1565C0");
                        headerRow.Style.Font.FontColor = XLColor.White;

                        int row = 2;
                        foreach (var log in LogList)
                        {
                            worksheet.Cell(row, 1).Value = log.UserId;
                            worksheet.Cell(row, 2).Value = log.UserName;
                            worksheet.Cell(row, 3).Value = log.InOutMode;
                            worksheet.Cell(row, 4).Value = log.LogTime;
                            worksheet.Cell(row, 5).Value = log.VerifyMode;
                            row++;
                        }

                        worksheet.Columns().AdjustToContents();
                        workbook.SaveAs(sfd.FileName);
                    }

                    MessageBox.Show("Excel raporu kaydedildi!\n" + sfd.FileName, "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    Logger.LogException("BtnExportExcel_Click", ex);
                    MessageBox.Show("Excel hatası: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
