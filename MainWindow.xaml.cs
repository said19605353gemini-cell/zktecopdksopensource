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

        public string LocalizedInOutMode
        {
            get
            {
                if (string.IsNullOrEmpty(InOutMode)) return "";
                string s = InOutMode.Trim().ToLowerInvariant();
                if (s == "giriş" || s == "in" || s == "check-in") return LocalizationService.Get("Punch_In");
                if (s == "çıkış" || s == "out" || s == "check-out") return LocalizationService.Get("Punch_Out");
                if (s.Contains("mola") && s.Contains("çıkış")) return LocalizationService.Get("Punch_BreakOut");
                if (s.Contains("mola") && (s.Contains("dönüş") || s.Contains("giriş"))) return LocalizationService.Get("Punch_BreakIn");
                if (s.Contains("mesai") && s.Contains("giriş")) return LocalizationService.Get("Punch_OtIn");
                if (s.Contains("mesai") && s.Contains("çıkış")) return LocalizationService.Get("Punch_OtOut");
                return InOutMode;
            }
        }

        public string LocalizedVerifyMode
        {
            get
            {
                if (string.IsNullOrEmpty(VerifyMode)) return "";
                string s = VerifyMode.Trim().ToLowerInvariant();
                if (s.Contains("parmak") || s.Contains("finger")) return LocalizationService.Get("Verify_Finger");
                if (s.Contains("kart") || s.Contains("card") || s.Contains("rfid")) return LocalizationService.Get("Verify_Card");
                if (s.Contains("yüz") || s.Contains("face")) return LocalizationService.Get("Verify_Face");
                if (s.Contains("pin") || s.Contains("şifre") || s.Contains("password")) return LocalizationService.Get("Verify_Pin");
                return LocalizationService.Get("Verify_Other");
            }
        }
    }

    public partial class MainWindow : Window
    {
        public ZkService zkService;
        public DatabaseService dbService;
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
                RestoreSavedLanguageAndCountry();

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

            PopulateMonthCombobox(DateTime.Now.Month);

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
                    cbReportMonth.SelectedIndex = logMonth;

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
                MessageBox.Show(LocalizationService.Get("Msg_NoLogsInDb"), LocalizationService.Get("MsgTitle_Info"), MessageBoxButton.OK, MessageBoxImage.Information);
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
                    timeResult = HolidayService.GetCurrentCountryDateTimeSafe();
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
                cbReportMonth.SelectedIndex = netMonth;

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

                var country = HolidayService.GetCurrentCountry();
                if (timeResult.IsFromInternet)
                {
                    MessageBox.Show($"{country.Flag} {country.Name} Canlı Tarih & Saati İnternetten Alındı:\n" +
                                    $"• Tarih / Saat: {targetTime:dd.MM.yyyy HH:mm:ss} ({country.TimeZoneId})\n" +
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

                var country = HolidayService.GetCurrentCountry();
                int count = holidays?.Count ?? 0;
                MessageBox.Show($"{year} yılı için {country.Flag} {country.Name} resmi tatilleri başarıyla internetten çekildi ve güncellendi ({count} tatil günü).", 
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

        
        
        private DeviceSyncConfigWindow activeDeviceSyncConfigWindow;
        private void BtnOpenDeviceSyncConfig_Click(object sender, RoutedEventArgs e)
        {
            activeDeviceSyncConfigWindow = new DeviceSyncConfigWindow(this);
            activeDeviceSyncConfigWindow.Closed += (s, ev) => activeDeviceSyncConfigWindow = null;
            activeDeviceSyncConfigWindow.ShowDialog();
        }

        
        public void UpdateCountryButtonLabels()
        {
            try
            {
                var country = HolidayService.GetCurrentCountry();
                string timeFormat = LocalizationService.Get("BtnLiveTimeFormat");
                string holidayFormat = LocalizationService.Get("BtnUpdateHolidaysFormat");
                string tipClockFormat = LocalizationService.Get("TooltipSyncClock");
                string tipHolidayFormat = LocalizationService.Get("TooltipRefreshHolidays");

                string countryDisplayName = country.LocalizedName;

                if (txtBtnInternetTime != null)
                {
                    txtBtnInternetTime.Text = string.Format(timeFormat, $"{country.Flag} {countryDisplayName}");
                }
                if (txtBtnUpdateHolidays != null)
                {
                    txtBtnUpdateHolidays.Text = string.Format(holidayFormat, $"{country.Flag} {countryDisplayName}");
                }
                if (btnSyncDeviceTime != null)
                {
                    btnSyncDeviceTime.ToolTip = string.Format(tipClockFormat, country.Flag, countryDisplayName, country.TimeZoneId);
                }
                if (btnRefreshHolidays != null)
                {
                    btnRefreshHolidays.ToolTip = string.Format(tipHolidayFormat, country.Flag, countryDisplayName);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException("UpdateCountryButtonLabels", ex);
            }
        }

        
        private void RestoreSavedLanguageAndCountry()
        {
            try
            {
                string savedLang = dbService.GetSetting("SelectedLanguage", "tr");
                string savedCountry = dbService.GetSetting("SelectedCountry", null);

                if (!string.IsNullOrEmpty(savedLang))
                {
                    LocalizationService.CurrentLanguage = savedLang;
                    if (cbLanguage != null)
                    {
                        cbLanguage.SelectionChanged -= CbLanguage_SelectionChanged;
                        foreach (System.Windows.Controls.ComboBoxItem item in cbLanguage.Items)
                        {
                            if (item.Tag != null && item.Tag.ToString().Equals(savedLang, StringComparison.OrdinalIgnoreCase))
                            {
                                cbLanguage.SelectedItem = item;
                                break;
                            }
                        }
                        cbLanguage.SelectionChanged += CbLanguage_SelectionChanged;
                    }
                }

                if (!string.IsNullOrEmpty(savedCountry))
                {
                    HolidayService.CurrentCountryCode = savedCountry;
                    DeviceSyncConfigWindow.SelectedDeviceClockCountryCode = savedCountry;
                    DeviceSyncConfigWindow.SelectedHolidayCountryCode = savedCountry;
                    DeviceSyncConfigWindow.SelectedCalendarCountryCode = savedCountry;
                }
                else
                {
                    string countryCode = "TR";
                    if (savedLang.Equals("de", StringComparison.OrdinalIgnoreCase)) countryCode = "DE";
                    else if (savedLang.Equals("ru", StringComparison.OrdinalIgnoreCase)) countryCode = "RU";
                    else if (savedLang.Equals("ar", StringComparison.OrdinalIgnoreCase)) countryCode = "SA";
                    else if (savedLang.Equals("en", StringComparison.OrdinalIgnoreCase)) countryCode = "GB";
                    else if (savedLang.Equals("zh", StringComparison.OrdinalIgnoreCase)) countryCode = "CN";
                    else if (savedLang.Equals("es", StringComparison.OrdinalIgnoreCase)) countryCode = "ES";
                    else if (savedLang.Equals("fr", StringComparison.OrdinalIgnoreCase)) countryCode = "FR";
                    else if (savedLang.Equals("ku", StringComparison.OrdinalIgnoreCase)) countryCode = "TR";
                    HolidayService.CurrentCountryCode = countryCode;
                    DeviceSyncConfigWindow.SelectedDeviceClockCountryCode = countryCode;
                    DeviceSyncConfigWindow.SelectedHolidayCountryCode = countryCode;
                    DeviceSyncConfigWindow.SelectedCalendarCountryCode = countryCode;
                }

                // Son seçili Yıl ve Ay filtresini geri yükle
                string savedYearStr = dbService.GetSetting("SelectedYear", null);
                if (int.TryParse(savedYearStr, out int sYear) && cbReportYear != null)
                {
                    if (!cbReportYear.Items.Contains(sYear)) cbReportYear.Items.Add(sYear);
                    cbReportYear.SelectedItem = sYear;
                }

                string savedMonthStr = dbService.GetSetting("SelectedMonthIndex", null);
                if (int.TryParse(savedMonthStr, out int sMonthIdx) && cbReportMonth != null && sMonthIdx >= 0 && sMonthIdx < cbReportMonth.Items.Count)
                {
                    cbReportMonth.SelectedIndex = sMonthIdx;
                }

                ApplyLocalization();
                CalculateShiftReport();
            }
            catch (Exception ex)
            {
                Logger.LogException("RestoreSavedLanguageAndCountry", ex);
            }
        }

        private void CbLanguage_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cbLanguage?.SelectedItem is System.Windows.Controls.ComboBoxItem item && item.Tag != null)
            {
                string lang = item.Tag.ToString();
                LocalizationService.CurrentLanguage = lang;

                // Seçilen dile göre varsayılan resmi tatil ve saat ülkesini de senkronize et:
                // tr -> TR, de -> DE, ru -> RU, ar -> SA, ku -> TR, en -> GB
                string countryCode = "TR";
                if (lang.Equals("de", StringComparison.OrdinalIgnoreCase)) countryCode = "DE";
                else if (lang.Equals("ru", StringComparison.OrdinalIgnoreCase)) countryCode = "RU";
                else if (lang.Equals("ar", StringComparison.OrdinalIgnoreCase)) countryCode = "SA";
                else if (lang.Equals("en", StringComparison.OrdinalIgnoreCase)) countryCode = "GB";
                else if (lang.Equals("zh", StringComparison.OrdinalIgnoreCase)) countryCode = "CN";
                else if (lang.Equals("es", StringComparison.OrdinalIgnoreCase)) countryCode = "ES";
                else if (lang.Equals("fr", StringComparison.OrdinalIgnoreCase)) countryCode = "FR";
                else if (lang.Equals("ku", StringComparison.OrdinalIgnoreCase)) countryCode = "TR";
                else countryCode = "TR";

                HolidayService.CurrentCountryCode = countryCode;
                DeviceSyncConfigWindow.SelectedDeviceClockCountryCode = countryCode;
                DeviceSyncConfigWindow.SelectedHolidayCountryCode = countryCode;
                DeviceSyncConfigWindow.SelectedCalendarCountryCode = countryCode;

                if (dbService != null)
                {
                    dbService.SetSetting("SelectedLanguage", lang);
                    dbService.SetSetting("SelectedCountry", countryCode);
                }

                ApplyLocalization();

                // Yeni ülkeye göre tatilleri ve raporu arka planda yenile
                try
                {
                    int year = (cbReportYear?.SelectedItem is int y) ? y : DateTime.Now.Year;
                    _ = System.Threading.Tasks.Task.Run(() =>
                    {
                        HolidayService.GetHolidaysForYear(year, dbService, forceRefresh: false, countryCode: countryCode);
                    }).ContinueWith(t =>
                    {
                        Dispatcher.Invoke(() => CalculateShiftReport());
                    });
                }
                catch { }
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
                if (btnShiftSchedule != null) btnShiftSchedule.Content = LocalizationService.Get("BtnShiftSchedule");
                if (btnIrregularPunches != null) btnIrregularPunches.Content = LocalizationService.Get("BtnIrregularPunches");

                // Üst Başlıklar
                Title = LocalizationService.Get("AppTitle");
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

                // Filtre Etiketleri & Butonları
                if (lblYear != null) lblYear.Text = LocalizationService.Get("Year");
                if (lblMonth != null) lblMonth.Text = LocalizationService.Get("Month");
                if (lblDay != null) lblDay.Text = LocalizationService.Get("Day");
                if (lblGroup != null) lblGroup.Text = LocalizationService.Get("Group");
                if (lblType != null) lblType.Text = LocalizationService.Get("Type");
                if (lblStatus != null) lblStatus.Text = LocalizationService.Get("Status");

                if (txtBtnToday != null) txtBtnToday.Text = LocalizationService.Get("BtnToday");
                if (chkUseDateRange != null) chkUseDateRange.Content = LocalizationService.Get("ChkUseDateRange");
                if (txtBtnLatestPeriod != null) txtBtnLatestPeriod.Text = LocalizationService.Get("BtnLatestPeriod");
                UpdateCountryButtonLabels();
                // Handled by UpdateCountryButtonLabels
                if (btnCalculate != null) btnCalculate.Content = LocalizationService.Get("BtnCalculate");
                if (txtBtnClearFilters != null) txtBtnClearFilters.Text = LocalizationService.Get("BtnClearFilters");
                if (btnExportMonthly != null) btnExportMonthly.Content = LocalizationService.Get("BtnExportMonthly");
                if (btnExportDaily != null) btnExportDaily.Content = LocalizationService.Get("BtnExportDaily");
                if (btnRefreshDb != null) btnRefreshDb.Content = LocalizationService.Get("BtnRefreshDb");
                if (btnExportAllLogs != null) btnExportAllLogs.Content = LocalizationService.Get("BtnExportAllLogs");
                if (btnExportEmpDetail != null) btnExportEmpDetail.Content = LocalizationService.Get("BtnExportEmpDetail");
                if (btnRefreshEmpDetail != null) btnRefreshEmpDetail.Content = LocalizationService.Get("BtnRefreshEmpDetail");

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
                if (txtLogSearchUser != null) MaterialDesignThemes.Wpf.HintAssist.SetHint(txtLogSearchUser, LocalizationService.Get("SearchName"));

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

                // Log Tipi ve Doğrulama ComboBox Öğelerini Güncelle
                if (cbLogTypeFilter != null && cbLogTypeFilter.Items.Count >= 6)
                {
                    ((System.Windows.Controls.ComboBoxItem)cbLogTypeFilter.Items[0]).Content = LocalizationService.Get("LogType_All");
                    ((System.Windows.Controls.ComboBoxItem)cbLogTypeFilter.Items[1]).Content = LocalizationService.Get("LogType_In");
                    ((System.Windows.Controls.ComboBoxItem)cbLogTypeFilter.Items[2]).Content = LocalizationService.Get("LogType_Out");
                    ((System.Windows.Controls.ComboBoxItem)cbLogTypeFilter.Items[3]).Content = LocalizationService.Get("LogType_BreakIn");
                    ((System.Windows.Controls.ComboBoxItem)cbLogTypeFilter.Items[4]).Content = LocalizationService.Get("LogType_BreakOut");
                    ((System.Windows.Controls.ComboBoxItem)cbLogTypeFilter.Items[5]).Content = LocalizationService.Get("LogType_OtIn");
                }

                if (cbLogVerifyFilter != null && cbLogVerifyFilter.Items.Count >= 5)
                {
                    ((System.Windows.Controls.ComboBoxItem)cbLogVerifyFilter.Items[0]).Content = LocalizationService.Get("LogVerify_All");
                    ((System.Windows.Controls.ComboBoxItem)cbLogVerifyFilter.Items[1]).Content = LocalizationService.Get("LogVerify_Finger");
                    ((System.Windows.Controls.ComboBoxItem)cbLogVerifyFilter.Items[2]).Content = LocalizationService.Get("LogVerify_Card");
                    ((System.Windows.Controls.ComboBoxItem)cbLogVerifyFilter.Items[3]).Content = LocalizationService.Get("LogVerify_Face");
                    ((System.Windows.Controls.ComboBoxItem)cbLogVerifyFilter.Items[4]).Content = LocalizationService.Get("LogVerify_Pwd");
                }

                // DataGrid Sütun Başlıklarını Güncelle
                ApplyDataGridLocalization();
                if (txtBtnDeviceSyncConfig != null) txtBtnDeviceSyncConfig.Text = LocalizationService.Get("BtnDeviceSyncConfig");
                if (activeDeviceSyncConfigWindow != null) activeDeviceSyncConfigWindow.ApplyLocalization();

                // Grup ve Tür filtrelerinin ilk elemanını güncelle
                LoadGroupFilter();
                LoadEmploymentTypeFilter();

                // Ay ComboBox'ını mevcut dile göre güncelle (seçili index korunur)
                int currentMonthIdx = cbReportMonth?.SelectedIndex ?? 0;
                PopulateMonthCombobox(currentMonthIdx);

                // KPI Kartı Personel ve Hareket Sayısı Güncelle
                if (txtEmpCount != null) txtEmpCount.Text = $"{currentEmployees.Count} {LocalizationService.Get("Unit_Person")}";
                if (zkService != null && zkService.IsConnected)
                {
                    txtConnectionStatus.Text = LocalizationService.Get("Conn_Active");
                }
                else
                {
                    txtConnectionStatus.Text = LocalizationService.Get("Conn_NoConnection");
                    txtDeviceTime.Text = LocalizationService.Get("Conn_NoConnection");
                }

                UpdateSummaryCards();
                ApplyLogFilters();
            }
            catch (Exception ex)
            {
                Logger.LogException("ApplyLocalization", ex);
            }
        }

        private void PopulateMonthCombobox(int selectedIndex = 0)
        {
            if (cbReportMonth == null) return;

            int prevIndex = selectedIndex >= 0 ? selectedIndex : cbReportMonth.SelectedIndex;
            cbReportMonth.SelectionChanged -= CbReportDate_SelectionChanged;
            cbReportMonth.Items.Clear();

            // 0 - Tüm Aylar
            cbReportMonth.Items.Add(LocalizationService.Get("Month_All"));

            // 1..12 Aylar (aktif dildeki kültür adına göre formatlanır)
            System.Globalization.CultureInfo ci;
            try
            {
                string lang = LocalizationService.CurrentLanguage?.ToLowerInvariant() ?? "tr";
                string cultureName = lang == "en" ? "en-US" : (lang == "de" ? "de-DE" : (lang == "ru" ? "ru-RU" : (lang == "ar" ? "ar-SA" : (lang == "zh" ? "zh-CN" : (lang == "es" ? "es-ES" : (lang == "fr" ? "fr-FR" : (lang == "ku" ? "ku-TR" : "tr-TR")))))));
                ci = new System.Globalization.CultureInfo(cultureName);
            }
            catch
            {
                ci = System.Globalization.CultureInfo.CurrentCulture;
            }

            for (int m = 1; m <= 12; m++)
            {
                string monthName = ci.DateTimeFormat.GetMonthName(m);
                if (!string.IsNullOrEmpty(monthName))
                {
                    monthName = char.ToUpper(monthName[0]) + monthName.Substring(1);
                }
                cbReportMonth.Items.Add($"{m} - {monthName}");
            }

            if (prevIndex >= 0 && prevIndex < cbReportMonth.Items.Count)
            {
                cbReportMonth.SelectedIndex = prevIndex;
            }
            else
            {
                cbReportMonth.SelectedIndex = 0;
            }

            cbReportMonth.SelectionChanged += CbReportDate_SelectionChanged;
        }

        private void ApplyDataGridLocalization()
        {
            try
            {
                // dgMonthlyReport (32 Sütun)
                if (dgMonthlyReport != null && dgMonthlyReport.Columns.Count >= 32)
                {
                    dgMonthlyReport.Columns[0].Header = LocalizationService.Get("Col_Id");
                    dgMonthlyReport.Columns[1].Header = LocalizationService.Get("Col_Name");
                    dgMonthlyReport.Columns[2].Header = LocalizationService.Get("Col_Tc");
                    dgMonthlyReport.Columns[3].Header = LocalizationService.Get("Col_Status");
                    dgMonthlyReport.Columns[4].Header = LocalizationService.Get("Col_Group");
                    dgMonthlyReport.Columns[5].Header = LocalizationService.Get("Col_Type");
                    dgMonthlyReport.Columns[6].Header = LocalizationService.Get("Col_Phone");
                    dgMonthlyReport.Columns[7].Header = LocalizationService.Get("Col_CompanyPhone");
                    dgMonthlyReport.Columns[8].Header = LocalizationService.Get("Col_HireDate");
                    dgMonthlyReport.Columns[9].Header = LocalizationService.Get("Col_LeaveDate");
                    dgMonthlyReport.Columns[10].Header = LocalizationService.Get("Col_Dept");
                    dgMonthlyReport.Columns[11].Header = LocalizationService.Get("Col_Title");
                    dgMonthlyReport.Columns[12].Header = LocalizationService.Get("Col_Shift");
                    dgMonthlyReport.Columns[13].Header = LocalizationService.Get("Col_Date");
                    dgMonthlyReport.Columns[14].Header = LocalizationService.Get("Col_Day");
                    dgMonthlyReport.Columns[15].Header = LocalizationService.Get("Col_FirstIn");
                    dgMonthlyReport.Columns[16].Header = LocalizationService.Get("Col_LastOut");
                    dgMonthlyReport.Columns[17].Header = LocalizationService.Get("Col_Pre12Work");
                    dgMonthlyReport.Columns[18].Header = LocalizationService.Get("Col_Post12Night");
                    dgMonthlyReport.Columns[19].Header = LocalizationService.Get("Col_BreakMin");
                    dgMonthlyReport.Columns[20].Header = LocalizationService.Get("Col_ExcessBreakMin");
                    dgMonthlyReport.Columns[21].Header = LocalizationService.Get("Col_NetWork");
                    dgMonthlyReport.Columns[22].Header = LocalizationService.Get("Col_MorningOt");
                    dgMonthlyReport.Columns[23].Header = LocalizationService.Get("Col_EveningOt");
                    dgMonthlyReport.Columns[24].Header = LocalizationService.Get("Col_NightOt");
                    dgMonthlyReport.Columns[25].Header = LocalizationService.Get("Col_RawOt");
                    dgMonthlyReport.Columns[26].Header = LocalizationService.Get("Col_Multiplier");
                    dgMonthlyReport.Columns[27].Header = LocalizationService.Get("Col_WeightedOt");
                    dgMonthlyReport.Columns[28].Header = LocalizationService.Get("Col_RejectedOt");
                    dgMonthlyReport.Columns[29].Header = LocalizationService.Get("Col_LateMin");
                    dgMonthlyReport.Columns[30].Header = LocalizationService.Get("Col_Missing");
                    dgMonthlyReport.Columns[31].Header = LocalizationService.Get("Col_AttendanceStatus");
                }

                // dgLogs (5 Sütun)
                if (dgLogs != null && dgLogs.Columns.Count >= 5)
                {
                    dgLogs.Columns[0].Header = LocalizationService.Get("Col_LogUserId");
                    dgLogs.Columns[1].Header = LocalizationService.Get("Col_LogUserName");
                    dgLogs.Columns[2].Header = LocalizationService.Get("Col_LogAction");
                    dgLogs.Columns[3].Header = LocalizationService.Get("Col_LogTime");
                    dgLogs.Columns[4].Header = LocalizationService.Get("Col_LogVerify");
                }

                // dgEmpDailyDetails (18 Sütun)
                if (dgEmpDailyDetails != null && dgEmpDailyDetails.Columns.Count >= 18)
                {
                    dgEmpDailyDetails.Columns[0].Header = LocalizationService.Get("Col_Date");
                    dgEmpDailyDetails.Columns[1].Header = LocalizationService.Get("Col_Day");
                    dgEmpDailyDetails.Columns[2].Header = LocalizationService.Get("Col_FirstIn");
                    dgEmpDailyDetails.Columns[3].Header = LocalizationService.Get("Col_LastOut");
                    dgEmpDailyDetails.Columns[4].Header = LocalizationService.Get("Col_Pre12Work");
                    dgEmpDailyDetails.Columns[5].Header = LocalizationService.Get("Col_Post12Night");
                    dgEmpDailyDetails.Columns[6].Header = LocalizationService.Get("Col_BreakMinShort");
                    dgEmpDailyDetails.Columns[7].Header = LocalizationService.Get("Col_ExcessBreakMin");
                    dgEmpDailyDetails.Columns[8].Header = LocalizationService.Get("Col_NetWork");
                    dgEmpDailyDetails.Columns[9].Header = LocalizationService.Get("Col_MorningOt");
                    dgEmpDailyDetails.Columns[10].Header = LocalizationService.Get("Col_EveningOt");
                    dgEmpDailyDetails.Columns[11].Header = LocalizationService.Get("Col_NightOt");
                    dgEmpDailyDetails.Columns[12].Header = LocalizationService.Get("Col_RawOt");
                    dgEmpDailyDetails.Columns[13].Header = LocalizationService.Get("Col_Multiplier");
                    dgEmpDailyDetails.Columns[14].Header = LocalizationService.Get("Col_WeightedOt");
                    dgEmpDailyDetails.Columns[15].Header = LocalizationService.Get("Col_LateMin");
                    dgEmpDailyDetails.Columns[16].Header = LocalizationService.Get("Col_Missing");
                    dgEmpDailyDetails.Columns[17].Header = LocalizationService.Get("Col_DailyAttendanceStatus");
                }
            }
            catch (Exception ex)
            {
                Logger.LogException("ApplyDataGridLocalization", ex);
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
            cbReportGroup.Items.Add(LocalizationService.Get("FilterAllGroups"));
            
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
            cbReportEmploymentTypeFilter.Items.Add(LocalizationService.Get("FilterAllTypes"));

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

                if (txtEmpCount != null) txtEmpCount.Text = $"{currentEmployees.Count} {LocalizationService.Get("Unit_Person")}";

                var logs = dbService.GetAllLogs();
                LogList.Clear();
                foreach (var log in logs)
                {
                    LogList.Add(log);
                }

                ApplyLogFilters();
                CalculateShiftReport();

                if (txtStatus != null) txtStatus.Text = string.Format(LocalizationService.Get("Unit_PassCount"), logs.Count);
            }
            catch (Exception ex)
            {
                Logger.LogException("RefreshAllData", ex);
            }
        }

        public void CalculateShiftReport()
        {
            if (cbReportYear.SelectedItem == null || cbReportMonth.SelectedIndex < 0) return;

            int year = (int)cbReportYear.SelectedItem;
            // cbReportMonth.SelectedIndex == 0 -> Tüm Aylar (month = 0)
            // cbReportMonth.SelectedIndex >= 1 -> 1..12 Aylar (month = cbReportMonth.SelectedIndex)
            int month = cbReportMonth.SelectedIndex;

            var shiftsToUse = currentShifts;

            string selectedGroup = cbReportGroup?.SelectedItem?.ToString() ?? "Tüm Gruplar";
            string selectedEmpType = cbReportEmploymentTypeFilter?.SelectedItem?.ToString() ?? "Tüm Türler";
            var filteredEmployees = currentEmployees;

            // 1. Grup Filtresi (Örn: Üretim, Montaj, Yönetim)
            if (cbReportGroup != null && cbReportGroup.SelectedIndex > 0 && !string.IsNullOrEmpty(selectedGroup))
            {
                filteredEmployees = filteredEmployees.Where(e => (e.GroupName ?? "Genel").Trim().Equals(selectedGroup.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // 2. İstihdam Türü Filtresi (Örn: Kadrolu, Taşeron, Sözleşmeli)
            if (cbReportEmploymentTypeFilter != null && cbReportEmploymentTypeFilter.SelectedIndex > 0 && !string.IsNullOrEmpty(selectedEmpType))
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
                rangeStart = dpStartDate?.SelectedDate ?? new DateTime(year, month > 0 ? month : 1, 1);
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

        public void UpdateDeviceTimeDisplay()
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
                txtDeviceTime.Text = LocalizationService.Get("Conn_NoConnection");
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
                    timeResult = HolidayService.GetCurrentCountryDateTimeSafe();
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

            txtConnectionStatus.Text = LocalizationService.Get("Conn_Connecting");
            txtConnectionStatus.Foreground = System.Windows.Media.Brushes.Orange;

            try
            {
                bool success = await Task.Run(() => zkService.Connect(ip, port));
                if (success)
                {
                    txtConnectionStatus.Text = LocalizationService.Get("Conn_Active");
                    txtConnectionStatus.Foreground = System.Windows.Media.Brushes.LightGreen;
                    UpdateDeviceTimeDisplay();
                    MessageBox.Show(LocalizationService.Get("Msg_ConnectedSuccess"), LocalizationService.Get("MsgTitle_Connected"), MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    txtConnectionStatus.Text = LocalizationService.Get("Conn_Failed");
                    txtConnectionStatus.Foreground = System.Windows.Media.Brushes.IndianRed;
                    MessageBox.Show(LocalizationService.Get("Msg_ConnFailed"), LocalizationService.Get("MsgTitle_ConnError"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                txtConnectionStatus.Text = LocalizationService.Get("Conn_Error");
                txtConnectionStatus.Foreground = System.Windows.Media.Brushes.IndianRed;
                Logger.LogException("BtnConnect_Click", ex);
                MessageBox.Show("Bağlantı sırasında hata oluştu:\n" + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnGetLogs_Click(object sender, RoutedEventArgs e)
        {
            if (!zkService.IsConnected)
            {
                MessageBox.Show(LocalizationService.Get("Msg_PleaseConnectFirst"), LocalizationService.Get("MsgTitle_Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
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
                MessageBox.Show(string.Format(LocalizationService.Get("Msg_LogsFetchedSuccess"), fetchedList.Count), LocalizationService.Get("MsgTitle_Success"), MessageBoxButton.OK, MessageBoxImage.Information);
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

            int logTypeIdx = cbLogTypeFilter != null ? cbLogTypeFilter.SelectedIndex : 0;
            if (logTypeIdx > 0)
            {
                if (logTypeIdx == 1) query = query.Where(l => !string.IsNullOrEmpty(l.InOutMode) && (l.InOutMode.Contains("Giriş") || l.InOutMode.Contains("In") || l.InOutMode.Contains("Eingang") || l.InOutMode.Contains("Ketin") || l.InOutMode.Contains("Вход") || l.InOutMode.Contains("دخول")));
                else if (logTypeIdx == 2) query = query.Where(l => !string.IsNullOrEmpty(l.InOutMode) && (l.InOutMode.Contains("Çıkış") || l.InOutMode.Contains("Out") || l.InOutMode.Contains("Ausgang") || l.InOutMode.Contains("Derketin") || l.InOutMode.Contains("Выход") || l.InOutMode.Contains("خروج")));
                else if (!string.IsNullOrEmpty(selectedType)) query = query.Where(l => !string.IsNullOrEmpty(l.InOutMode) && l.InOutMode.IndexOf(selectedType, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            int logVerifyIdx = cbLogVerifyFilter != null ? cbLogVerifyFilter.SelectedIndex : 0;
            if (logVerifyIdx > 0)
            {
                if (logVerifyIdx == 1) query = query.Where(l => !string.IsNullOrEmpty(l.VerifyMode) && (l.VerifyMode.Contains("Parmak") || l.VerifyMode.Contains("Finger") || l.VerifyMode.Contains("Tilî") || l.VerifyMode.Contains("Палец") || l.VerifyMode.Contains("بصمة")));
                else if (logVerifyIdx == 2) query = query.Where(l => !string.IsNullOrEmpty(l.VerifyMode) && (l.VerifyMode.Contains("Kart") || l.VerifyMode.Contains("Card") || l.VerifyMode.Contains("Karte") || l.VerifyMode.Contains("Карта") || l.VerifyMode.Contains("بطاقة")));
                else if (logVerifyIdx == 3) query = query.Where(l => !string.IsNullOrEmpty(l.VerifyMode) && (l.VerifyMode.Contains("Yüz") || l.VerifyMode.Contains("Face") || l.VerifyMode.Contains("Gesicht") || l.VerifyMode.Contains("Rû") || l.VerifyMode.Contains("Лицо") || l.VerifyMode.Contains("وجه")));
                else if (logVerifyIdx == 4) query = query.Where(l => !string.IsNullOrEmpty(l.VerifyMode) && (l.VerifyMode.Contains("Şifre") || l.VerifyMode.Contains("Password") || l.VerifyMode.Contains("Passwort") || l.VerifyMode.Contains("Şîfre") || l.VerifyMode.Contains("Пароль") || l.VerifyMode.Contains("كلمة")));
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
                txtFilteredLogCount.Text = string.Format(LocalizationService.Get("Unit_PunchCount"), count);
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
                    MessageBox.Show(LocalizationService.Get("Msg_NoDataToExport"), LocalizationService.Get("MsgTitle_Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int year = (cbReportYear != null && cbReportYear.SelectedItem != null) 
                    ? (int)cbReportYear.SelectedItem 
                    : DateTime.Now.Year;
                int month = (cbReportMonth != null && cbReportMonth.SelectedIndex >= 0) 
                    ? cbReportMonth.SelectedIndex 
                    : DateTime.Now.Month;
                string monthSuffix = month == 0 ? "Tum_Yil" : month.ToString("D2");

                string lastDir = dbService?.GetSetting("LastExportDirectory", null);
            var sfd = new SaveFileDialog
            {
                InitialDirectory = (!string.IsNullOrEmpty(lastDir) && System.IO.Directory.Exists(lastDir)) ? lastDir : Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    Filter = "Excel Dosyası (*.xlsx)|*.xlsx",
                    FileName = $"ZKTeco_Gece_Gunduz_Mesai_Raporu_{year}_{monthSuffix}.xlsx"
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
                            try { dbService?.SetSetting("LastExportDirectory", System.IO.Path.GetDirectoryName(sfd.FileName)); } catch {}
                        }

                        MessageBox.Show(string.Format(LocalizationService.Get("Msg_ExcelExportSuccess"), sfd.FileName), LocalizationService.Get("MsgTitle_Success"), MessageBoxButton.OK, MessageBoxImage.Information);
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

            string lastDir = dbService?.GetSetting("LastExportDirectory", null);
            var sfd = new SaveFileDialog
            {
                InitialDirectory = (!string.IsNullOrEmpty(lastDir) && System.IO.Directory.Exists(lastDir)) ? lastDir : Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
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
                            try { dbService?.SetSetting("LastExportDirectory", System.IO.Path.GetDirectoryName(sfd.FileName)); } catch {}
                    }

                    MessageBox.Show(string.Format(LocalizationService.Get("Msg_ExcelExportSuccess"), sfd.FileName), LocalizationService.Get("MsgTitle_Success"), MessageBoxButton.OK, MessageBoxImage.Information);
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
                int month = (cbReportMonth != null && cbReportMonth.SelectedIndex >= 0) ? cbReportMonth.SelectedIndex : DateTime.Now.Month;
                var holidays = HolidayService.GetHolidaysForYear(year, dbService);

                DateTime? rangeStart = null;
                DateTime? rangeEnd = null;
                if (chkUseDateRange != null && chkUseDateRange.IsChecked == true)
                {
                    rangeStart = dpStartDate?.SelectedDate ?? new DateTime(year, month > 0 ? month : 1, 1);
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
                MessageBox.Show(LocalizationService.Get("Msg_SelectEmployeeFirst"), LocalizationService.Get("MsgTitle_Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var emp = cbSelectedEmployeeCard.SelectedItem as Employee;
            if (emp == null || SingleEmpDailyList.Count == 0)
            {
                MessageBox.Show(LocalizationService.Get("Msg_NoDailyLogsForEmp"), LocalizationService.Get("MsgTitle_Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int year = (int)(cbReportYear.SelectedItem ?? DateTime.Now.Year);
            int month = cbReportMonth.SelectedIndex;
            string monthSuffix = month == 0 ? "Tum_Yil" : month.ToString("D2");

            string lastDir = dbService?.GetSetting("LastExportDirectory", null);
            var sfd = new SaveFileDialog
            {
                InitialDirectory = (!string.IsNullOrEmpty(lastDir) && System.IO.Directory.Exists(lastDir)) ? lastDir : Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Filter = "Excel Dosyası (*.xlsx)|*.xlsx",
                FileName = $"PDKS_Gunluk_Puantaj_{emp.Name.Replace(" ", "_")}_{year}_{monthSuffix}.xlsx"
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
                            try { dbService?.SetSetting("LastExportDirectory", System.IO.Path.GetDirectoryName(sfd.FileName)); } catch {}
                    }

                    MessageBox.Show(string.Format(LocalizationService.Get("Msg_ExcelExportSuccess"), sfd.FileName), LocalizationService.Get("MsgTitle_Success"), MessageBoxButton.OK, MessageBoxImage.Information);
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
                MessageBox.Show(LocalizationService.Get("Msg_NoDataToExport"), LocalizationService.Get("MsgTitle_Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int year = (int)(cbReportYear.SelectedItem ?? DateTime.Now.Year);
            int month = cbReportMonth.SelectedIndex;
            string monthSuffix = month == 0 ? "Tum_Yil" : month.ToString("D2");

            string lastDir = dbService?.GetSetting("LastExportDirectory", null);
            var sfd = new SaveFileDialog
            {
                InitialDirectory = (!string.IsNullOrEmpty(lastDir) && System.IO.Directory.Exists(lastDir)) ? lastDir : Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Filter = "Excel Dosyası (*.xlsx)|*.xlsx",
                FileName = $"PDKS_Tum_Personel_Gun_Gun_Puantaj_{year}_{monthSuffix}.xlsx"
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
                            try { dbService?.SetSetting("LastExportDirectory", System.IO.Path.GetDirectoryName(sfd.FileName)); } catch {}
                    }

                    MessageBox.Show(string.Format(LocalizationService.Get("Msg_ExcelExportSuccess"), sfd.FileName), LocalizationService.Get("MsgTitle_Success"), MessageBoxButton.OK, MessageBoxImage.Information);
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
                case "DeviceSyncConfig":
                    title = LocalizationService.Get("HelpTitle_DeviceSyncConfig");
                    desc = LocalizationService.Get("HelpDesc_DeviceSyncConfig");
                    tip = LocalizationService.Get("HelpTip_DeviceSyncConfig");
                    break;
                case "IpPort":
                    title = LocalizationService.Get("HelpTitle_IpPort");
                    desc = LocalizationService.Get("HelpDesc_IpPort");
                    tip = LocalizationService.Get("HelpTip_IpPort");
                    break;
                case "Connect":
                    title = LocalizationService.Get("HelpTitle_Connect");
                    desc = LocalizationService.Get("HelpDesc_Connect");
                    tip = LocalizationService.Get("HelpTip_Connect");
                    break;
                case "GetLogs":
                    title = LocalizationService.Get("HelpTitle_GetLogs");
                    desc = LocalizationService.Get("HelpDesc_GetLogs");
                    tip = LocalizationService.Get("HelpTip_GetLogs");
                    break;
                case "SyncDeviceTime":
                    title = LocalizationService.Get("HelpTitle_SyncDeviceTime");
                    desc = LocalizationService.Get("HelpDesc_SyncDeviceTime");
                    tip = LocalizationService.Get("HelpTip_SyncDeviceTime");
                    break;
                case "Employees":
                    title = LocalizationService.Get("HelpTitle_Employees");
                    desc = LocalizationService.Get("HelpDesc_Employees");
                    tip = LocalizationService.Get("HelpTip_Employees");
                    break;
                case "Groups":
                    title = LocalizationService.Get("HelpTitle_Groups");
                    desc = LocalizationService.Get("HelpDesc_Groups");
                    tip = LocalizationService.Get("HelpTip_Groups");
                    break;
                case "Shifts":
                    title = LocalizationService.Get("HelpTitle_Shifts");
                    desc = LocalizationService.Get("HelpDesc_Shifts");
                    tip = LocalizationService.Get("HelpTip_Shifts");
                    break;
                case "PeriodYear":
                    title = LocalizationService.Get("HelpTitle_PeriodYear");
                    desc = LocalizationService.Get("HelpDesc_PeriodYear");
                    break;
                case "DateRange":
                    title = LocalizationService.Get("HelpTitle_DateRange");
                    desc = LocalizationService.Get("HelpDesc_DateRange");
                    tip = LocalizationService.Get("HelpTip_DateRange");
                    break;
                case "TodayFilter":
                    title = LocalizationService.Get("HelpTitle_TodayFilter");
                    desc = LocalizationService.Get("HelpDesc_TodayFilter");
                    break;
                case "RefreshDb":
                    title = LocalizationService.Get("HelpTitle_RefreshDb");
                    desc = LocalizationService.Get("HelpDesc_RefreshDb");
                    break;
                case "ExportAllLogs":
                    title = LocalizationService.Get("HelpTitle_ExportAllLogs");
                    desc = LocalizationService.Get("HelpDesc_ExportAllLogs");
                    break;
                case "PeriodMonth":
                    title = LocalizationService.Get("HelpTitle_PeriodMonth");
                    desc = LocalizationService.Get("HelpDesc_PeriodMonth");
                    tip = LocalizationService.Get("HelpTip_PeriodMonth");
                    break;
                case "DayFilter":
                    title = LocalizationService.Get("HelpTitle_DayFilter");
                    desc = LocalizationService.Get("HelpDesc_DayFilter");
                    tip = LocalizationService.Get("HelpTip_DayFilter");
                    break;
                case "SyncLatestPeriod":
                    title = LocalizationService.Get("HelpTitle_SyncLatestPeriod");
                    desc = LocalizationService.Get("HelpDesc_SyncLatestPeriod");
                    tip = LocalizationService.Get("HelpTip_SyncLatestPeriod");
                    break;
                case "SyncInternetTimeBtn":
                    title = LocalizationService.Get("HelpTitle_SyncInternetTimeBtn");
                    desc = LocalizationService.Get("HelpDesc_SyncInternetTimeBtn");
                    break;
                case "RefreshHolidays":
                    title = LocalizationService.Get("HelpTitle_RefreshHolidays");
                    desc = LocalizationService.Get("HelpDesc_RefreshHolidays");
                    tip = LocalizationService.Get("HelpTip_RefreshHolidays");
                    break;
                case "GroupFilter":
                    title = LocalizationService.Get("HelpTitle_GroupFilter");
                    desc = LocalizationService.Get("HelpDesc_GroupFilter");
                    break;
                case "TypeFilter":
                    title = LocalizationService.Get("HelpTitle_TypeFilter");
                    desc = LocalizationService.Get("HelpDesc_TypeFilter");
                    break;
                case "StatusFilter":
                    title = LocalizationService.Get("HelpTitle_StatusFilter");
                    desc = LocalizationService.Get("HelpDesc_StatusFilter");
                    break;
                case "SearchNameTc":
                    title = LocalizationService.Get("HelpTitle_SearchNameTc");
                    desc = LocalizationService.Get("HelpDesc_SearchNameTc");
                    break;
                case "Calculate":
                    title = LocalizationService.Get("HelpTitle_Calculate");
                    desc = LocalizationService.Get("HelpDesc_Calculate");
                    tip = LocalizationService.Get("HelpTip_Calculate");
                    break;
                case "ClearFilters":
                    title = LocalizationService.Get("HelpTitle_ClearFilters");
                    desc = LocalizationService.Get("HelpDesc_ClearFilters");
                    break;
                case "ExportMonthly":
                    title = LocalizationService.Get("HelpTitle_ExportMonthly");
                    desc = LocalizationService.Get("HelpDesc_ExportMonthly");
                    tip = LocalizationService.Get("HelpTip_ExportMonthly");
                    break;
                case "ExportDaily":
                    title = LocalizationService.Get("HelpTitle_ExportDaily");
                    desc = LocalizationService.Get("HelpDesc_ExportDaily");
                    break;
                case "EmpCardSelect":
                    title = LocalizationService.Get("HelpTitle_EmpCardSelect");
                    desc = LocalizationService.Get("HelpDesc_EmpCardSelect");
                    break;
                case "ExportEmpDetail":
                    title = LocalizationService.Get("HelpTitle_ExportEmpDetail");
                    desc = LocalizationService.Get("HelpDesc_ExportEmpDetail");
                    break;
                case "ShiftSchedule":
                    title = LocalizationService.Get("HelpTitle_ShiftSchedule");
                    desc = LocalizationService.Get("HelpDesc_ShiftSchedule");
                    tip = LocalizationService.Get("HelpTip_ShiftSchedule");
                    break;
                case "IrregularPunches":
                    title = LocalizationService.Get("HelpTitle_IrregularPunches");
                    desc = LocalizationService.Get("HelpDesc_IrregularPunches");
                    tip = LocalizationService.Get("HelpTip_IrregularPunches");
                    break;
                default:
                    title = LocalizationService.Get("HelpTitle_General");
                    desc = LocalizationService.Get("HelpDesc_General");
                    break;
            }
            HelpInfoWindow.ShowHelp(this, title, desc, tip);
        }
    }
}
