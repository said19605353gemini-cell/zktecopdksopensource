using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
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
        public ObservableCollection<AttendanceLogModel> FilteredLogList { get; set; }
        public ObservableCollection<ShiftAttendanceResult> ReportList { get; set; }
        public ObservableCollection<ShiftAttendanceResult> SingleEmpDailyList { get; set; }
        private List<Employee> currentEmployees = new List<Employee>();
        private List<ShiftRule> currentShifts = new List<ShiftRule>();
        private List<EmployeeShiftSchedule> currentShiftSchedules = new List<EmployeeShiftSchedule>();
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
                FilteredLogList = new ObservableCollection<AttendanceLogModel>();
                ReportList = new ObservableCollection<ShiftAttendanceResult>();
                SingleEmpDailyList = new ObservableCollection<ShiftAttendanceResult>();

                if (dgLogs != null) dgLogs.ItemsSource = FilteredLogList;
                if (dgMonthlyReport != null) dgMonthlyReport.ItemsSource = ReportList;
                if (dgEmpDailyDetails != null) dgEmpDailyDetails.ItemsSource = SingleEmpDailyList;

                dbService = new DatabaseService();
                zkService = new ZkService();

                InitDateComboboxes();
                SyncCalendarToLatestLogDate(false);
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

            // Yıl ve Ay Değişiminde Otomatik Rapor Hesaplama Handler'ı
            cbReportYear.SelectionChanged += CbReportDate_SelectionChanged;
            cbReportMonth.SelectionChanged += CbReportDate_SelectionChanged;

            // Varsayılan Başlangıç ve Bitiş Tarihleri (Bugün)
            if (dpStartDate != null) dpStartDate.SelectedDate = DateTime.Today;
            if (dpEndDate != null) dpEndDate.SelectedDate = DateTime.Today;

            // Eğer veritabanında daha önce kaydedilmiş loglar varsa takvimi son hareket tarihine göre güncelle
            SyncCalendarToLatestLogDate(false);

            LoadGroupFilter();
            LoadEmploymentTypeFilter();
        }

        public void SyncCalendarToLatestLogDate(bool triggerCalculation = true)
        {
            try
            {
                var latestDate = dbService.GetLatestLogDate();
                if (latestDate.HasValue)
                {
                    int logYear = latestDate.Value.Year;
                    int logMonth = latestDate.Value.Month;

                    if (!cbReportYear.Items.Contains(logYear))
                    {
                        cbReportYear.Items.Add(logYear);
                    }
                    cbReportYear.SelectedItem = logYear;
                    cbReportMonth.SelectedIndex = logMonth - 1;

                    if (triggerCalculation)
                    {
                        CalculateShiftReport();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException("SyncCalendarToLatestLogDate", ex);
            }
        }

        private void BtnSyncLatestPeriod_Click(object sender, RoutedEventArgs e)
        {
            var latestDate = dbService.GetLatestLogDate();
            if (latestDate.HasValue)
            {
                SyncCalendarToLatestLogDate(true);
                MessageBox.Show($"Takvim en son hareket verisi olan döneme ({latestDate.Value:yyyy / MMMM}) ayarlandı ve rapor hesaplandı.", 
                    "Dönem Güncellendi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Veritabanında henüz kayıtlı bir geçiş logu bulunmuyor.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void BtnSyncInternetTime_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                (DateTime Time, bool IsFromInternet, string ErrorMessage) timeResult = (DateTime.Now, false, null);

                await Task.Run(() =>
                {
                    timeResult = HolidayService.GetCurrentTurkeyDateTimeSafe();
                });

                DateTime targetTime = timeResult.Time;

                // 1. Takvim Yıl ve Ayını Eşitle
                int netYear = targetTime.Year;
                int netMonth = targetTime.Month;

                if (!cbReportYear.Items.Contains(netYear))
                {
                    cbReportYear.Items.Add(netYear);
                }
                cbReportYear.SelectedItem = netYear;
                cbReportMonth.SelectedIndex = netMonth - 1;

                // 2. Raporu ve Tatilleri Güncelle
                CalculateShiftReport();

                // 3. Eğer ZKTeco Cihazı Bağlıysa Cihazın Saatini de Eşitle
                string deviceSyncMsg = "";
                if (zkService != null && zkService.IsConnected)
                {
                    bool setOk = zkService.SetDeviceTime(targetTime);
                    UpdateDeviceTimeDisplay();
                    deviceSyncMsg = setOk 
                        ? "\n• ZKTeco cihaz saati de güncellendi."
                        : "\n• ZKTeco cihaz saati güncellenemedi.";
                }

                if (timeResult.IsFromInternet)
                {
                    MessageBox.Show($"Türkiye Canlı Tarih & Saati İnternetten Alındı:\n" +
                                    $"• Tarih / Saat: {targetTime:dd.MM.yyyy HH:mm:ss} (Türkiye UTC+3)\n" +
                                    $"• Takvim {targetTime:yyyy / MMMM} dönemine odaklandı.{deviceSyncMsg}", 
                                    "İnternet Saat ve Takvim Senkronizasyonu", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"DİKKAT: İnternet bağlantısı kurulamadı veya zaman sunucusu yanıt vermedi!\n\n" +
                                    $"Güvenlik amacıyla BİLGİSAYARINIZIN SİSTEM SAATİ KULLANILDI:\n" +
                                    $"• Bilgisayar Saati: {targetTime:dd.MM.yyyy HH:mm:ss}\n" +
                                    $"• Takvim ve cihaz saati bilgisayar saatine göre ayarlandı.{deviceSyncMsg}", 
                                    "İnternet Yok - Bilgisayar Saati Kullanıldı", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException("BtnSyncInternetTime_Click", ex);
                MessageBox.Show("Saat alınırken hata oluştu:\n" + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private async void BtnRefreshHolidays_Click(object sender, RoutedEventArgs e)
        {
            int year = (cbReportYear != null && cbReportYear.SelectedItem != null) 
                ? (int)cbReportYear.SelectedItem 
                : DateTime.Now.Year;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                Dictionary<string, string> holidays = null;

                await Task.Run(() =>
                {
                    holidays = HolidayService.GetHolidaysForYear(year, dbService, forceRefresh: true);
                });

                CalculateShiftReport();

                int count = holidays?.Count ?? 0;
                MessageBox.Show($"{year} yılı için Türkiye resmi tatilleri başarıyla internetten çekildi ve güncellendi ({count} tatil günü).", 
                    "Tatiller Güncellendi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Logger.LogException("BtnRefreshHolidays_Click", ex);
                MessageBox.Show("Resmi tatiller çekilirken bir hata oluştu:\n" + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void CbLanguage_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cbLanguage?.SelectedItem is System.Windows.Controls.ComboBoxItem item && item.Tag != null)
            {
                string lang = item.Tag.ToString();
                LocalizationService.CurrentLanguage = lang;
                ApplyLocalization();
            }
        }

        private void ApplyLocalization()
        {
            try
            {
                // Üst Bar Butonları
                if (btnConnect != null) btnConnect.Content = LocalizationService.Get("Connect");
                if (btnGetLogs != null) btnGetLogs.Content = LocalizationService.Get("GetLogs");
                if (btnSyncDeviceTime != null) btnSyncDeviceTime.Content = LocalizationService.Get("SyncDeviceTime");
                if (btnEmployees != null) btnEmployees.Content = LocalizationService.Get("Employees");
                if (btnGroups != null) btnGroups.Content = LocalizationService.Get("Groups");
                if (btnShifts != null) btnShifts.Content = LocalizationService.Get("Shifts");

                // Üst Başlıklar
                if (lblAppTitle != null) lblAppTitle.Text = LocalizationService.Get("AppTitle");
                if (lblAppSubtitle != null) lblAppSubtitle.Text = LocalizationService.Get("AppSubtitle");

                // KPI Kart Başlıkları
                if (lblKpiConnection != null) lblKpiConnection.Text = LocalizationService.Get("KpiConnection");
                if (lblKpiDeviceTime != null) lblKpiDeviceTime.Text = LocalizationService.Get("KpiDeviceTime");
                if (lblKpiEmpCount != null) lblKpiEmpCount.Text = LocalizationService.Get("KpiEmpCount");
                if (lblKpiTotalOt != null) lblKpiTotalOt.Text = LocalizationService.Get("KpiTotalOt");
                if (lblKpiSatHours != null) lblKpiSatHours.Text = LocalizationService.Get("KpiSatHours");
                if (lblKpiSunHours != null) lblKpiSunHours.Text = LocalizationService.Get("KpiSunHours");

                // Sekme Başlıkları
                if (tabReport != null) tabReport.Header = LocalizationService.Get("TabReport");
                if (tabLogs != null) tabLogs.Header = LocalizationService.Get("TabLogs");
                if (tabEmpPanel != null) tabEmpPanel.Header = LocalizationService.Get("TabEmpPanel");

                // Filtre Etiketleri
                if (lblYear != null) lblYear.Text = LocalizationService.Get("Year");
                if (lblMonth != null) lblMonth.Text = LocalizationService.Get("Month");
                if (lblDay != null) lblDay.Text = LocalizationService.Get("Day");
                if (lblGroup != null) lblGroup.Text = LocalizationService.Get("Group");
                if (lblType != null) lblType.Text = LocalizationService.Get("Type");
                if (lblStatus != null) lblStatus.Text = LocalizationService.Get("Status");

                // Buton Metinleri
                if (txtBtnLatestPeriod != null) txtBtnLatestPeriod.Text = LocalizationService.Get("BtnLatestPeriod");
                if (txtBtnInternetTime != null) txtBtnInternetTime.Text = LocalizationService.Get("BtnInternetTime");
                if (txtBtnUpdateHolidays != null) txtBtnUpdateHolidays.Text = LocalizationService.Get("BtnUpdateHolidays");
                if (btnCalculate != null) btnCalculate.Content = LocalizationService.Get("BtnCalculate");
                if (txtBtnClearFilters != null) txtBtnClearFilters.Text = LocalizationService.Get("BtnClearFilters");
                if (btnExportMonthly != null) btnExportMonthly.Content = LocalizationService.Get("BtnExportMonthly");
                if (btnExportDaily != null) btnExportDaily.Content = LocalizationService.Get("BtnExportDaily");
                if (btnRefreshDb != null) btnRefreshDb.Content = LocalizationService.Get("BtnRefreshDb");
                if (btnExportAllLogs != null) btnExportAllLogs.Content = LocalizationService.Get("BtnExportAllLogs");
                if (btnExportEmpDetail != null) btnExportEmpDetail.Content = LocalizationService.Get("BtnExportEmpDetail");

                // Personel Kartı Etiketleri
                if (lblInspectEmp != null) lblInspectEmp.Text = LocalizationService.Get("InspectEmp");
                if (lblEmpSummaryTitle != null) lblEmpSummaryTitle.Text = LocalizationService.Get("EmpSummaryTitle");
                if (lblWorkedDays != null) lblWorkedDays.Text = LocalizationService.Get("WorkedDays");
                if (lblTotalNet != null) lblTotalNet.Text = LocalizationService.Get("TotalNet");
                if (lblWeekdayOt != null) lblWeekdayOt.Text = LocalizationService.Get("WeekdayOt");
                if (lblSaturdayOt != null) lblSaturdayOt.Text = LocalizationService.Get("SaturdayOt");
                if (lblSundayOt != null) lblSundayOt.Text = LocalizationService.Get("SundayOt");
                if (lblMorningOt != null) lblMorningOt.Text = LocalizationService.Get("MorningOt");
                if (lblEveningOt != null) lblEveningOt.Text = LocalizationService.Get("EveningOt");
                if (lblNightOt != null) lblNightOt.Text = LocalizationService.Get("NightOt");
                if (lblExcessBreak != null) lblExcessBreak.Text = LocalizationService.Get("ExcessBreak");
                if (lblWeightedOt != null) lblWeightedOt.Text = LocalizationService.Get("WeightedOt");
                if (lblMissingWork != null) lblMissingWork.Text = LocalizationService.Get("MissingWork");

                // Arama Kutuları Hint
                if (txtSearchName != null) MaterialDesignThemes.Wpf.HintAssist.SetHint(txtSearchName, LocalizationService.Get("SearchName"));
                if (txtSearchTc != null) MaterialDesignThemes.Wpf.HintAssist.SetHint(txtSearchTc, LocalizationService.Get("SearchTc"));

                // Gün Tipi ComboBox Öğelerini Güncelle
                if (cbReportDayFilter != null && cbReportDayFilter.Items.Count >= 7)
                {
                    ((System.Windows.Controls.ComboBoxItem)cbReportDayFilter.Items[0]).Content = LocalizationService.Get("DayFilter_All");
                    ((System.Windows.Controls.ComboBoxItem)cbReportDayFilter.Items[1]).Content = LocalizationService.Get("DayFilter_Holidays");
                    ((System.Windows.Controls.ComboBoxItem)cbReportDayFilter.Items[2]).Content = LocalizationService.Get("DayFilter_HolidaysWeekend");
                    ((System.Windows.Controls.ComboBoxItem)cbReportDayFilter.Items[3]).Content = LocalizationService.Get("DayFilter_Weekend");
                    ((System.Windows.Controls.ComboBoxItem)cbReportDayFilter.Items[4]).Content = LocalizationService.Get("DayFilter_Sat");
                    ((System.Windows.Controls.ComboBoxItem)cbReportDayFilter.Items[5]).Content = LocalizationService.Get("DayFilter_Sun");
                    ((System.Windows.Controls.ComboBoxItem)cbReportDayFilter.Items[6]).Content = LocalizationService.Get("DayFilter_Weekdays");
                }

                // Statü ComboBox Öğelerini Güncelle
                if (cbReportStatusFilter != null && cbReportStatusFilter.Items.Count >= 4)
                {
                    ((System.Windows.Controls.ComboBoxItem)cbReportStatusFilter.Items[0]).Content = LocalizationService.Get("Status_All");
                    ((System.Windows.Controls.ComboBoxItem)cbReportStatusFilter.Items[1]).Content = LocalizationService.Get("Status_Active");
                    ((System.Windows.Controls.ComboBoxItem)cbReportStatusFilter.Items[2]).Content = LocalizationService.Get("Status_Passive");
                    ((System.Windows.Controls.ComboBoxItem)cbReportStatusFilter.Items[3]).Content = LocalizationService.Get("Status_OnLeave");
                }
            }
            catch (Exception ex)
            {
                Logger.LogException("ApplyLocalization", ex);
            }
        }

        private void CbReportDate_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            CalculateShiftReport();
        }

        private void ChkUseDateRange_Changed(object sender, RoutedEventArgs e)
        {
            CalculateShiftReport();
        }

        private void DpDateRange_SelectedDateChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (chkUseDateRange != null && chkUseDateRange.IsChecked == true)
            {
                CalculateShiftReport();
            }
        }

        private void BtnTodayFilter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dpStartDate != null) dpStartDate.SelectedDate = DateTime.Today;
                if (dpEndDate != null) dpEndDate.SelectedDate = DateTime.Today;
                if (chkUseDateRange != null) chkUseDateRange.IsChecked = true;
                CalculateShiftReport();
            }
            catch (Exception ex)
            {
                Logger.LogException("BtnTodayFilter_Click", ex);
            }
        }

        private void BtnClearFilters_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Arama kutularını temizle
                if (txtSearchName != null) txtSearchName.Text = string.Empty;
                if (txtSearchTc != null) txtSearchTc.Text = string.Empty;

                // 2. ComboBox filtrelerini varsayılan ilk elemana ("Tümü") çek
                if (cbReportDayFilter != null) cbReportDayFilter.SelectedIndex = 0; // Tüm Günler
                if (cbReportGroup != null) cbReportGroup.SelectedIndex = 0;         // Tüm Gruplar
                if (cbReportEmploymentTypeFilter != null) cbReportEmploymentTypeFilter.SelectedIndex = 0; // Tüm Türler
                if (cbReportStatusFilter != null) cbReportStatusFilter.SelectedIndex = 0; // Tümü (Tüm Personel)

                // 3. Özel Tarih Aralığı Filtresini Kapat
                if (chkUseDateRange != null) chkUseDateRange.IsChecked = false;

                // 4. Raporu ve kartları sıfırlanmış filtrelerle tekrar hesapla
                CalculateShiftReport();
            }
            catch (Exception ex)
            {
                Logger.LogException("BtnClearFilters_Click", ex);
            }
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

        private void LoadEmploymentTypeFilter()
        {
            if (cbReportEmploymentTypeFilter == null) return;
            string selected = cbReportEmploymentTypeFilter.SelectedItem?.ToString();
            cbReportEmploymentTypeFilter.Items.Clear();
            cbReportEmploymentTypeFilter.Items.Add("Tüm Türler");

            var types = dbService.GetAllEmploymentTypes();
            foreach (var t in types)
            {
                if (!cbReportEmploymentTypeFilter.Items.Contains(t))
                    cbReportEmploymentTypeFilter.Items.Add(t);
            }

            if (!string.IsNullOrEmpty(selected) && cbReportEmploymentTypeFilter.Items.Contains(selected))
                cbReportEmploymentTypeFilter.SelectedItem = selected;
            else
                cbReportEmploymentTypeFilter.SelectedIndex = 0;
        }

        private void RefreshAllData()
        {
            try
            {
                currentEmployees = dbService.GetAllEmployees();
                currentShifts = dbService.GetAllShifts();
                currentShiftSchedules = dbService.GetAllShiftSchedules();
                LoadGroupFilter();
                LoadEmploymentTypeFilter();

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

                ApplyLogFilters();
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
            string selectedEmpType = cbReportEmploymentTypeFilter?.SelectedItem?.ToString() ?? "Tüm Türler";
            var filteredEmployees = currentEmployees;

            // 1. Grup Filtresi (Örn: Üretim, Montaj, Yönetim)
            if (!string.IsNullOrEmpty(selectedGroup) && selectedGroup != "Tüm Gruplar")
            {
                filteredEmployees = filteredEmployees.Where(e => (e.GroupName ?? "Genel").Trim().Equals(selectedGroup.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // 2. İstihdam Türü Filtresi (Örn: Kadrolu, Taşeron, Sözleşmeli)
            if (!string.IsNullOrEmpty(selectedEmpType) && selectedEmpType != "Tüm Türler")
            {
                filteredEmployees = filteredEmployees.Where(e => (e.EmploymentType ?? "Kadrolu").Trim().Equals(selectedEmpType.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // 3. Personel Durumu / Statü Filtresi (Aktif, Pasif, İzinli)
            if (cbReportStatusFilter != null && cbReportStatusFilter.SelectedIndex > 0)
            {
                int statusIdx = cbReportStatusFilter.SelectedIndex;
                if (statusIdx == 1) // Sadece Aktif Olanlar
                {
                    filteredEmployees = filteredEmployees.Where(e => string.IsNullOrEmpty(e.Status) || e.Status.Trim().Equals("Aktif", StringComparison.OrdinalIgnoreCase)).ToList();
                }
                else if (statusIdx == 2) // Pasif (Ayrılanlar)
                {
                    filteredEmployees = filteredEmployees.Where(e => !string.IsNullOrEmpty(e.Status) && (e.Status.IndexOf("Pasif", StringComparison.OrdinalIgnoreCase) >= 0 || e.Status.IndexOf("Ayrıldı", StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
                }
                else if (statusIdx == 3) // İzinli Olanlar
                {
                    filteredEmployees = filteredEmployees.Where(e => !string.IsNullOrEmpty(e.Status) && e.Status.IndexOf("İzin", StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                }
            }

            // 4. Ad Soyad Arama Filtresi
            string searchName = txtSearchName?.Text?.Trim();
            if (!string.IsNullOrEmpty(searchName))
            {
                filteredEmployees = filteredEmployees.Where(e => !string.IsNullOrEmpty(e.Name) && e.Name.IndexOf(searchName, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            }

            // 5. T.C. Kimlik No Arama Filtresi
            string searchTc = txtSearchTc?.Text?.Trim();
            if (!string.IsNullOrEmpty(searchTc))
            {
                filteredEmployees = filteredEmployees.Where(e => !string.IsNullOrEmpty(e.TcNo) && e.TcNo.IndexOf(searchTc, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            }

            // Eğer filtrelenen grupta/türde hiçbir personel bulunamadıysa (Örn: Yönetim grubu henüz boşsa)
            if (filteredEmployees.Count == 0)
            {
                ReportList.Clear();
                UpdateSummaryCards();
                UpdateSingleEmployeeDetailsView();
                return;
            }

            // 6. Gün Tipi Filtresi (Tüm Günler, Sadece Resmi Tatiller, Resmi Tatil & Hafta Sonu, Cumartesi & Pazar, Sadece Cumartesi, Sadece Pazar, Sadece Hafta İçi)
            int dayFilterIdx = cbReportDayFilter != null ? cbReportDayFilter.SelectedIndex : 0;

            var holidays = HolidayService.GetHolidaysForYear(year, dbService);

            Func<ShiftAttendanceResult, bool> dayMatches = r =>
            {
                if (dayFilterIdx <= 0) return true; // Tüm Günler
                bool isHoliday = !string.IsNullOrEmpty(r.DayType) && r.DayType.StartsWith("Resmi Tatil");
                if (DateTime.TryParse(r.DateString, out DateTime dt))
                {
                    if (dayFilterIdx == 1) // Sadece Resmi Tatiller (Resmi tatilde kim çalışmış?)
                        return isHoliday;
                    if (dayFilterIdx == 2) // Resmi Tatil & Hafta Sonu
                        return isHoliday || dt.DayOfWeek == DayOfWeek.Saturday || dt.DayOfWeek == DayOfWeek.Sunday;
                    if (dayFilterIdx == 3) // Cumartesi & Pazar (Hafta Sonu)
                        return dt.DayOfWeek == DayOfWeek.Saturday || dt.DayOfWeek == DayOfWeek.Sunday;
                    if (dayFilterIdx == 4) // Sadece Cumartesi
                        return dt.DayOfWeek == DayOfWeek.Saturday;
                    if (dayFilterIdx == 5) // Sadece Pazar
                        return dt.DayOfWeek == DayOfWeek.Sunday;
                    if (dayFilterIdx == 6) // Sadece Hafta İçi (Normal Günler)
                        return !isHoliday && dt.DayOfWeek != DayOfWeek.Saturday && dt.DayOfWeek != DayOfWeek.Sunday;
                }
                return true;
            };

            DateTime? rangeStart = null;
            DateTime? rangeEnd = null;

            if (chkUseDateRange != null && chkUseDateRange.IsChecked == true)
            {
                rangeStart = dpStartDate?.SelectedDate ?? new DateTime(year, month, 1);
                rangeEnd = dpEndDate?.SelectedDate ?? rangeStart.Value.AddDays(1);
            }

            var items = AttendanceCalculator.CalculateAdvancedShiftReport(LogList, filteredEmployees, currentShifts, year, month, holidays, rangeStart, rangeEnd, currentShiftSchedules);
            ReportList.Clear();
            foreach (var item in items.Where(dayMatches))
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
                    .Where(r => (DateTime.TryParse(r.DateString, out DateTime d) && d.DayOfWeek == DayOfWeek.Sunday) || 
                                (!string.IsNullOrEmpty(r.DayType) && r.DayType.StartsWith("Resmi Tatil")))
                    .Sum(r => r.NetWorkHours), 2);

                if (txtKpiTotalOvertime != null) txtKpiTotalOvertime.Text = ShiftAttendanceResult.FormatHoursToHourMinute(totalOvertime);
                if (txtKpiSaturdayHours != null) txtKpiSaturdayHours.Text = ShiftAttendanceResult.FormatHoursToHourMinute(satHours);
                if (txtKpiSundayHours != null) txtKpiSundayHours.Text = ShiftAttendanceResult.FormatHoursToHourMinute(sunHours);
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

        private async void BtnSyncDeviceInternetTime_Click(object sender, RoutedEventArgs e)
        {
            if (!zkService.IsConnected)
            {
                MessageBox.Show("Cihaz bağlı değil. Lütfen önce cihaz IP ve portunu kontrol edip 'Cihaza Bağlan' butonuna basınız.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                (DateTime Time, bool IsFromInternet, string ErrorMessage) timeResult = (DateTime.Now, false, null);

                await Task.Run(() =>
                {
                    timeResult = HolidayService.GetCurrentTurkeyDateTimeSafe();
                });

                DateTime targetTime = timeResult.Time;
                bool ok = zkService.SetDeviceTime(targetTime);
                UpdateDeviceTimeDisplay();

                if (ok)
                {
                    if (timeResult.IsFromInternet)
                    {
                        MessageBox.Show($"Cihazın tarihi ve saati Türkiye canlı internet verisine göre başarıyla güncellendi!\n\n" +
                                        $"• Ayarlanan Tarih / Saat: {targetTime:dd.MM.yyyy HH:mm:ss} (Türkiye UTC+3)\n" +
                                        $"• Kaynak: Canlı İnternet Zaman Sunucusu\n" +
                                        $"• Donanım RTC Çipi: Senkronize edildi.",
                                        "Cihaz Saati & Tarihi Güncellendi", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show($"UYARI: İnternet bağlantısı yok veya zaman sunucusu verisi bozuk!\n\n" +
                                        $"Cihaz BİLGİSAYARINIZIN SİSTEM SAATİNE göre güncellendi:\n" +
                                        $"• Ayarlanan Tarih / Saat: {targetTime:dd.MM.yyyy HH:mm:ss} (Bilgisayar Saati)\n" +
                                        $"• Bilgisayarınızın tarih/saatinin doğru olduğundan emin olunuz.",
                                        "İnternet Yok - Bilgisayar Saatiyle Güncellendi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("Cihaza saat ve tarih güncelleme komutu gönderildi fakat onay alınamadı. Cihaz bağlantısını kontrol edin.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException("BtnSyncDeviceInternetTime_Click", ex);
                MessageBox.Show("Cihaz güncellenirken hata oluştu:\n" + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private async void BtnSyncTime_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(() => BtnSyncDeviceInternetTime_Click(sender, e));
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

                SyncCalendarToLatestLogDate(false);
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

        private void ApplyLogFilters()
        {
            if (FilteredLogList == null) return;
            FilteredLogList.Clear();

            string search = txtLogSearchUser?.Text?.Trim();
            string selectedType = cbLogTypeFilter?.SelectedItem is System.Windows.Controls.ComboBoxItem typeItem ? typeItem.Content?.ToString() : null;
            string selectedVerify = cbLogVerifyFilter?.SelectedItem is System.Windows.Controls.ComboBoxItem verifyItem ? verifyItem.Content?.ToString() : null;
            DateTime? dateFilter = dpLogDateFilter?.SelectedDate;

            var query = LogList.AsEnumerable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(l => (!string.IsNullOrEmpty(l.UserId) && l.UserId.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                         (!string.IsNullOrEmpty(l.UserName) && l.UserName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0));
            }

            if (!string.IsNullOrEmpty(selectedType) && selectedType != "Tüm İşlemler")
            {
                query = query.Where(l => !string.IsNullOrEmpty(l.InOutMode) && l.InOutMode.IndexOf(selectedType, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            if (!string.IsNullOrEmpty(selectedVerify) && selectedVerify != "Tümü (Tüm Doğrulama)")
            {
                query = query.Where(l => !string.IsNullOrEmpty(l.VerifyMode) && l.VerifyMode.IndexOf(selectedVerify, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            if (dateFilter.HasValue)
            {
                string dtStr = dateFilter.Value.ToString("yyyy-MM-dd");
                query = query.Where(l => !string.IsNullOrEmpty(l.LogTime) && l.LogTime.StartsWith(dtStr));
            }

            int count = 0;
            foreach (var log in query)
            {
                FilteredLogList.Add(log);
                count++;
            }

            if (txtFilteredLogCount != null)
            {
                txtFilteredLogCount.Text = $"{count} Basım Kaydı";
            }
        }

        private void TxtLogSearch_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ApplyLogFilters();
        }

        private void CbLogFilter_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ApplyLogFilters();
        }

        private void DpLogDateFilter_SelectedDateChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ApplyLogFilters();
        }

        private void BtnClearLogDate_Click(object sender, RoutedEventArgs e)
        {
            if (dpLogDateFilter != null) dpLogDateFilter.SelectedDate = null;
            ApplyLogFilters();
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
                SyncCalendarToLatestLogDate(false);
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
                SyncCalendarToLatestLogDate(false);
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
                SyncCalendarToLatestLogDate(false);
                RefreshAllData();
            }
            catch (Exception ex)
            {
                Logger.LogException("BtnShifts_Click", ex);
            }
        }

        private void BtnShiftSchedule_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var scheduleWin = new ShiftScheduleWindow(dbService);
                scheduleWin.Owner = this;
                scheduleWin.ShowDialog();
                RefreshAllData();
            }
            catch (Exception ex)
            {
                Logger.LogException("BtnShiftSchedule_Click", ex);
                MessageBox.Show("Vardiya Çizelgesi açılırken hata: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnIrregularPunches_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var irregularWin = new IrregularPunchesWindow(ReportList);
                irregularWin.Owner = this;
                irregularWin.ShowDialog();
            }
            catch (Exception ex)
            {
                Logger.LogException("BtnIrregularPunches_Click", ex);
                MessageBox.Show("Düzensiz Basımlar ekranı açılırken hata: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
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
                            worksheet.Cell(1, 3).Value = "T.C. Kimlik No";
                            worksheet.Cell(1, 4).Value = "Durum / Statü";
                            worksheet.Cell(1, 5).Value = "Grup";
                            worksheet.Cell(1, 6).Value = "İstihdam Türü";
                            worksheet.Cell(1, 7).Value = "İşe Giriş (Aktif Tarihi)";
                            worksheet.Cell(1, 8).Value = "Ayrılış (Pasif Tarihi)";
                            worksheet.Cell(1, 9).Value = "Departman";
                            worksheet.Cell(1, 10).Value = "Birim";
                            worksheet.Cell(1, 11).Value = "Unvan";
                            worksheet.Cell(1, 12).Value = "Şirket / Şube";
                            worksheet.Cell(1, 13).Value = "Kart (RFID) No";
                            worksheet.Cell(1, 14).Value = "Şahsi Telefon";
                            worksheet.Cell(1, 15).Value = "Firma Telefonu";
                            worksheet.Cell(1, 16).Value = "Vardiya";
                            worksheet.Cell(1, 17).Value = "Tarih";
                            worksheet.Cell(1, 18).Value = "Gün";
                            worksheet.Cell(1, 19).Value = "İlk Giriş";
                            worksheet.Cell(1, 20).Value = "Son Çıkış";
                            
                            // Gece 12 ayrımı sütunları
                            worksheet.Cell(1, 21).Value = "12 Öncesi Çal.(Saat)";
                            worksheet.Cell(1, 22).Value = "12 Sonrası Gece Çal.(Saat)";
                            worksheet.Cell(1, 23).Value = "Fiili Mola (dk)";
                            worksheet.Cell(1, 24).Value = "Mola Aşımı (dk)";
                            worksheet.Cell(1, 25).Value = "Net Toplam (Saat)";
                            
                            // 3'LÜ MESAİ AYRIMI: Sabah, Akşam, Gece
                            worksheet.Cell(1, 26).Value = "Sabah Mesaisi (Saat)";
                            worksheet.Cell(1, 27).Value = "Akşam Mesaisi (Saat)";
                            worksheet.Cell(1, 28).Value = "Gece Mesaisi (Saat)";

                            worksheet.Cell(1, 29).Value = "Ham Mesai (Saat)";
                            worksheet.Cell(1, 30).Value = "Çarpan";
                            worksheet.Cell(1, 31).Value = "Hakediş Mesai (Saat)";
                            worksheet.Cell(1, 32).Value = "İptal/Aşan Mesai (Saat)";
                            worksheet.Cell(1, 33).Value = "Geç Kalma (dk)";
                            worksheet.Cell(1, 34).Value = "Eksik Çalışma (Saat)";
                            worksheet.Cell(1, 35).Value = "Mesai / Devamsızlık Durumu";

                            var headerRow = worksheet.Row(1);
                            headerRow.Style.Font.Bold = true;
                            headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#1B5E20");
                            headerRow.Style.Font.FontColor = XLColor.White;

                            int row = 2;
                            foreach (var item in ReportList)
                            {
                                worksheet.Cell(row, 1).Value = item.UserId ?? "";
                                worksheet.Cell(row, 2).Value = item.EmployeeName ?? "";
                                worksheet.Cell(row, 3).Value = item.TcNo ?? "";
                                worksheet.Cell(row, 4).Value = item.EmployeeStatus ?? "Aktif";
                                worksheet.Cell(row, 5).Value = item.GroupName ?? "Genel";
                                worksheet.Cell(row, 6).Value = item.EmploymentType ?? "Kadrolu";
                                worksheet.Cell(row, 7).Value = item.HireDate ?? "";
                                worksheet.Cell(row, 8).Value = item.LeaveDate ?? "";
                                worksheet.Cell(row, 9).Value = item.Department ?? "";
                                worksheet.Cell(row, 10).Value = item.Unit ?? "";
                                worksheet.Cell(row, 11).Value = item.Title ?? "";
                                worksheet.Cell(row, 12).Value = !string.IsNullOrEmpty(item.Company) ? $"{item.Company} / {item.Branch ?? ""}" : "";
                                worksheet.Cell(row, 13).Value = item.CardNo ?? "";
                                worksheet.Cell(row, 14).Value = item.Phone ?? "";
                                worksheet.Cell(row, 15).Value = item.CompanyPhone ?? "";
                                worksheet.Cell(row, 16).Value = item.ShiftName ?? "";
                                worksheet.Cell(row, 17).Value = item.DateString ?? "";
                                worksheet.Cell(row, 18).Value = item.DayType ?? "Hafta İçi";
                                worksheet.Cell(row, 19).Value = item.ActualFirstIn ?? "";
                                worksheet.Cell(row, 20).Value = item.ActualLastOut ?? "";
                                worksheet.Cell(row, 21).Value = item.DayWorkHours;
                                worksheet.Cell(row, 22).Value = item.NightWorkHours;
                                worksheet.Cell(row, 23).Value = item.ActualBreakMinutes;
                                worksheet.Cell(row, 24).Value = item.ExcessBreakMinutes;
                                worksheet.Cell(row, 25).Value = item.NetWorkHours;
                                
                                // Sabah, Akşam, Gece Mesaileri
                                worksheet.Cell(row, 26).Value = item.MorningOvertimeHours;
                                worksheet.Cell(row, 27).Value = item.EveningOvertimeHours;
                                worksheet.Cell(row, 28).Value = item.NightShiftOvertimeHours;

                                worksheet.Cell(row, 29).Value = item.ApprovedOvertime;
                                worksheet.Cell(row, 30).Value = item.OvertimeMultiplier;
                                worksheet.Cell(row, 31).Value = item.WeightedOvertimeHours;
                                worksheet.Cell(row, 32).Value = item.RejectedOvertime;
                                worksheet.Cell(row, 33).Value = item.LateMinutes;
                                worksheet.Cell(row, 34).Value = item.MissingWorkHours;
                                worksheet.Cell(row, 35).Value = item.Status ?? "";

                                if (item.NightWorkHours > 0 || item.NightShiftOvertimeHours > 0)
                                {
                                    worksheet.Cell(row, 22).Style.Font.FontColor = XLColor.FromHtml("#0D47A1");
                                    worksheet.Cell(row, 22).Style.Font.Bold = true;
                                    worksheet.Cell(row, 28).Style.Font.FontColor = XLColor.FromHtml("#0D47A1");
                                    worksheet.Cell(row, 28).Style.Font.Bold = true;
                                }

                                if (item.MorningOvertimeHours > 0)
                                {
                                    worksheet.Cell(row, 26).Style.Font.FontColor = XLColor.FromHtml("#D97706");
                                    worksheet.Cell(row, 26).Style.Font.Bold = true;
                                }

                                if (item.ApprovedOvertime > 0)
                                {
                                    worksheet.Cell(row, 29).Style.Font.FontColor = XLColor.FromHtml("#1B5E20");
                                    worksheet.Cell(row, 31).Style.Font.FontColor = XLColor.FromHtml("#1B5E20");
                                    worksheet.Cell(row, 31).Style.Font.Bold = true;
                                }

                                if (item.ExcessBreakMinutes > 0)
                                {
                                    worksheet.Cell(row, 24).Style.Font.FontColor = XLColor.FromHtml("#DC2626");
                                    worksheet.Cell(row, 24).Style.Font.Bold = true;
                                }

                                if (item.RejectedOvertime > 0)
                                {
                                    worksheet.Cell(row, 32).Style.Font.FontColor = XLColor.FromHtml("#C62828");
                                    worksheet.Cell(row, 32).Style.Font.Bold = true;
                                }

                                if (item.LateMinutes > 0 || item.MissingWorkHours > 0)
                                {
                                    worksheet.Cell(row, 33).Style.Font.FontColor = XLColor.FromHtml("#E65100");
                                    worksheet.Cell(row, 34).Style.Font.FontColor = XLColor.FromHtml("#C62828");
                                    worksheet.Cell(row, 34).Style.Font.Bold = true;
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
                        foreach (var log in FilteredLogList)
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

        private void MainTabControl_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (e.Source is System.Windows.Controls.TabControl && tabEmpPanel != null && tabEmpPanel.IsSelected)
            {
                UpdateSingleEmployeeDetailsView();
            }
        }

        private void BtnRefreshEmpDetail_Click(object sender, RoutedEventArgs e)
        {
            UpdateSingleEmployeeDetailsView();
        }

        private void TxtCardSearch_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            FilterCardEmployeeList();
        }

        private void FilterCardEmployeeList()
        {
            try
            {
                if (cbSelectedEmployeeCard == null || currentEmployees == null) return;

                string searchName = txtCardSearchName?.Text?.Trim()?.ToLower() ?? "";
                string searchTc = txtCardSearchTc?.Text?.Trim()?.ToLower() ?? "";

                var filtered = currentEmployees.Where(emp =>
                {
                    bool matchName = string.IsNullOrEmpty(searchName) ||
                                     (!string.IsNullOrEmpty(emp.Name) && emp.Name.ToLower().Contains(searchName)) ||
                                     (!string.IsNullOrEmpty(emp.Badgenumber) && emp.Badgenumber.ToLower().Contains(searchName));

                    bool matchTc = string.IsNullOrEmpty(searchTc) ||
                                   (!string.IsNullOrEmpty(emp.TcNo) && emp.TcNo.ToLower().Contains(searchTc)) ||
                                   (!string.IsNullOrEmpty(emp.Badgenumber) && emp.Badgenumber.ToLower().Contains(searchTc));

                    return matchName && matchTc;
                }).ToList();

                string currentSelectedBadge = cbSelectedEmployeeCard.SelectedValue?.ToString();

                cbSelectedEmployeeCard.ItemsSource = null;
                cbSelectedEmployeeCard.ItemsSource = filtered;

                if (filtered.Count > 0)
                {
                    // Eğer önceden seçili kişi filtrelenmiş listede varsa onu seçili tut
                    var matchExisting = filtered.FirstOrDefault(x => x.Badgenumber == currentSelectedBadge);
                    if (matchExisting != null)
                    {
                        cbSelectedEmployeeCard.SelectedItem = matchExisting;
                    }
                    else
                    {
                        cbSelectedEmployeeCard.SelectedIndex = 0;
                    }
                }
                else
                {
                    UpdateSingleEmployeeDetailsView();
                }
            }
            catch (Exception ex)
            {
                Logger.LogException("FilterCardEmployeeList", ex);
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
                if (cbSelectedEmployeeCard == null) return;

                // Seçili personeli bul (SelectedItem veya SelectedValue veya currentEmployees üzerinden)
                Employee emp = cbSelectedEmployeeCard.SelectedItem as Employee;
                if (emp == null && cbSelectedEmployeeCard.SelectedValue != null)
                {
                    string badge = cbSelectedEmployeeCard.SelectedValue.ToString();
                    emp = currentEmployees?.FirstOrDefault(x => x.Badgenumber == badge);
                }
                if (emp == null && cbSelectedEmployeeCard.SelectedIndex >= 0 && currentEmployees != null && cbSelectedEmployeeCard.SelectedIndex < currentEmployees.Count)
                {
                    emp = currentEmployees[cbSelectedEmployeeCard.SelectedIndex];
                }
                if (emp == null && currentEmployees != null && currentEmployees.Count > 0)
                {
                    emp = currentEmployees[0];
                    if (cbSelectedEmployeeCard.SelectedIndex != 0)
                    {
                        cbSelectedEmployeeCard.SelectedIndex = 0;
                    }
                }

                if (emp == null)
                {
                    // Personel seçili değilse kartları temizle
                    if (txtCardEmpName != null) txtCardEmpName.Text = "Personel Seçiniz";
                    if (txtCardEmpBadge != null) txtCardEmpBadge.Text = "ID: -";
                    if (txtCardTcNo != null) txtCardTcNo.Text = "-";
                    if (txtCardEmpType != null) txtCardEmpType.Text = "-";
                    if (txtCardGroup != null) txtCardGroup.Text = "-";
                    if (txtCardDept != null) txtCardDept.Text = "-";
                    if (txtCardTitle != null) txtCardTitle.Text = "-";
                    if (txtCardCompanyBranch != null) txtCardCompanyBranch.Text = "-";
                    if (txtCardRfid != null) txtCardRfid.Text = "-";
                    if (txtCardPhone != null) txtCardPhone.Text = "-";
                    if (txtCardCompanyPhone != null) txtCardCompanyPhone.Text = "-";
                    if (txtCardStatus != null) txtCardStatus.Text = "-";
                    SingleEmpDailyList?.Clear();
                    if (txtCardWorkedDays != null) txtCardWorkedDays.Text = "0 Gün";
                    if (txtCardTotalNet != null) txtCardTotalNet.Text = "0.00 Saat";
                    if (txtCardWeekdayOt != null) txtCardWeekdayOt.Text = "0.00 Saat";
                    if (txtCardSaturdayOt != null) txtCardSaturdayOt.Text = "0.00 Saat";
                    if (txtCardSundayOt != null) txtCardSundayOt.Text = "0.00 Saat";
                    if (txtCardMorningOt != null) txtCardMorningOt.Text = "0.00 Saat";
                    if (txtCardEveningOt != null) txtCardEveningOt.Text = "0.00 Saat";
                    if (txtCardNightOt != null) txtCardNightOt.Text = "0.00 Saat";
                    if (txtCardTotalExcessBreak != null) txtCardTotalExcessBreak.Text = "0 dk";
                    if (txtCardTotalWeightedOt != null) txtCardTotalWeightedOt.Text = "0.00 Saat";
                    if (txtCardTotalMissing != null) txtCardTotalMissing.Text = "0.00 Saat";
                    return;
                }

                // Özlük Kartı Alanlarını Güncelle
                if (txtCardEmpName != null) txtCardEmpName.Text = emp.Name;
                if (txtCardEmpBadge != null) txtCardEmpBadge.Text = $"Sicil / Cihaz ID: {emp.Badgenumber}";
                if (txtCardTcNo != null) txtCardTcNo.Text = string.IsNullOrEmpty(emp.TcNo) ? "-" : emp.TcNo;
                if (txtCardEmpType != null) txtCardEmpType.Text = string.IsNullOrEmpty(emp.EmploymentType) ? "Kadrolu" : emp.EmploymentType;
                if (txtCardGroup != null) txtCardGroup.Text = string.IsNullOrEmpty(emp.GroupName) ? "Genel" : emp.GroupName;
                if (txtCardDept != null) txtCardDept.Text = string.IsNullOrEmpty(emp.Department) ? "-" : emp.Department;
                if (txtCardTitle != null) txtCardTitle.Text = string.IsNullOrEmpty(emp.Title) ? "-" : emp.Title;
                if (txtCardCompanyBranch != null) txtCardCompanyBranch.Text = $"{emp.Company ?? "-"} / {emp.Branch ?? "-"}";
                if (txtCardRfid != null) txtCardRfid.Text = string.IsNullOrEmpty(emp.CardNo) ? "-" : emp.CardNo;
                if (txtCardPhone != null) txtCardPhone.Text = string.IsNullOrEmpty(emp.Phone) ? "-" : emp.Phone;
                if (txtCardCompanyPhone != null)
                {
                    txtCardCompanyPhone.Text = string.IsNullOrEmpty(emp.CompanyPhone) ? "-" : emp.CompanyPhone;
                }
                if (txtCardStatus != null) txtCardStatus.Text = string.IsNullOrEmpty(emp.Status) ? "Aktif" : emp.Status;

                // Bu Personelin Gün Gün Kayıtlarını Getir:
                int year = (cbReportYear != null && cbReportYear.SelectedItem != null) ? (int)cbReportYear.SelectedItem : DateTime.Now.Year;
                int month = (cbReportMonth != null && cbReportMonth.SelectedIndex >= 0) ? cbReportMonth.SelectedIndex + 1 : DateTime.Now.Month;
                var holidays = HolidayService.GetHolidaysForYear(year, dbService);

                DateTime? rangeStart = null;
                DateTime? rangeEnd = null;
                if (chkUseDateRange != null && chkUseDateRange.IsChecked == true)
                {
                    rangeStart = dpStartDate?.SelectedDate ?? new DateTime(year, month, 1);
                    rangeEnd = dpEndDate?.SelectedDate ?? rangeStart.Value.AddDays(1);
                }

                var empList = new List<Employee> { emp };
                // Kişi görüntüleme ekranında personelin kendi mesaisi silinmeden tam/ham olarak görüntülenmesi istendiğinde:
                // ShiftRule kopyası oluşturulup limitsiz (ExcludeOvernightOvertime = false, MaxDaily=0 vb.) geçilerek mesaisi tam hesaplanır
                var unconstrainedShifts = currentShifts?.Select(s => new ShiftRule
                {
                    Id = s.Id,
                    Name = s.Name,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    GracePeriodMinutes = s.GracePeriodMinutes,
                    EarlyInToleranceMinutes = s.EarlyInToleranceMinutes,
                    LunchBreakMinutes = s.LunchBreakMinutes,
                    DuplicateFilterSeconds = s.DuplicateFilterSeconds,
                    IsOvernight = s.IsOvernight,
                    MaxDailyOvertimeHours = 0, // Günlük sınır yok (Tam mesai)
                    MaxWeeklyOvertimeHours = 0, // Haftalık sınır yok
                    MaxMonthlyOvertimeHours = 0, // Aylık sınır yok
                    ExcludeOvernightOvertime = false, // Akşamdan sabaha olan mesailer silinmesin
                    MinOvertimeThresholdMinutes = s.MinOvertimeThresholdMinutes,
                    RequiredDailyWorkHours = s.RequiredDailyWorkHours,
                    AllowEarlyInOvertime = true, // Sabah erken gelmeler mesaiye dahil edilsin
                    EarlyInMaxOvertimeHours = s.EarlyInMaxOvertimeHours > 0 ? s.EarlyInMaxOvertimeHours : 4.0,
                    OvertimeWindowStart = s.OvertimeWindowStart,
                    OvertimeWindowEnd = new TimeSpan(23, 59, 59),
                    TargetGroup = s.TargetGroup,
                    ApplyToAllMatchingEmployees = s.ApplyToAllMatchingEmployees,
                    WeekdayMultiplier = s.WeekdayMultiplier,
                    WeekendMultiplier = s.WeekendMultiplier,
                    HolidayMultiplier = s.HolidayMultiplier,
                    RoundingMinutes = s.RoundingMinutes
                }).ToList();

                var empCalculated = AttendanceCalculator.CalculateAdvancedShiftReport(
                    LogList, empList, unconstrainedShifts ?? currentShifts, year, month, holidays, rangeStart, rangeEnd, currentShiftSchedules);

                // Eğer seçili ayda personelin hareketi yoksa ama geçmiş/gelecek aylarda logları varsa,
                // personelin en son hareket gördüğü ayı otomatik bulup gösterelim (Kullanıcı boş ekran görmesin)
                if (empCalculated.Count == 0 && (rangeStart == null || rangeEnd == null))
                {
                    var empLogs = LogList?.Where(l => l.UserId == emp.Badgenumber).ToList();
                    if (empLogs != null && empLogs.Count > 0)
                    {
                        var latestLog = empLogs
                            .Select(l => DateTime.TryParse(l.LogTime, out DateTime d) ? (DateTime?)d : null)
                            .Where(d => d.HasValue)
                            .OrderByDescending(d => d.Value)
                            .FirstOrDefault();

                        if (latestLog.HasValue && (latestLog.Value.Year != year || latestLog.Value.Month != month))
                        {
                            year = latestLog.Value.Year;
                            month = latestLog.Value.Month;
                            empCalculated = AttendanceCalculator.CalculateAdvancedShiftReport(
                                LogList, empList, unconstrainedShifts ?? currentShifts, year, month, holidays, null, null, currentShiftSchedules);
                        }
                    }
                }

                if (lblEmpSummaryTitle != null)
                {
                    lblEmpSummaryTitle.Text = $"{year} / {month:D2} DÖNEMİ TOPLAMLARI";
                }

                SingleEmpDailyList.Clear();
                foreach (var rec in empCalculated.OrderBy(r => r.DateString))
                {
                    SingleEmpDailyList.Add(rec);
                }

                if (dgEmpDailyDetails != null && dgEmpDailyDetails.Items != null)
                {
                    dgEmpDailyDetails.Items.Refresh();
                }

                // İstatistikleri Hesapla
                int workedDays = empCalculated.Count(r => r.NetWorkHours > 0);
                double totalNet = Math.Round(empCalculated.Sum(r => r.NetWorkHours), 2);
                double totalDayOt = Math.Round(empCalculated.Sum(r => r.DayOvertimeHours), 2);
                double totalNightOt = Math.Round(empCalculated.Sum(r => r.NightOvertimeHours), 2);
                double totalWeightedOt = Math.Round(empCalculated.Sum(r => r.WeightedOvertimeHours), 2);
                double totalMissing = Math.Round(empCalculated.Sum(r => r.MissingWorkHours), 2);
                double totalExcessBreak = Math.Round(empCalculated.Sum(r => r.ExcessBreakMinutes), 1);

                // Bu personele özel Hafta İçi, Cumartesi, Pazar/Tatil mesaisi
                double empWkdOt = Math.Round(empCalculated
                    .Where(r => DateTime.TryParse(r.DateString, out DateTime d) && d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
                    .Sum(r => r.ApprovedOvertime), 2);
                double empSatOt = Math.Round(empCalculated
                    .Where(r => DateTime.TryParse(r.DateString, out DateTime d) && d.DayOfWeek == DayOfWeek.Saturday)
                    .Sum(r => r.NetWorkHours), 2);
                double empSunOt = Math.Round(empCalculated
                    .Where(r => (DateTime.TryParse(r.DateString, out DateTime d) && d.DayOfWeek == DayOfWeek.Sunday) || (!string.IsNullOrEmpty(r.DayType) && r.DayType.StartsWith("Resmi Tatil")))
                    .Sum(r => r.NetWorkHours), 2);

                // Sabah, Akşam, Gece Mesaileri
                double empMorningOt = Math.Round(empCalculated.Sum(r => r.MorningOvertimeHours), 2);
                double empEveningOt = Math.Round(empCalculated.Sum(r => r.EveningOvertimeHours), 2);
                double empNightShiftOt = Math.Round(empCalculated.Sum(r => r.NightShiftOvertimeHours), 2);

                if (txtCardWorkedDays != null) txtCardWorkedDays.Text = $"{workedDays} Gün";
                if (txtCardTotalNet != null) txtCardTotalNet.Text = ShiftAttendanceResult.FormatHoursToHourMinute(totalNet);
                if (txtCardWeekdayOt != null) txtCardWeekdayOt.Text = ShiftAttendanceResult.FormatHoursToHourMinute(empWkdOt);
                if (txtCardSaturdayOt != null) txtCardSaturdayOt.Text = ShiftAttendanceResult.FormatHoursToHourMinute(empSatOt);
                if (txtCardSundayOt != null) txtCardSundayOt.Text = ShiftAttendanceResult.FormatHoursToHourMinute(empSunOt);
                if (txtCardMorningOt != null) txtCardMorningOt.Text = ShiftAttendanceResult.FormatHoursToHourMinute(empMorningOt);
                if (txtCardEveningOt != null) txtCardEveningOt.Text = ShiftAttendanceResult.FormatHoursToHourMinute(empEveningOt);
                if (txtCardNightOt != null) txtCardNightOt.Text = ShiftAttendanceResult.FormatHoursToHourMinute(empNightShiftOt);
                if (txtCardTotalExcessBreak != null) txtCardTotalExcessBreak.Text = $"{totalExcessBreak} dk";
                if (txtCardTotalWeightedOt != null) txtCardTotalWeightedOt.Text = ShiftAttendanceResult.FormatHoursToHourMinute(totalWeightedOt);
                if (txtCardTotalMissing != null) txtCardTotalMissing.Text = ShiftAttendanceResult.FormatHoursToHourMinute(totalMissing);
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
                        worksheet.Cell(1, 5).Value = "T.C. Kimlik No";
                        worksheet.Cell(1, 6).Value = "Durum / Statü";
                        worksheet.Cell(1, 7).Value = "Grup";
                        worksheet.Cell(1, 8).Value = "İstihdam Türü";
                        worksheet.Cell(1, 9).Value = "İşe Giriş (Aktif Tarihi)";
                        worksheet.Cell(1, 10).Value = "Ayrılış (Pasif Tarihi)";
                        worksheet.Cell(1, 11).Value = "Departman";
                        worksheet.Cell(1, 12).Value = "Birim";
                        worksheet.Cell(1, 13).Value = "Unvan";
                        worksheet.Cell(1, 14).Value = "Şirket / Şube";
                        worksheet.Cell(1, 15).Value = "Kart (RFID) No";
                        worksheet.Cell(1, 16).Value = "Şahsi Telefon";
                        worksheet.Cell(1, 17).Value = "Firma Telefonu";
                        worksheet.Cell(1, 18).Value = "Vardiya";
                        worksheet.Cell(1, 19).Value = "İlk Giriş";
                        worksheet.Cell(1, 20).Value = "Son Çıkış";
                        worksheet.Cell(1, 21).Value = "12 Öncesi Çal.(s)";
                        worksheet.Cell(1, 22).Value = "12 Sonrası Gece Çal.(s)";
                        worksheet.Cell(1, 23).Value = "Fiili Mola (dk)";
                        worksheet.Cell(1, 24).Value = "Mola Aşımı (dk)";
                        worksheet.Cell(1, 25).Value = "Net Çalışma (s)";
                        worksheet.Cell(1, 26).Value = "Sabah Mesaisi (s)";
                        worksheet.Cell(1, 27).Value = "Akşam Mesaisi (s)";
                        worksheet.Cell(1, 28).Value = "Gece Mesaisi (s)";
                        worksheet.Cell(1, 29).Value = "Hakediş Mesai (s)";
                        worksheet.Cell(1, 30).Value = "Geç Kalma (dk)";
                        worksheet.Cell(1, 31).Value = "Eksik (s)";
                        worksheet.Cell(1, 32).Value = "Puantaj Durumu";

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
                            worksheet.Cell(row, 5).Value = item.TcNo ?? "";
                            worksheet.Cell(row, 6).Value = item.EmployeeStatus ?? "Aktif";
                            worksheet.Cell(row, 7).Value = item.GroupName ?? "Genel";
                            worksheet.Cell(row, 8).Value = item.EmploymentType ?? "Kadrolu";
                            worksheet.Cell(row, 9).Value = item.HireDate ?? "";
                            worksheet.Cell(row, 10).Value = item.LeaveDate ?? "";
                            worksheet.Cell(row, 11).Value = item.Department ?? "";
                            worksheet.Cell(row, 12).Value = item.Unit ?? "";
                            worksheet.Cell(row, 13).Value = item.Title ?? "";
                            worksheet.Cell(row, 14).Value = !string.IsNullOrEmpty(item.Company) ? $"{item.Company} / {item.Branch ?? ""}" : "";
                            worksheet.Cell(row, 15).Value = item.CardNo ?? "";
                            worksheet.Cell(row, 16).Value = item.Phone ?? "";
                            worksheet.Cell(row, 17).Value = item.CompanyPhone ?? "";
                            worksheet.Cell(row, 18).Value = item.ShiftName ?? "";
                            worksheet.Cell(row, 19).Value = item.ActualFirstIn ?? "";
                            worksheet.Cell(row, 20).Value = item.ActualLastOut ?? "";
                            worksheet.Cell(row, 21).Value = item.DayWorkHours;
                            worksheet.Cell(row, 22).Value = item.NightWorkHours;
                            worksheet.Cell(row, 23).Value = item.ActualBreakMinutes;
                            worksheet.Cell(row, 24).Value = item.ExcessBreakMinutes;
                            worksheet.Cell(row, 25).Value = item.NetWorkHours;
                            worksheet.Cell(row, 26).Value = item.MorningOvertimeHours;
                            worksheet.Cell(row, 27).Value = item.EveningOvertimeHours;
                            worksheet.Cell(row, 28).Value = item.NightShiftOvertimeHours;
                            worksheet.Cell(row, 29).Value = item.WeightedOvertimeHours;
                            worksheet.Cell(row, 30).Value = item.LateMinutes;
                            worksheet.Cell(row, 31).Value = item.MissingWorkHours;
                            worksheet.Cell(row, 32).Value = item.Status ?? "";

                            if (item.NightWorkHours > 0 || item.NightShiftOvertimeHours > 0)
                            {
                                worksheet.Cell(row, 22).Style.Font.Bold = true;
                                worksheet.Cell(row, 22).Style.Font.FontColor = XLColor.FromHtml("#0D47A1");
                                worksheet.Cell(row, 28).Style.Font.Bold = true;
                                worksheet.Cell(row, 28).Style.Font.FontColor = XLColor.FromHtml("#0D47A1");
                            }

                            if (item.MorningOvertimeHours > 0)
                            {
                                worksheet.Cell(row, 26).Style.Font.Bold = true;
                                worksheet.Cell(row, 26).Style.Font.FontColor = XLColor.FromHtml("#D97706");
                            }

                            if (item.ExcessBreakMinutes > 0)
                            {
                                worksheet.Cell(row, 24).Style.Font.Bold = true;
                                worksheet.Cell(row, 24).Style.Font.FontColor = XLColor.FromHtml("#DC2626");
                            }

                            if (item.WeightedOvertimeHours > 0)
                            {
                                worksheet.Cell(row, 29).Style.Font.Bold = true;
                                worksheet.Cell(row, 29).Style.Font.FontColor = XLColor.FromHtml("#15803D");
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

        private void CbReportGroup_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            CalculateShiftReport();
        }

        private void CbReportEmploymentTypeFilter_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            CalculateShiftReport();
        }

        private void CbReportStatusFilter_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            CalculateShiftReport();
        }

        private void CbReportDayFilter_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            CalculateShiftReport();
        }

        private void TxtSearch_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            CalculateShiftReport();
        }

        private void BtnHelp_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.FrameworkElement elem && elem.Tag is string tag)
            {
                ShowHelpDialogByTag(tag);
            }
        }

        private void ShowHelpDialogByTag(string tag)
        {
            string title = "Özellik Açıklaması";
            string desc = "";
            string tip = "";

            switch (tag)
            {
                case "IpPort":
                    title = "Cihaz IP Adresi ve Port";
                    desc = "ZKTeco parmak izi, yüz tanıma veya kart okuyucu cihazınızın yerel ağdaki (Ethernet / Wi-Fi) IP adresi ve haberleşme portudur. Varsayılan IP '127.0.0.1' (Simülatör için) veya cihazınızın ağdaki IP adresidir. Standart PDKS iletişim portu '4370'tir.";
                    tip = "Cihazınız ağa bağlıysa ve 'Bağlan' dediğinizde hata alıyorsanız IP adresinin doğruluğunu ve cihaz menüsünden 'İletişim -> Ethernet' ayarlarını kontrol ediniz.";
                    break;

                case "Connect":
                    title = "Cihaza Bağlan";
                    desc = "Girilen IP adresi ve Port üzerinden ZKTeco donanımına TCP/IP soket bağlantısı kurar. Bağlantı başarılı olduğunda üst paneldeki CİHAZ VE HAT durumu yeşile döner ve cihazın dahili saati canlı olarak senkronize edilir.";
                    tip = "Cihazdan veri çekmeden veya saat eşitlemesi yapmadan önce mutlaka bağlantı kurulmuş olmalıdır.";
                    break;

                case "GetLogs":
                    title = "Logları Çek (Cihazdan Canlı Okuma)";
                    desc = "Cihazın hafızasında kayıtlı olan tüm biyometrik geçiş hareketlerini (parmak izi basımları, kart okutmaları, yüz doğrulamaları) çeker ve yerel SQLite veritabanına kaydeder. Cihaz hafızasındaki orijinal loglar ASLA SİLİNMEZ (salt okunur güvenlik kuralı).";
                    tip = "Her gün mesai sonunda veya haftalık olarak 'Logları Çek' butonuna basarak tüm yeni hareketleri sisteme aktarabilirsiniz.";
                    break;

                case "SyncDeviceTime":
                    title = "Cihaz Saatini İnternetle Eşitle";
                    desc = "Türkiye canlı resmi atomik internet saatini çeker ve bağlı olan ZKTeco cihazının donanımsal dahili saatine (RTC) ve takvimine yazar. Cihaz saatinin geri kalması veya ileri gitmesi sonucunda personelin erken/geç basmış gibi görünmesini engeller.";
                    tip = "Cihazın saatini haftada bir kez bu butona basarak internet saatiyle eşitlemeniz tavsiye edilir.";
                    break;

                case "Employees":
                    title = "Personel Özlük Yönetimi";
                    desc = "Personellerin T.C. Kimlik Numarası, İsim, Soyisim, Kart (RFID) Numarası, Departman, Unvan, Çalışma Grubu, İstihdam Türü (Kadrolu / Taşeron), İşe Giriş ve Ayrılış tarihlerinin yönetildiği ekrandır.";
                    tip = "Yeni işe başlayan veya ayrılan personelleri bu ekrandan 'Aktif / Pasif' olarak güncelleyebilirsiniz.";
                    break;

                case "Groups":
                    title = "Grup & Statü Yönetimi";
                    desc = "İşletmenizdeki departman ve istihdam türlerini (Kadrolu, Taşeron, Sözleşmeli, Stajyer, Üretim, Yönetim, Güvenlik vb.) tanımlayabileceğiniz ve özelleştirebileceğiniz yönetim merkezidir.";
                    tip = "Farklı taşeron firmaların çalışanlarını veya vardiya gruplarını buradan gruplandırarak raporlarda ayrı ayrı süzebilirsiniz.";
                    break;

                case "Shifts":
                    title = "Mesai & Taşeron Kuralları";
                    desc = "Vardiya başlangıç ve bitiş saatleri, tolerans dakikaları, zorunlu çalışma süresi, yemek molası kesintisi, sabah erken gelme mesaisi, akşam mesai kabul penceresi, günlük-haftalık-aylık azami mesai tavan sınırları ve gece (24:00+) mesaisi kurallarının tanımlandığı merkezdir.";
                    tip = "'Fazla Mesai Silinmesin' kutucuğunu işaretleyerek personelin hiçbir mesaisinin tavan sınırlara takılmadan eksiksiz hakedişe yazılmasını sağlayabilirsiniz.";
                    break;

                case "PeriodYear":
                    title = "Bordro / Rapor Yılı";
                    desc = "Puantaj ve mesai raporunun hesaplanacağı takvim yılını belirler. Cihazdaki geçmiş yıllara ait hareketleri veya güncel yılı buradan seçebilirsiniz.";
                    break;

                case "DateRange":
                    title = "Özel İki Tarih Arası Puantaj";
                    desc = "Belirli bir aya bağlı kalmaksızın, örneğin 15 Ocak ile 14 Şubat arasındaki bir hakediş dönemini incelemek için başlangıç ve bitiş tarihini belirlemenizi sağlar.";
                    tip = "Tarih aralığı kutucuğunu işaretlediğinizde üstteki ay seçimi devre dışı kalır ve sadece belirlediğiniz iki tarih arası hesaplanır.";
                    break;

                case "TodayFilter":
                    title = "Bugün (Günlük Hareketler)";
                    desc = "Rapor takvimini bugünün güncel tarihine odaklar ve işletmede bugün kimlerin giriş-çıkış yaptığını anlık olarak listeler.";
                    break;

                case "RefreshDb":
                    title = "Veritabanını Yenile";
                    desc = "Yerel SQLite veritabanındaki geçiş loglarını ve personel listesini yeniden okuyarak ekrandaki ham geçiş hareketleri tablosunu tazeler.";
                    break;

                case "ExportAllLogs":
                    title = "Tüm Hareketleri Excel'e Aktar";
                    desc = "Veritabanında kayıtlı olan parmak izi, kart ve yüz tanıma ham geçiş loglarının tamamını (tarih, saat, personel ID, doğrulama türü) Excel tablosu olarak kaydeder.";
                    break;

                case "PeriodMonth":
                    title = "Bordro / Rapor Dönemi (Yıl & Ay)";
                    desc = "Puantaj ve mesai raporunun hesaplanacağı ay ve yılı belirler. Seçilen ayın 1. gününden son gününe kadar olan tüm geçişler vardiya kurallarına göre analiz edilir.";
                    tip = "Geçmiş ayların veya gelecek dönemin bordro hesaplamalarını görmek için buradan ayı değiştirebilirsiniz.";
                    break;

                case "DayFilter":
                    title = "Gün Tipi Filtresi";
                    desc = "Rapor tablosunda yalnızca Hafta İçi, Cumartesi, Pazar veya Resmi Tatil günlerine ait çalışma ve mesai kayıtlarının filtrelenmesini sağlar.";
                    tip = "Örneğin sadece Resmi Tatil günlerinde yapılan çalışmaları görmek için bu listeden 'Sadece Resmi Tatiller' seçeneğini belirleyebilirsiniz.";
                    break;

                case "SyncLatestPeriod":
                    title = "Son Veri Dönemine Git";
                    desc = "Cihazdan çekilen veya veritabanında kayıtlı olan en son geçiş hareketinin tarihini otomatik tespit eder ve raporlama takvimini o aya ve yıla odaklar.";
                    tip = "Cihazdan geçmiş bir döneme ait log çektiyseniz takvimde manuel aramadan tek tıkla o döneme gitmek için kullanabilirsiniz.";
                    break;

                case "SyncInternetTimeBtn":
                    title = "Türkiye Canlı Tarih/Saat & İnternet Senkronu";
                    desc = "Türkiye canlı internet saatini sorgulayarak uygulama takvimini içinde bulunduğumuz güncel ay ve yıla konumlandırır.";
                    break;

                case "RefreshHolidays":
                    title = "Resmi Tatilleri İnternetten Güncelle";
                    desc = "Seçilen yıla ait 23 Nisan, 29 Ekim, Ramazan & Kurban Bayramı gibi Türkiye ulusal resmi tatil takvimini internet servislerinden otomatik olarak indirip kaydeder.";
                    tip = "Resmi tatil günlerinde yapılan çalışmalar kuralınızdaki tatil mesai katsayısı (örneğin %100 / x2.0) üzerinden hesaplanır.";
                    break;

                case "GroupFilter":
                    title = "Grup Filtresi";
                    desc = "Rapor tablosunda sadece belirli bir departmanın veya çalışma grubunun (örneğin yalnızca 'Üretim' veya 'Montaj') çalışanlarını listeler.";
                    break;

                case "TypeFilter":
                    title = "İstihdam Türü Filtresi (Kadrolu / Taşeron)";
                    desc = "Puantaj tablosunu personelin çalışma statüsüne göre (Kadrolu, Taşeron, Sözleşmeli, Yevmiyeli vb.) ayırır. Taşeron personellerin hakedişlerini şirket kadrosundan ayrı incelemek için idealdir.";
                    break;

                case "StatusFilter":
                    title = "Personel Statü Filtresi";
                    desc = "Sadece aktif çalışanları, işten ayrılmış eski personelleri veya izinli personelleri raporda filtrelemenize olanak tanır.";
                    break;

                case "SearchNameTc":
                    title = "Ad Soyad ve T.C. No Hızlı Arama";
                    desc = "İsmini veya T.C. kimlik numarasını yazdığınız personeli yüzlerce çalışan arasından anlık olarak bulur ve listeler.";
                    break;

                case "Calculate":
                    title = "Hesapla Butonu";
                    desc = "Seçili ay, gün, grup ve personel filtrelerine göre tüm geçiş loglarını vardiya kuralları, geç kalma, mola aşımı, gece/gündüz mesai ayrımları ve çarpanlarıyla baştan hesaplar.";
                    tip = "Filtreleri veya vardiya kurallarını değiştirdiğinizde güncel sonuçları görmek için 'Hesapla' butonuna basabilirsiniz.";
                    break;

                case "ClearFilters":
                    title = "Filtreleri Sıfırla";
                    desc = "Arama kutuları, grup, statü ve gün seçimlerinin tümünü temizleyerek varsayılan 'Tümü' durumuna geri döndürür.";
                    break;

                case "ExportMonthly":
                    title = "Bordro Excel'e Aktar";
                    desc = "Seçilen döneme ait aylık özet bordro ve hakediş tablosunu (normal çalışma saatleri, fazla mesailer, çarpanlı hakedişler, eksik çalışmalar) renkli ve formatlı bir Excel tablosu olarak dışa aktarır.";
                    tip = "Muhasebe ve bordro departmanına vermek için bu Excel çıktısını kullanabilirsiniz.";
                    break;

                case "ExportDaily":
                    title = "Gün Gün Puantaj Excel'e Aktar";
                    desc = "Personellerin seçilen aydaki 1. günden 31. güne kadar her bir güne ait ilk giriş, son çıkış, net çalışma, sabah-akşam-gece mesailerini gün gün döküm halinde Excel'e kaydeder.";
                    break;

                case "EmpCardSelect":
                    title = "İncelenecek Personel Kartı";
                    desc = "360 Derece Personel Bilgi Paneli'nde detaylarını incelemek istediğiniz çalışanı seçmenizi sağlar. Seçilen personelin tüm özlük bilgileri, aylık çalışma grafiği, sabah-akşam-gece mesaileri ve gün gün hareket tablosu anında görüntülenir.";
                    break;

                case "ExportEmpDetail":
                    title = "Personelin Aylık Detayını Excel'e Aktar";
                    desc = "Seçili personelin tüm ay boyunca çalıştığı günleri, giriş-çıkış saatlerini, mola kullanımlarını ve mesai dökümünü o kişiye özel Excel kartı olarak kaydeder.";
                    break;

                case "ShiftSchedule":
                    title = "Haftalık / Tarih Bazlı Vardiya Çizelgesi";
                    desc = "Personellere tarih aralığına göre değişen vardiya programları atamanızı sağlar. Örneğin 1. hafta sabah vardiyası, sonraki hafta gece vardiyası veya taşeron/kadro rotasyonları bu ekrandan takvime bağlanır.";
                    tip = "Vardiya ataması yapıldığında sistem o tarihteki günleri otomatik olarak yeni vardiya saatleriyle puantajlandırır.";
                    break;

                case "IrregularPunches":
                    title = "Parmak İzine Düzensiz Basanlar Analizi";
                    desc = "Giriş yapıp çıkış basmayan (tek basım), çok geç kalan veya erken çıkan personelleri anında filtreleyip listeler ve Excel raporu almanızı sağlar.";
                    tip = "Eksik ve hatalı basımları düzeltmek veya denetlemek için bu pencereyi kullanabilirsiniz.";
                    break;

                default:
                    title = "Sistem Yardımı";
                    desc = "Bu özellik ZKTeco PDKS sisteminin otomatik hesaplama ve raporlama modüllerinden biridir.";
                    break;
            }

            HelpInfoWindow.ShowHelp(this, title, desc, tip);
        }
    }
}
