using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace ZkPdksApp
{
    public partial class DeviceSyncConfigWindow : Window
    {
        private readonly MainWindow mainWindow;
        private readonly DispatcherTimer previewTimer;

        public static string SelectedDeviceClockCountryCode { get; set; } = "TR";
        public static string SelectedHolidayCountryCode { get; set; } = "TR";
        public static string SelectedCalendarCountryCode { get; set; } = "TR";

        public DeviceSyncConfigWindow(MainWindow main)
        {
            InitializeComponent();
            mainWindow = main;
            Owner = main;

            PopulateComboboxes();
            ApplyLocalization();

            previewTimer = new DispatcherTimer();
            previewTimer.Interval = TimeSpan.FromSeconds(1);
            previewTimer.Tick += (s, e) => UpdateClockPreview();
            previewTimer.Start();

            Closed += (s, e) => previewTimer.Stop();

            PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape) Close();
            };
        }

        private void PopulateComboboxes()
        {
            var countries = HolidayService.SupportedCountries;

            cbDeviceClockCountry.ItemsSource = null;
            cbDeviceClockCountry.ItemsSource = countries;
            cbDeviceClockCountry.DisplayMemberPath = "LocalizedName";
            cbDeviceClockCountry.SelectedValuePath = "Code";
            cbDeviceClockCountry.SelectedValue = SelectedDeviceClockCountryCode;

            cbHolidayCountry.ItemsSource = null;
            cbHolidayCountry.ItemsSource = countries;
            cbHolidayCountry.DisplayMemberPath = "LocalizedName";
            cbHolidayCountry.SelectedValuePath = "Code";
            cbHolidayCountry.SelectedValue = SelectedHolidayCountryCode;

            cbCalendarCountry.ItemsSource = null;
            cbCalendarCountry.ItemsSource = countries;
            cbCalendarCountry.DisplayMemberPath = "LocalizedName";
            cbCalendarCountry.SelectedValuePath = "Code";
            cbCalendarCountry.SelectedValue = SelectedCalendarCountryCode;

            UpdateClockPreview();
            UpdateLabelsText();
        }

        private void UpdateClockPreview()
        {
            try
            {
                string code = cbDeviceClockCountry?.SelectedValue?.ToString() ?? SelectedDeviceClockCountryCode;
                var country = HolidayService.SupportedCountries.Find(c => c.Code.Equals(code, StringComparison.OrdinalIgnoreCase)) ?? HolidayService.SupportedCountries[0];
                
                DateTime countryTime = HolidayService.GetCurrentCountryDateTime(code);
                string liveClockFormat = LocalizationService.Get("DeviceSyncLiveClockFormat");
                string timeZoneFormat = LocalizationService.Get("DeviceSyncTimeZoneFormat");

                if (lblClockPreview != null)
                {
                    lblClockPreview.Text = string.Format(liveClockFormat, $"{country.Flag} {country.LocalizedName}", countryTime);
                }
                if (lblClockInfo != null)
                {
                    string fallbackStr = (country.FallbackUtcOffsetHours >= 0 ? "+" : "") + country.FallbackUtcOffsetHours;
                    lblClockInfo.Text = string.Format(timeZoneFormat, country.TimeZoneId, fallbackStr);
                }
            }
            catch { }
        }

        private void UpdateLabelsText()
        {
            try
            {
                var holidayCountry = HolidayService.SupportedCountries.Find(c => c.Code.Equals(SelectedHolidayCountryCode, StringComparison.OrdinalIgnoreCase)) ?? HolidayService.SupportedCountries[0];
                if (lblHolidayInfo != null)
                {
                    string holidayInfoFormat = LocalizationService.Get("DeviceSyncHolidayInfoFormat");
                    lblHolidayInfo.Text = string.Format(holidayInfoFormat, $"{holidayCountry.Flag} {holidayCountry.LocalizedName}");
                }

                var calendarCountry = HolidayService.SupportedCountries.Find(c => c.Code.Equals(SelectedCalendarCountryCode, StringComparison.OrdinalIgnoreCase)) ?? HolidayService.SupportedCountries[0];
                if (lblCalendarInfo != null)
                {
                    string calendarInfoFormat = LocalizationService.Get("DeviceSyncCalendarInfoFormat");
                    lblCalendarInfo.Text = string.Format(calendarInfoFormat, $"{calendarCountry.Flag} {calendarCountry.LocalizedName}");
                }
            }
            catch { }
        }

        private void CbDeviceClockCountry_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbDeviceClockCountry?.SelectedValue != null)
            {
                SelectedDeviceClockCountryCode = cbDeviceClockCountry.SelectedValue.ToString();
                UpdateClockPreview();
            }
        }

        private void CbHolidayCountry_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbHolidayCountry?.SelectedValue != null)
            {
                SelectedHolidayCountryCode = cbHolidayCountry.SelectedValue.ToString();
                UpdateLabelsText();
            }
        }

        private void CbCalendarCountry_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbCalendarCountry?.SelectedValue != null)
            {
                SelectedCalendarCountryCode = cbCalendarCountry.SelectedValue.ToString();
                UpdateLabelsText();
            }
        }

        private async void BtnExecuteDeviceClockSync_Click(object sender, RoutedEventArgs e)
        {
            string code = cbDeviceClockCountry?.SelectedValue?.ToString() ?? SelectedDeviceClockCountryCode;
            var country = HolidayService.SupportedCountries.Find(c => c.Code.Equals(code, StringComparison.OrdinalIgnoreCase)) ?? HolidayService.SupportedCountries[0];

            if (mainWindow?.zkService == null || !mainWindow.zkService.IsConnected)
            {
                MessageBox.Show("Cihaz bağlı değil. Lütfen ana ekrandan önce cihaza bağlanın.", "Bağlantı Uyarısı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                (DateTime Time, bool IsFromInternet, string ErrorMessage) timeResult = (DateTime.Now, false, null);

                await Task.Run(() =>
                {
                    timeResult = HolidayService.GetCurrentCountryDateTimeSafe(code);
                });

                DateTime targetTime = timeResult.Time;
                bool success = mainWindow.zkService.SetDeviceTime(targetTime);

                if (success)
                {
                    mainWindow.UpdateDeviceTimeDisplay();
                    MessageBox.Show($"Cihaz saati başarıyla güncellendi!\n\n" +
                                    $"Hedef Ülke: {country.Flag} {country.Name}\n" +
                                    $"Saat Dilimi: {country.TimeZoneId}\n" +
                                    $"Yeni Cihaz Saati: {targetTime:yyyy-MM-dd HH:mm:ss}\n" +
                                    $"Kaynak: {(timeResult.IsFromInternet ? "Canlı İnternet Saati" : "Yerel Saat Dönüşümü")}", 
                                    "Cihaz Saati Eşitlendi", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Cihaza saat gönderilirken hata oluştu. Bağlantıyı kontrol edin.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException("BtnExecuteDeviceClockSync_Click", ex);
                MessageBox.Show("Hata oluştu:\n" + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private async void BtnExecuteHolidaySync_Click(object sender, RoutedEventArgs e)
        {
            string code = cbHolidayCountry?.SelectedValue?.ToString() ?? SelectedHolidayCountryCode;
            var country = HolidayService.SupportedCountries.Find(c => c.Code.Equals(code, StringComparison.OrdinalIgnoreCase)) ?? HolidayService.SupportedCountries[0];

            int year = mainWindow != null && mainWindow.cbReportYear?.SelectedItem != null
                ? (int)mainWindow.cbReportYear.SelectedItem 
                : DateTime.Now.Year;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                Dictionary<string, string> holidays = null;

                await Task.Run(() =>
                {
                    HolidayService.CurrentCountryCode = code;
                    holidays = HolidayService.GetHolidaysForYear(year, mainWindow?.dbService, forceRefresh: true, countryCode: code);
                });

                if (mainWindow != null)
                {
                    mainWindow.CalculateShiftReport();
                    mainWindow.UpdateCountryButtonLabels();
                }

                int count = holidays?.Count ?? 0;
                MessageBox.Show($"{country.Flag} {country.Name} ({year}) yılına ait {count} adet resmi tatil başarıyla çekildi ve puantaja yansıtıldı.", 
                    "Resmi Tatiller Güncellendi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Logger.LogException("BtnExecuteHolidaySync_Click", ex);
                MessageBox.Show("Tatiller çekilirken hata oluştu:\n" + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private async void BtnExecuteCalendarSync_Click(object sender, RoutedEventArgs e)
        {
            string code = cbCalendarCountry?.SelectedValue?.ToString() ?? SelectedCalendarCountryCode;
            var country = HolidayService.SupportedCountries.Find(c => c.Code.Equals(code, StringComparison.OrdinalIgnoreCase)) ?? HolidayService.SupportedCountries[0];

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                (DateTime Time, bool IsFromInternet, string ErrorMessage) timeResult = (DateTime.Now, false, null);

                await Task.Run(() =>
                {
                    timeResult = HolidayService.GetCurrentCountryDateTimeSafe(code);
                });

                DateTime targetTime = timeResult.Time;

                if (mainWindow != null)
                {
                    if (mainWindow.cbReportYear != null && mainWindow.cbReportYear.Items.Contains(targetTime.Year))
                    {
                        mainWindow.cbReportYear.SelectedItem = targetTime.Year;
                    }
                    if (mainWindow.cbReportMonth != null && mainWindow.cbReportMonth.Items.Count >= targetTime.Month)
                    {
                        mainWindow.cbReportMonth.SelectedIndex = targetTime.Month - 1;
                    }
                    if (mainWindow.dpStartDate != null) mainWindow.dpStartDate.SelectedDate = targetTime.Date;
                    if (mainWindow.dpEndDate != null) mainWindow.dpEndDate.SelectedDate = targetTime.Date;

                    mainWindow.CalculateShiftReport();
                    mainWindow.UpdateCountryButtonLabels();
                }

                MessageBox.Show($"Takvim ve Rapor odağı başarıyla güncellendi!\n\n" +
                                $"Hedef Ülke: {country.Flag} {country.Name}\n" +
                                $"Tarih / Saat: {targetTime:yyyy-MM-dd HH:mm:ss}\n" +
                                $"Odak Dönemi: {targetTime.Year} / {targetTime.Month}. Ay", 
                                "Takvim Eşitlendi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Logger.LogException("BtnExecuteCalendarSync_Click", ex);
                MessageBox.Show("Takvim güncellenirken hata oluştu:\n" + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void BtnSectionHelp_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.Tag is string tag)
            {
                string title = "Ayar Açıklaması";
                string desc = "";
                string tip = "";

                switch (tag)
                {
                    case "Help_DeviceClock":
                        title = LocalizationService.Get("HelpTitle_DeviceClock");
                        desc = LocalizationService.Get("HelpDesc_DeviceClock");
                        tip = LocalizationService.Get("HelpTip_DeviceClock");
                        break;
                    case "Help_Holidays":
                        title = LocalizationService.Get("HelpTitle_Holidays");
                        desc = LocalizationService.Get("HelpDesc_Holidays");
                        tip = LocalizationService.Get("HelpTip_Holidays");
                        break;
                    case "Help_Calendar":
                        title = LocalizationService.Get("HelpTitle_Calendar");
                        desc = LocalizationService.Get("HelpDesc_Calendar");
                        tip = LocalizationService.Get("HelpTip_Calendar");
                        break;
                }
                HelpInfoWindow.ShowHelp(this, title, desc, tip);
            }
        }

        public void ApplyLocalization()
        {
            try
            {
                Title = LocalizationService.Get("TitleDeviceSyncConfig");
                if (txtHeaderTitle != null) txtHeaderTitle.Text = LocalizationService.Get("DeviceSyncHeaderTitle");
                if (txtHeaderSubtitle != null) txtHeaderSubtitle.Text = LocalizationService.Get("DeviceSyncHeaderDesc");
                if (txtSec1Title != null) txtSec1Title.Text = LocalizationService.Get("DeviceSyncSec1Title");
                if (txtSec1Desc != null) txtSec1Desc.Text = LocalizationService.Get("DeviceSyncSec1Desc");
                if (lblTargetClockCountry != null) lblTargetClockCountry.Text = LocalizationService.Get("DeviceSyncTargetClock");
                if (btnExecuteDeviceClockSync != null) btnExecuteDeviceClockSync.Content = LocalizationService.Get("DeviceSyncBtnClock");
                if (txtSec2Title != null) txtSec2Title.Text = LocalizationService.Get("DeviceSyncSec2Title");
                if (txtSec2Desc != null) txtSec2Desc.Text = LocalizationService.Get("DeviceSyncSec2Desc");
                if (lblTargetHolidayCountry != null) lblTargetHolidayCountry.Text = LocalizationService.Get("DeviceSyncTargetHoliday");
                if (btnExecuteHolidaySync != null) btnExecuteHolidaySync.Content = LocalizationService.Get("DeviceSyncBtnHoliday");
                if (txtSec3Title != null) txtSec3Title.Text = LocalizationService.Get("DeviceSyncSec3Title");
                if (txtSec3Desc != null) txtSec3Desc.Text = LocalizationService.Get("DeviceSyncSec3Desc");
                if (lblTargetCalendarCountry != null) lblTargetCalendarCountry.Text = LocalizationService.Get("DeviceSyncTargetCalendar");
                if (btnExecuteCalendarSync != null) btnExecuteCalendarSync.Content = LocalizationService.Get("DeviceSyncBtnCalendar");
                if (txtFooterTip != null) txtFooterTip.Text = LocalizationService.Get("DeviceSyncFooterTip");
                if (btnClose != null) btnClose.Content = LocalizationService.Get("Close");

                PopulateComboboxes();
            }
            catch (Exception ex)
            {
                Logger.LogException("DeviceSyncConfigWindow.ApplyLocalization", ex);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
