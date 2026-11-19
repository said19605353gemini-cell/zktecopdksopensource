const fs = require('fs');

const mainFile = 'C:/Users/SaidNurs/Desktop/v5/zkteco/MainWindow.xaml.cs';
let content = fs.readFileSync(mainFile, 'utf8');

// 1. In MainWindow ctor, restore language & country settings right after dbService = new DatabaseService();
const dbInit = 'dbService = new DatabaseService();';
const restoreSettings = `dbService = new DatabaseService();
                RestoreSavedLanguageAndCountry();`;

if (content.includes(dbInit)) {
    content = content.replace(dbInit, restoreSettings);
}

// 2. Add RestoreSavedLanguageAndCountry method
const methodToAdd = `
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
        }
`;

// Insert the method above CbLanguage_SelectionChanged
const targetFunc = 'private void CbLanguage_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)';
content = content.replace(targetFunc, methodToAdd + '\r\n        ' + targetFunc);

// 3. In CbLanguage_SelectionChanged, save to DB:
const oldCbHandler = `                HolidayService.CurrentCountryCode = countryCode;
                DeviceSyncConfigWindow.SelectedDeviceClockCountryCode = countryCode;
                DeviceSyncConfigWindow.SelectedHolidayCountryCode = countryCode;
                DeviceSyncConfigWindow.SelectedCalendarCountryCode = countryCode;

                ApplyLocalization();`;

const newCbHandler = `                HolidayService.CurrentCountryCode = countryCode;
                DeviceSyncConfigWindow.SelectedDeviceClockCountryCode = countryCode;
                DeviceSyncConfigWindow.SelectedHolidayCountryCode = countryCode;
                DeviceSyncConfigWindow.SelectedCalendarCountryCode = countryCode;

                // Son seçilen dil ve ülkeyi veritabanına kaydet
                if (dbService != null)
                {
                    dbService.SetSetting("SelectedLanguage", lang);
                    dbService.SetSetting("SelectedCountry", countryCode);
                }

                ApplyLocalization();`;

content = content.replace(oldCbHandler, newCbHandler);

fs.writeFileSync(mainFile, content, 'utf8');
console.log('MainWindow.xaml.cs updated with persistent language/country settings!');
