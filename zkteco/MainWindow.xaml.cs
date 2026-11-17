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
        public ObservableCollection<ShiftAttendanceResult> SingleEmpDailyList { get; set; }
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
                SingleEmpDailyList = new ObservableCollection<ShiftAttendanceResult>();

                if (dgLogs != null) dgLogs.ItemsSource = LogList;
                if (dgMonthlyReport != null) dgMonthlyReport.ItemsSource = ReportList;
                if (dgEmpDailyDetails != null) dgEmpDailyDetails.ItemsSource = SingleEmpDailyList;

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

            LoadGroupFilter();
        }

        private void LoadGroupFilter()
        {
            if (cbReportGroup == null) return;
            string selected = cbReportGroup.SelectedItem?.ToString();
            cbReportGroup.Items.Clear();
            cbReportGroup.Items.Add("Tüm Gruplar");
            
            var groups = dbService.GetAllGroupNames();
            foreach (var g in groups)
            {
                if (!cbReportGroup.Items.Contains(g))
                    cbReportGroup.Items.Add(g);
            }

            if (!string.IsNullOrEmpty(selected) && cbReportGroup.Items.Contains(selected))
                cbReportGroup.SelectedItem = selected;
            else
                cbReportGroup.SelectedIndex = 0;
        }

        private void RefreshAllData()
        {
            try
            {
                currentEmployees = dbService.GetAllEmployees();
                currentShifts = dbService.GetAllShifts();
                LoadGroupFilter();

                // Personel Kartı Seçim Kutusunu Doldur
                if (cbSelectedEmployeeCard != null)
                {
                    string prevSelectedBadge = cbSelectedEmployeeCard.SelectedValue?.ToString();
                    cbSelectedEmployeeCard.ItemsSource = null;
                    cbSelectedEmployeeCard.ItemsSource = currentEmployees;
                    if (!string.IsNullOrEmpty(prevSelectedBadge))
                    {
                        cbSelectedEmployeeCard.SelectedValue = prevSelectedBadge;
                    }
                    else if (currentEmployees.Count > 0)
                    {
                        cbSelectedEmployeeCard.SelectedIndex = 0;
                    }
                }

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

            var shiftsToUse = currentShifts;

            string selectedGroup = cbReportGroup?.SelectedItem?.ToString() ?? "Tüm Gruplar";
            var filteredEmployees = currentEmployees;
            if (!string.IsNullOrEmpty(selectedGroup) && selectedGroup != "Tüm Gruplar")
            {
                filteredEmployees = currentEmployees.Where(e => (e.GroupName ?? "Genel") == selectedGroup).ToList();
            }

            // Eğer kullanıcı ana ekrandan "Özel Saat Uygula" seçeneğini işaretlediyse
            if (chkOverrideShift.IsChecked == true)
            {
                if (TimeSpan.TryParse(txtQuickStart.Text.Trim(), out TimeSpan customStart) &&
                    TimeSpan.TryParse(txtQuickEnd.Text.Trim(), out TimeSpan customEnd))
                {
                    int.TryParse(txtQuickTolerance.Text.Trim(), out int customTol);
                    var customRule = new ShiftRule
                    {
                        Id = 9999,
                        Name = $"Özel ({customStart:hh\\:mm} - {customEnd:hh\\:mm})",
                        StartTime = customStart,
                        EndTime = customEnd,
                        GracePeriodMinutes = customTol > 0 ? customTol : 10,
                        LunchBreakMinutes = 60,
                        DuplicateFilterSeconds = 60,
                        RequiredDailyWorkHours = Math.Max(0, (customEnd - customStart).TotalHours - 1.0),
                        MaxDailyOvertimeHours = 4.0,
                        MinOvertimeThresholdMinutes = 30.0,
                        OvertimeWindowStart = customEnd,
                        OvertimeWindowEnd = customEnd.Add(TimeSpan.FromHours(4))
                    };

                    // Seçilen gruptaki personelin bu özel vardiyaya göre hesaplanmasını sağla
                    var modifiedEmployees = filteredEmployees.Select(e => new Employee
                    {
                        Badgenumber = e.Badgenumber,
                        Name = e.Name,
                        Department = e.Department,
                        GroupName = e.GroupName,
                        ShiftId = 9999
                    }).ToList();

                    var customShiftList = new List<ShiftRule> { customRule };
                    var customItems = AttendanceCalculator.CalculateAdvancedShiftReport(LogList, modifiedEmployees, customShiftList, year, month);
                    
                    ReportList.Clear();
                    foreach (var item in customItems)
                    {
                        ReportList.Add(item);
                    }
                    UpdateSummaryCards();
                    UpdateSingleEmployeeDetailsView();
                    return;
                }
                else
                {
                    MessageBox.Show("Giriş ve çıkış saatleri geçerli saat formatında olmalıdır (Örn: 08:30 ve 18:00).", "Geçersiz Saat", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            var items = AttendanceCalculator.CalculateAdvancedShiftReport(LogList, filteredEmployees, currentShifts, year, month);
            ReportList.Clear();
            foreach (var item in items)
            {
                ReportList.Add(item);
            }
            UpdateSummaryCards();
            UpdateSingleEmployeeDetailsView();
        }

        private void UpdateSummaryCards()
        {
            try
            {
                double totalOvertime = Math.Round(ReportList.Sum(r => r.ApprovedOvertime), 2);
                
                // Cumartesi günleri yapılan çalışma toplamı
                double satHours = Math.Round(ReportList
                    .Where(r => DateTime.TryParse(r.DateString, out DateTime d) && d.DayOfWeek == DayOfWeek.Saturday)
                    .Sum(r => r.NetWorkHours), 2);

                // Pazar günleri ve resmi tatiller yapılan çalışma toplamı
                double sunHours = Math.Round(ReportList
                    .Where(r => (DateTime.TryParse(r.DateString, out DateTime d) && d.DayOfWeek == DayOfWeek.Sunday) || r.DayType == "Resmi Tatil")
                    .Sum(r => r.NetWorkHours), 2);

                if (txtKpiTotalOvertime != null) txtKpiTotalOvertime.Text = $"{totalOvertime:F2} Saat";
                if (txtKpiSaturdayHours != null) txtKpiSaturdayHours.Text = $"{satHours:F2} Saat";
                if (txtKpiSundayHours != null) txtKpiSundayHours.Text = $"{sunHours:F2} Saat";
            }
            catch (Exception ex)
            {
                Logger.LogException("UpdateSummaryCards", ex);
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

        private async void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            string ip = txtIpAddress.Text.Trim();
            if (!int.TryParse(txtPort.Text.Trim(), out int port)) port = 4370;

            txtConnectionStatus.Text = "Bağlanıyor...";
            txtConnectionStatus.Foreground = System.Windows.Media.Brushes.Orange;

            try
            {
                bool success = await Task.Run(() => zkService.Connect(ip, port));
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
                txtConnectionStatus.Text = "Hata Oluştu";
                txtConnectionStatus.Foreground = System.Windows.Media.Brushes.IndianRed;
                Logger.LogException("BtnConnect_Click", ex);
                MessageBox.Show("Bağlantı sırasında hata oluştu:\n" + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void BtnGroupManagement_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var groupWin = new GroupManagementWindow(dbService);
                groupWin.Owner = this;
                groupWin.ShowDialog();
                RefreshAllData();
            }
            catch (Exception ex)
            {
                Logger.LogException("BtnGroupManagement_Click", ex);
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
            try
            {
                if (ReportList == null || ReportList.Count == 0)
                {
                    MessageBox.Show("Dışa aktarılacak kayıt bulunmuyor.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int year = (cbReportYear != null && cbReportYear.SelectedItem != null) 
                    ? (int)cbReportYear.SelectedItem 
                    : DateTime.Now.Year;
                int month = (cbReportMonth != null && cbReportMonth.SelectedIndex >= 0) 
                    ? cbReportMonth.SelectedIndex + 1 
                    : DateTime.Now.Month;

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
                            worksheet.Cell(1, 3).Value = "Grup";
                            worksheet.Cell(1, 4).Value = "İstihdam Türü";
                            worksheet.Cell(1, 5).Value = "Departman";
                            worksheet.Cell(1, 6).Value = "Vardiya";
                            worksheet.Cell(1, 7).Value = "Tarih";
                            worksheet.Cell(1, 8).Value = "Gün";
                            worksheet.Cell(1, 9).Value = "İlk Giriş";
                            worksheet.Cell(1, 10).Value = "Son Çıkış";
                            
                            // Gece 12 ayrımı sütunları
                            worksheet.Cell(1, 11).Value = "12 Öncesi Çal.(Saat)";
                            worksheet.Cell(1, 12).Value = "12 Sonrası Gece Çal.(Saat)";
                            worksheet.Cell(1, 13).Value = "Fiili Mola (dk)";
                            worksheet.Cell(1, 14).Value = "Mola Aşımı (dk)";
                            worksheet.Cell(1, 15).Value = "Net Toplam (Saat)";
                            
                            // 3'LÜ MESAİ AYRIMI: Sabah, Akşam, Gece
                            worksheet.Cell(1, 16).Value = "Sabah Mesaisi (Saat)";
                            worksheet.Cell(1, 17).Value = "Akşam Mesaisi (Saat)";
                            worksheet.Cell(1, 18).Value = "Gece Mesaisi (Saat)";

                            worksheet.Cell(1, 19).Value = "Ham Mesai (Saat)";
                            worksheet.Cell(1, 20).Value = "Çarpan";
                            worksheet.Cell(1, 21).Value = "Hakediş Mesai (Saat)";
                            worksheet.Cell(1, 22).Value = "İptal/Aşan Mesai (Saat)";
                            worksheet.Cell(1, 23).Value = "Geç Kalma (dk)";
                            worksheet.Cell(1, 24).Value = "Eksik Çalışma (Saat)";
                            worksheet.Cell(1, 25).Value = "Mesai / Devamsızlık Durumu";

                            var headerRow = worksheet.Row(1);
                            headerRow.Style.Font.Bold = true;
                            headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#1B5E20");
                            headerRow.Style.Font.FontColor = XLColor.White;

                            int row = 2;
                            foreach (var item in ReportList)
                            {
                                worksheet.Cell(row, 1).Value = item.UserId ?? "";
                                worksheet.Cell(row, 2).Value = item.EmployeeName ?? "";
                                worksheet.Cell(row, 3).Value = item.GroupName ?? "Genel";
                                worksheet.Cell(row, 4).Value = item.EmploymentType ?? "Kadrolu";
                                worksheet.Cell(row, 5).Value = item.Department ?? "";
                                worksheet.Cell(row, 6).Value = item.ShiftName ?? "";
                                worksheet.Cell(row, 7).Value = item.DateString ?? "";
                                worksheet.Cell(row, 8).Value = item.DayType ?? "Hafta İçi";
                                worksheet.Cell(row, 9).Value = item.ActualFirstIn ?? "";
                                worksheet.Cell(row, 10).Value = item.ActualLastOut ?? "";
                                worksheet.Cell(row, 11).Value = item.DayWorkHours;
                                worksheet.Cell(row, 12).Value = item.NightWorkHours;
                                worksheet.Cell(row, 13).Value = item.ActualBreakMinutes;
                                worksheet.Cell(row, 14).Value = item.ExcessBreakMinutes;
                                worksheet.Cell(row, 15).Value = item.NetWorkHours;
                                
                                // Sabah, Akşam, Gece Mesaileri
                                worksheet.Cell(row, 16).Value = item.MorningOvertimeHours;
                                worksheet.Cell(row, 17).Value = item.EveningOvertimeHours;
                                worksheet.Cell(row, 18).Value = item.NightShiftOvertimeHours;

                                worksheet.Cell(row, 19).Value = item.ApprovedOvertime;
                                worksheet.Cell(row, 20).Value = item.OvertimeMultiplier;
                                worksheet.Cell(row, 21).Value = item.WeightedOvertimeHours;
                                worksheet.Cell(row, 22).Value = item.RejectedOvertime;
                                worksheet.Cell(row, 23).Value = item.LateMinutes;
                                worksheet.Cell(row, 24).Value = item.MissingWorkHours;
                                worksheet.Cell(row, 25).Value = item.Status ?? "";

                                if (item.NightWorkHours > 0 || item.NightShiftOvertimeHours > 0)
                                {
                                    worksheet.Cell(row, 12).Style.Font.FontColor = XLColor.FromHtml("#0D47A1");
                                    worksheet.Cell(row, 12).Style.Font.Bold = true;
                                    worksheet.Cell(row, 18).Style.Font.FontColor = XLColor.FromHtml("#0D47A1");
                                    worksheet.Cell(row, 18).Style.Font.Bold = true;
                                }

                                if (item.MorningOvertimeHours > 0)
                                {
                                    worksheet.Cell(row, 16).Style.Font.FontColor = XLColor.FromHtml("#D97706");
                                    worksheet.Cell(row, 16).Style.Font.Bold = true;
                                }

                                if (item.ApprovedOvertime > 0)
                                {
                                    worksheet.Cell(row, 19).Style.Font.FontColor = XLColor.FromHtml("#1B5E20");
                                    worksheet.Cell(row, 21).Style.Font.FontColor = XLColor.FromHtml("#1B5E20");
                                    worksheet.Cell(row, 21).Style.Font.Bold = true;
                                }

                                if (item.ExcessBreakMinutes > 0)
                                {
                                    worksheet.Cell(row, 14).Style.Font.FontColor = XLColor.FromHtml("#DC2626");
                                    worksheet.Cell(row, 14).Style.Font.Bold = true;
                                }

                                if (item.RejectedOvertime > 0)
                                {
                                    worksheet.Cell(row, 22).Style.Font.FontColor = XLColor.FromHtml("#C62828");
                                    worksheet.Cell(row, 22).Style.Font.Bold = true;
                                }

                                if (item.LateMinutes > 0 || item.MissingWorkHours > 0)
                                {
                                    worksheet.Cell(row, 23).Style.Font.FontColor = XLColor.FromHtml("#E65100");
                                    worksheet.Cell(row, 24).Style.Font.FontColor = XLColor.FromHtml("#C62828");
                                    worksheet.Cell(row, 24).Style.Font.Bold = true;
                                }
                                    // Geç kalma ve eksik çalışma sürelerini turuncu/kırmızı vurgula
                                    worksheet.Cell(row, 20).Style.Font.FontColor = XLColor.FromHtml("#E65100");
                                    worksheet.Cell(row, 21).Style.Font.FontColor = XLColor.FromHtml("#C62828");
                                    worksheet.Cell(row, 21).Style.Font.Bold = true;
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
                        Logger.LogException("BtnExportMonthlyReport_Click Save", ex);
                        MessageBox.Show("Excel oluşturma hatası: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException("BtnExportMonthlyReport_Click Outer", ex);
                MessageBox.Show("Hata oluştu: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void CbSelectedEmployeeCard_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateSingleEmployeeDetailsView();
        }

        private void UpdateSingleEmployeeDetailsView()
        {
            try
            {
                if (cbSelectedEmployeeCard == null || cbSelectedEmployeeCard.SelectedItem == null) return;

                var emp = cbSelectedEmployeeCard.SelectedItem as Employee;
                if (emp == null) return;

                // Özlük Kartı Alanlarını Güncelle
                txtCardEmpName.Text = emp.Name;
                txtCardEmpBadge.Text = $"Sicil / Cihaz ID: {emp.Badgenumber}";
                txtCardTcNo.Text = string.IsNullOrEmpty(emp.TcNo) ? "-" : emp.TcNo;
                txtCardEmpType.Text = string.IsNullOrEmpty(emp.EmploymentType) ? "Kadrolu" : emp.EmploymentType;
                txtCardGroup.Text = string.IsNullOrEmpty(emp.GroupName) ? "Genel" : emp.GroupName;
                txtCardDept.Text = string.IsNullOrEmpty(emp.Department) ? "-" : emp.Department;
                txtCardTitle.Text = string.IsNullOrEmpty(emp.Title) ? "-" : emp.Title;
                txtCardCompanyBranch.Text = $"{emp.Company ?? "-"} / {emp.Branch ?? "-"}";
                txtCardRfid.Text = string.IsNullOrEmpty(emp.CardNo) ? "-" : emp.CardNo;
                txtCardPhone.Text = string.IsNullOrEmpty(emp.Phone) ? "-" : emp.Phone;
                txtCardStatus.Text = string.IsNullOrEmpty(emp.Status) ? "Aktif" : emp.Status;

                // Bu Personelin Seçili Aydaki Gün Gün Kayıtlarını Getir
                SingleEmpDailyList.Clear();
                var empRecords = ReportList.Where(r => r.UserId == emp.Badgenumber).OrderBy(r => r.DateString).ToList();
                foreach (var rec in empRecords)
                {
                    SingleEmpDailyList.Add(rec);
                }

                // İstatistikleri Hesapla
                int workedDays = empRecords.Count(r => r.NetWorkHours > 0);
                double totalNet = Math.Round(empRecords.Sum(r => r.NetWorkHours), 2);
                double totalDayOt = Math.Round(empRecords.Sum(r => r.DayOvertimeHours), 2);
                double totalNightOt = Math.Round(empRecords.Sum(r => r.NightOvertimeHours), 2);
                double totalWeightedOt = Math.Round(empRecords.Sum(r => r.WeightedOvertimeHours), 2);
                double totalMissing = Math.Round(empRecords.Sum(r => r.MissingWorkHours), 2);
                double totalExcessBreak = Math.Round(empRecords.Sum(r => r.ExcessBreakMinutes), 1);

                // Bu personele özel Hafta İçi, Cumartesi, Pazar/Tatil mesaisi
                double empWkdOt = Math.Round(empRecords
                    .Where(r => DateTime.TryParse(r.DateString, out DateTime d) && d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
                    .Sum(r => r.ApprovedOvertime), 2);
                double empSatOt = Math.Round(empRecords
                    .Where(r => DateTime.TryParse(r.DateString, out DateTime d) && d.DayOfWeek == DayOfWeek.Saturday)
                    .Sum(r => r.NetWorkHours), 2);
                double empSunOt = Math.Round(empRecords
                    .Where(r => (DateTime.TryParse(r.DateString, out DateTime d) && d.DayOfWeek == DayOfWeek.Sunday) || r.DayType == "Resmi Tatil")
                    .Sum(r => r.NetWorkHours), 2);

                // Sabah, Akşam, Gece Mesaileri
                double empMorningOt = Math.Round(empRecords.Sum(r => r.MorningOvertimeHours), 2);
                double empEveningOt = Math.Round(empRecords.Sum(r => r.EveningOvertimeHours), 2);
                double empNightShiftOt = Math.Round(empRecords.Sum(r => r.NightShiftOvertimeHours), 2);

                txtCardWorkedDays.Text = $"{workedDays} Gün";
                txtCardTotalNet.Text = $"{totalNet:F2} Saat";
                if (txtCardWeekdayOt != null) txtCardWeekdayOt.Text = $"{empWkdOt:F2} Saat";
                if (txtCardSaturdayOt != null) txtCardSaturdayOt.Text = $"{empSatOt:F2} Saat";
                if (txtCardSundayOt != null) txtCardSundayOt.Text = $"{empSunOt:F2} Saat";
                if (txtCardMorningOt != null) txtCardMorningOt.Text = $"{empMorningOt:F2} Saat";
                if (txtCardEveningOt != null) txtCardEveningOt.Text = $"{empEveningOt:F2} Saat";
                if (txtCardNightOt != null) txtCardNightOt.Text = $"{empNightShiftOt:F2} Saat";
                txtCardTotalDayOt.Text = $"{totalDayOt:F2} Saat";
                txtCardTotalNightOt.Text = $"{totalNightOt:F2} Saat";
                if (txtCardTotalExcessBreak != null) txtCardTotalExcessBreak.Text = $"{totalExcessBreak} dk";
                txtCardTotalWeightedOt.Text = $"{totalWeightedOt:F2} Saat";
                txtCardTotalMissing.Text = $"{totalMissing:F2} Saat";
            }
            catch (Exception ex)
            {
                Logger.LogException("UpdateSingleEmployeeDetailsView", ex);
            }
        }

        private void BtnExportSingleEmpExcel_Click(object sender, RoutedEventArgs e)
        {
            if (cbSelectedEmployeeCard == null || cbSelectedEmployeeCard.SelectedItem == null)
            {
                MessageBox.Show("Lütfen önce bir personel seçiniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var emp = cbSelectedEmployeeCard.SelectedItem as Employee;
            if (emp == null || SingleEmpDailyList.Count == 0)
            {
                MessageBox.Show("Seçilen personele ait gün gün çalışma kaydı bulunmuyor.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int year = (int)(cbReportYear.SelectedItem ?? DateTime.Now.Year);
            int month = cbReportMonth.SelectedIndex + 1;

            var sfd = new SaveFileDialog
            {
                Filter = "Excel Dosyası (*.xlsx)|*.xlsx",
                FileName = $"PDKS_Gunluk_Puantaj_{emp.Name.Replace(" ", "_")}_{year}_{month:D2}.xlsx"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Günlük Puantaj");

                        // Üst Bilgi Kartı Başlığı
                        worksheet.Cell(1, 1).Value = "PERSONEL DETAYLI GÜNLÜK ÇALIŞMA VE MESAİ KARTI";
                        var titleRange = worksheet.Range(1, 1, 1, 13);
                        titleRange.Merge();
                        titleRange.Style.Font.Bold = true;
                        titleRange.Style.Font.FontSize = 14;
                        titleRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#0F172A");
                        titleRange.Style.Font.FontColor = XLColor.White;

                        worksheet.Cell(2, 1).Value = "Ad Soyad:";
                        worksheet.Cell(2, 2).Value = emp.Name ?? "";
                        worksheet.Cell(2, 4).Value = "Sicil / ID:";
                        worksheet.Cell(2, 5).Value = emp.Badgenumber ?? "";
                        worksheet.Cell(2, 7).Value = "Statü / Tür:";
                        worksheet.Cell(2, 8).Value = emp.EmploymentType ?? "";

                        worksheet.Cell(3, 1).Value = "Grup / Dep.:";
                        worksheet.Cell(3, 2).Value = $"{emp.GroupName ?? ""} - {emp.Department ?? ""}";
                        worksheet.Cell(3, 4).Value = "Dönem:";
                        worksheet.Cell(3, 5).Value = $"{year} / {month:D2}";
                        worksheet.Cell(3, 7).Value = "Çalıştığı Gün:";
                        worksheet.Cell(3, 8).Value = $"{SingleEmpDailyList.Count(x => x.NetWorkHours > 0)} Gün";

                        for (int r = 2; r <= 3; r++)
                        {
                            worksheet.Row(r).Style.Font.Bold = true;
                            worksheet.Row(r).Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
                        }

                        // Tablo Başlıkları
                        int startRow = 5;
                        worksheet.Cell(startRow, 1).Value = "Tarih";
                        worksheet.Cell(startRow, 2).Value = "Gün";
                        worksheet.Cell(startRow, 3).Value = "İlk Giriş";
                        worksheet.Cell(startRow, 4).Value = "Son Çıkış";
                        worksheet.Cell(startRow, 5).Value = "12 Öncesi Çal.(s)";
                        worksheet.Cell(startRow, 6).Value = "12 Sonrası Gece Çal.(s)";
                        worksheet.Cell(startRow, 7).Value = "Fiili Mola (dk)";
                        worksheet.Cell(startRow, 8).Value = "Mola Aşımı (dk)";
                        worksheet.Cell(startRow, 9).Value = "Net Çalışma (s)";
                        worksheet.Cell(startRow, 10).Value = "Sabah Mesaisi (s)";
                        worksheet.Cell(startRow, 11).Value = "Akşam Mesaisi (s)";
                        worksheet.Cell(startRow, 12).Value = "Gece Mesaisi (s)";
                        worksheet.Cell(startRow, 13).Value = "Hakediş Mesai (s)";
                        worksheet.Cell(startRow, 14).Value = "Geç Kalma (dk)";
                        worksheet.Cell(startRow, 15).Value = "Eksik (s)";
                        worksheet.Cell(startRow, 16).Value = "Durum / Açıklama";

                        var headerRow = worksheet.Row(startRow);
                        headerRow.Style.Font.Bold = true;
                        headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A8A");
                        headerRow.Style.Font.FontColor = XLColor.White;

                        int currentRow = startRow + 1;
                        foreach (var dayRec in SingleEmpDailyList)
                        {
                            worksheet.Cell(currentRow, 1).Value = dayRec.DateString ?? "";
                            worksheet.Cell(currentRow, 2).Value = dayRec.DayType ?? "";
                            worksheet.Cell(currentRow, 3).Value = dayRec.ActualFirstIn ?? "";
                            worksheet.Cell(currentRow, 4).Value = dayRec.ActualLastOut ?? "";
                            worksheet.Cell(currentRow, 5).Value = dayRec.DayWorkHours;
                            worksheet.Cell(currentRow, 6).Value = dayRec.NightWorkHours;
                            worksheet.Cell(currentRow, 7).Value = dayRec.ActualBreakMinutes;
                            worksheet.Cell(currentRow, 8).Value = dayRec.ExcessBreakMinutes;
                            worksheet.Cell(currentRow, 9).Value = dayRec.NetWorkHours;
                            worksheet.Cell(currentRow, 10).Value = dayRec.MorningOvertimeHours;
                            worksheet.Cell(currentRow, 11).Value = dayRec.EveningOvertimeHours;
                            worksheet.Cell(currentRow, 12).Value = dayRec.NightShiftOvertimeHours;
                            worksheet.Cell(currentRow, 13).Value = dayRec.WeightedOvertimeHours;
                            worksheet.Cell(currentRow, 14).Value = dayRec.LateMinutes;
                            worksheet.Cell(currentRow, 15).Value = dayRec.MissingWorkHours;
                            worksheet.Cell(currentRow, 16).Value = dayRec.Status ?? "";

                            if (dayRec.NightShiftOvertimeHours > 0 || dayRec.NightWorkHours > 0)
                            {
                                worksheet.Cell(currentRow, 6).Style.Font.Bold = true;
                                worksheet.Cell(currentRow, 6).Style.Font.FontColor = XLColor.FromHtml("#0D47A1");
                                worksheet.Cell(currentRow, 12).Style.Font.Bold = true;
                                worksheet.Cell(currentRow, 12).Style.Font.FontColor = XLColor.FromHtml("#0D47A1");
                            }

                            if (dayRec.MorningOvertimeHours > 0)
                            {
                                worksheet.Cell(currentRow, 10).Style.Font.Bold = true;
                                worksheet.Cell(currentRow, 10).Style.Font.FontColor = XLColor.FromHtml("#D97706");
                            }

                            if (dayRec.ExcessBreakMinutes > 0)
                            {
                                worksheet.Cell(currentRow, 8).Style.Font.Bold = true;
                                worksheet.Cell(currentRow, 8).Style.Font.FontColor = XLColor.FromHtml("#DC2626");
                            }

                            if (dayRec.WeightedOvertimeHours > 0)
                            {
                                worksheet.Cell(currentRow, 13).Style.Font.Bold = true;
                                worksheet.Cell(currentRow, 13).Style.Font.FontColor = XLColor.FromHtml("#15803D");
                            }

                            currentRow++;
                        }

                        // Alt Toplam Satırı
                        worksheet.Cell(currentRow, 1).Value = "TOPLAM";
                        worksheet.Cell(currentRow, 5).Value = SingleEmpDailyList.Sum(x => x.DayWorkHours);
                        worksheet.Cell(currentRow, 6).Value = SingleEmpDailyList.Sum(x => x.NightWorkHours);
                        worksheet.Cell(currentRow, 7).Value = SingleEmpDailyList.Sum(x => x.ActualBreakMinutes);
                        worksheet.Cell(currentRow, 8).Value = SingleEmpDailyList.Sum(x => x.ExcessBreakMinutes);
                        worksheet.Cell(currentRow, 9).Value = SingleEmpDailyList.Sum(x => x.NetWorkHours);
                        worksheet.Cell(currentRow, 10).Value = SingleEmpDailyList.Sum(x => x.MorningOvertimeHours);
                        worksheet.Cell(currentRow, 11).Value = SingleEmpDailyList.Sum(x => x.EveningOvertimeHours);
                        worksheet.Cell(currentRow, 12).Value = SingleEmpDailyList.Sum(x => x.NightShiftOvertimeHours);
                        worksheet.Cell(currentRow, 13).Value = SingleEmpDailyList.Sum(x => x.WeightedOvertimeHours);
                        worksheet.Cell(currentRow, 14).Value = SingleEmpDailyList.Sum(x => x.LateMinutes);
                        worksheet.Cell(currentRow, 15).Value = SingleEmpDailyList.Sum(x => x.MissingWorkHours);

                        var sumRow = worksheet.Row(currentRow);
                        sumRow.Style.Font.Bold = true;
                        sumRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");

                        worksheet.Columns().AdjustToContents();
                        workbook.SaveAs(sfd.FileName);
                    }

                    MessageBox.Show($"'{emp.Name}' personeline ait gün gün çalışma ve mesai raporu Excel olarak kaydedildi!\n{sfd.FileName}", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    Logger.LogException("BtnExportSingleEmpExcel_Click", ex);
                    MessageBox.Show("Excel oluşturma hatası: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnExportDailyAttendance_Click(object sender, RoutedEventArgs e)
        {
            if (ReportList == null || ReportList.Count == 0)
            {
                MessageBox.Show("Dışa aktarılacak puantaj verisi bulunmuyor.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int year = (int)(cbReportYear.SelectedItem ?? DateTime.Now.Year);
            int month = cbReportMonth.SelectedIndex + 1;

            var sfd = new SaveFileDialog
            {
                Filter = "Excel Dosyası (*.xlsx)|*.xlsx",
                FileName = $"PDKS_Tum_Personel_Gun_Gun_Puantaj_{year}_{month:D2}.xlsx"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Günlük Puantaj");

                        worksheet.Cell(1, 1).Value = "Tarih";
                        worksheet.Cell(1, 2).Value = "Gün";
                        worksheet.Cell(1, 3).Value = "Personel ID";
                        worksheet.Cell(1, 4).Value = "Ad Soyad";
                        worksheet.Cell(1, 5).Value = "Grup";
                        worksheet.Cell(1, 6).Value = "İstihdam Türü";
                        worksheet.Cell(1, 7).Value = "Vardiya";
                        worksheet.Cell(1, 8).Value = "İlk Giriş";
                        worksheet.Cell(1, 9).Value = "Son Çıkış";
                        worksheet.Cell(1, 10).Value = "12 Öncesi Çal.(s)";
                        worksheet.Cell(1, 11).Value = "12 Sonrası Gece Çal.(s)";
                        worksheet.Cell(1, 12).Value = "Fiili Mola (dk)";
                        worksheet.Cell(1, 13).Value = "Mola Aşımı (dk)";
                        worksheet.Cell(1, 14).Value = "Net Çalışma (s)";
                        worksheet.Cell(1, 15).Value = "Sabah Mesaisi (s)";
                        worksheet.Cell(1, 16).Value = "Akşam Mesaisi (s)";
                        worksheet.Cell(1, 17).Value = "Gece Mesaisi (s)";
                        worksheet.Cell(1, 18).Value = "Hakediş Mesai (s)";
                        worksheet.Cell(1, 19).Value = "Geç Kalma (dk)";
                        worksheet.Cell(1, 20).Value = "Eksik (s)";
                        worksheet.Cell(1, 21).Value = "Puantaj Durumu";

                        var headerRow = worksheet.Row(1);
                        headerRow.Style.Font.Bold = true;
                        headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#0F766E");
                        headerRow.Style.Font.FontColor = XLColor.White;

                        int row = 2;
                        foreach (var item in ReportList.OrderBy(x => x.DateString).ThenBy(x => x.EmployeeName))
                        {
                            worksheet.Cell(row, 1).Value = item.DateString ?? "";
                            worksheet.Cell(row, 2).Value = item.DayType ?? "";
                            worksheet.Cell(row, 3).Value = item.UserId ?? "";
                            worksheet.Cell(row, 4).Value = item.EmployeeName ?? "";
                            worksheet.Cell(row, 5).Value = item.GroupName ?? "Genel";
                            worksheet.Cell(row, 6).Value = item.EmploymentType ?? "Kadrolu";
                            worksheet.Cell(row, 7).Value = item.ShiftName ?? "";
                            worksheet.Cell(row, 8).Value = item.ActualFirstIn ?? "";
                            worksheet.Cell(row, 9).Value = item.ActualLastOut ?? "";
                            worksheet.Cell(row, 10).Value = item.DayWorkHours;
                            worksheet.Cell(row, 11).Value = item.NightWorkHours;
                            worksheet.Cell(row, 12).Value = item.ActualBreakMinutes;
                            worksheet.Cell(row, 13).Value = item.ExcessBreakMinutes;
                            worksheet.Cell(row, 14).Value = item.NetWorkHours;
                            worksheet.Cell(row, 15).Value = item.MorningOvertimeHours;
                            worksheet.Cell(row, 16).Value = item.EveningOvertimeHours;
                            worksheet.Cell(row, 17).Value = item.NightShiftOvertimeHours;
                            worksheet.Cell(row, 18).Value = item.WeightedOvertimeHours;
                            worksheet.Cell(row, 19).Value = item.LateMinutes;
                            worksheet.Cell(row, 20).Value = item.MissingWorkHours;
                            worksheet.Cell(row, 21).Value = item.Status ?? "";

                            if (item.NightWorkHours > 0 || item.NightShiftOvertimeHours > 0)
                            {
                                worksheet.Cell(row, 11).Style.Font.Bold = true;
                                worksheet.Cell(row, 11).Style.Font.FontColor = XLColor.FromHtml("#0D47A1");
                                worksheet.Cell(row, 17).Style.Font.Bold = true;
                                worksheet.Cell(row, 17).Style.Font.FontColor = XLColor.FromHtml("#0D47A1");
                            }

                            if (item.MorningOvertimeHours > 0)
                            {
                                worksheet.Cell(row, 15).Style.Font.Bold = true;
                                worksheet.Cell(row, 15).Style.Font.FontColor = XLColor.FromHtml("#D97706");
                            }

                            if (item.ExcessBreakMinutes > 0)
                            {
                                worksheet.Cell(row, 13).Style.Font.Bold = true;
                                worksheet.Cell(row, 13).Style.Font.FontColor = XLColor.FromHtml("#DC2626");
                            }

                            if (item.WeightedOvertimeHours > 0)
                            {
                                worksheet.Cell(row, 18).Style.Font.Bold = true;
                                worksheet.Cell(row, 18).Style.Font.FontColor = XLColor.FromHtml("#15803D");
                            }

                            if (item.WeightedOvertimeHours > 0)
                            {
                                worksheet.Cell(row, 15).Style.Font.Bold = true;
                                worksheet.Cell(row, 15).Style.Font.FontColor = XLColor.FromHtml("#15803D");
                            }

                            row++;
                        }

                        worksheet.Columns().AdjustToContents();
                        workbook.SaveAs(sfd.FileName);
                    }

                    MessageBox.Show("Tüm personelin gün gün detaylı puantaj ve mesai raporu Excel olarak kaydedildi!\n" + sfd.FileName, "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    Logger.LogException("BtnExportDailyAttendance_Click", ex);
                    MessageBox.Show("Excel oluşturma hatası: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
