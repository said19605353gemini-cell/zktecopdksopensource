const fs = require('fs');

const mainFile = 'C:/Users/SaidNurs/Desktop/v5/zkteco/MainWindow.xaml.cs';
let s = fs.readFileSync(mainFile, 'utf8');

// 1. In RestoreSavedLanguageAndCountry, restore and save full state
const oldRestore = `        private void RestoreSavedLanguageAndCountry()
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
                    // Dil varsayılanına göre
                    string countryCode = "TR";
                    if (savedLang.Equals("de", StringComparison.OrdinalIgnoreCase)) countryCode = "DE";
                    else if (savedLang.Equals("ru", StringComparison.OrdinalIgnoreCase)) countryCode = "RU";
                    else if (savedLang.Equals("ar", StringComparison.OrdinalIgnoreCase)) countryCode = "SA";
                    else if (savedLang.Equals("en", StringComparison.OrdinalIgnoreCase)) countryCode = "GB";
                    else if (savedLang.Equals("ku", StringComparison.OrdinalIgnoreCase)) countryCode = "TR";
                    HolidayService.CurrentCountryCode = countryCode;
                    DeviceSyncConfigWindow.SelectedDeviceClockCountryCode = countryCode;
                    DeviceSyncConfigWindow.SelectedHolidayCountryCode = countryCode;
                    DeviceSyncConfigWindow.SelectedCalendarCountryCode = countryCode;
                }

                ApplyLocalization();
            }
            catch (Exception ex)
            {
                Logger.LogException("RestoreSavedLanguageAndCountry", ex);
            }
        }`;

const newRestore = `        private void RestoreSavedLanguageAndCountry()
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
        }`;

s = s.replace(oldRestore.replace(/\r\n/g, '\n'), newRestore.replace(/\r\n/g, '\n'));
// In case CRLF:
s = s.replace(oldRestore, newRestore);

// 2. In CbReportDate_SelectionChanged, save year and month index
const oldDateChanged = `        private void CbReportDate_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            CalculateShiftReport();
        }`;

const newDateChanged = `        private void CbReportDate_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (dbService != null)
            {
                if (cbReportYear?.SelectedItem is int y) dbService.SetSetting("SelectedYear", y.ToString());
                if (cbReportMonth != null && cbReportMonth.SelectedIndex >= 0) dbService.SetSetting("SelectedMonthIndex", cbReportMonth.SelectedIndex.ToString());
            }
            CalculateShiftReport();
        }`;

s = s.replace(oldDateChanged.replace(/\r\n/g, '\n'), newDateChanged.replace(/\r\n/g, '\n'));
s = s.replace(oldDateChanged, newDateChanged);

// 3. Save export directory in SaveFileDialog everywhere
// Replace sfd.ShowDialog() save directory
const sfdPattern = /workbook\.SaveAs\(sfd\.FileName\);/g;
s = s.replace(sfdPattern, `workbook.SaveAs(sfd.FileName);
                            try { dbService?.SetSetting("LastExportDirectory", System.IO.Path.GetDirectoryName(sfd.FileName)); } catch {}`);

// Also set InitialDirectory for all sfd in MainWindow
const sfdInitPattern = /var sfd = new SaveFileDialog\r?\n\s*\{/g;
s = s.replace(sfdInitPattern, (match) => {
    return `string lastDir = dbService?.GetSetting("LastExportDirectory", null);
            var sfd = new SaveFileDialog
            {
                InitialDirectory = (!string.IsNullOrEmpty(lastDir) && System.IO.Directory.Exists(lastDir)) ? lastDir : Environment.GetFolderPath(Environment.SpecialFolder.Desktop),`;
});

fs.writeFileSync(mainFile, s, 'utf8');
console.log('MainWindow updated with persistent settings (Year, Month, Excel dir, Holidays, Country)!');
