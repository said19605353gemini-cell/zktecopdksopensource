const fs = require('fs');

const file = 'C:/Users/SaidNurs/Desktop/v5/zkteco/DeviceSyncConfigWindow.xaml.cs';
let s = fs.readFileSync(file, 'utf8');

// In CbHolidayCountry_SelectionChanged, save to DB
const oldHolidayChange = `        private void CbHolidayCountry_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbHolidayCountry?.SelectedValue != null)
            {
                SelectedHolidayCountryCode = cbHolidayCountry.SelectedValue.ToString();
                UpdateLabelsText();
            }
        }`;

const newHolidayChange = `        private void CbHolidayCountry_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbHolidayCountry?.SelectedValue != null)
            {
                SelectedHolidayCountryCode = cbHolidayCountry.SelectedValue.ToString();
                HolidayService.CurrentCountryCode = SelectedHolidayCountryCode;
                mainWindow?.dbService?.SetSetting("SelectedCountry", SelectedHolidayCountryCode);
                UpdateLabelsText();
                mainWindow?.UpdateCountryButtonLabels();
            }
        }`;

s = s.replace(oldHolidayChange.replace(/\r\n/g, '\n'), newHolidayChange.replace(/\r\n/g, '\n'));
s = s.replace(oldHolidayChange, newHolidayChange);

// In CbCalendarCountry_SelectionChanged
const oldCalChange = `        private void CbCalendarCountry_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbCalendarCountry?.SelectedValue != null)
            {
                SelectedCalendarCountryCode = cbCalendarCountry.SelectedValue.ToString();
                UpdateLabelsText();
            }
        }`;

const newCalChange = `        private void CbCalendarCountry_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbCalendarCountry?.SelectedValue != null)
            {
                SelectedCalendarCountryCode = cbCalendarCountry.SelectedValue.ToString();
                mainWindow?.dbService?.SetSetting("SelectedCalendarCountry", SelectedCalendarCountryCode);
                UpdateLabelsText();
            }
        }`;

s = s.replace(oldCalChange.replace(/\r\n/g, '\n'), newCalChange.replace(/\r\n/g, '\n'));
s = s.replace(oldCalChange, newCalChange);

// In CbDeviceClockCountry_SelectionChanged
const oldClockChange = `        private void CbDeviceClockCountry_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbDeviceClockCountry?.SelectedValue != null)
            {
                SelectedDeviceClockCountryCode = cbDeviceClockCountry.SelectedValue.ToString();
                UpdateClockPreview();
            }
        }`;

const newClockChange = `        private void CbDeviceClockCountry_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbDeviceClockCountry?.SelectedValue != null)
            {
                SelectedDeviceClockCountryCode = cbDeviceClockCountry.SelectedValue.ToString();
                mainWindow?.dbService?.SetSetting("SelectedDeviceClockCountry", SelectedDeviceClockCountryCode);
                UpdateClockPreview();
            }
        }`;

s = s.replace(oldClockChange.replace(/\r\n/g, '\n'), newClockChange.replace(/\r\n/g, '\n'));
s = s.replace(oldClockChange, newClockChange);

// In BtnExecuteHolidaySync_Click, make sure setting is saved
const oldSyncSave = `                await Task.Run(() =>
                {
                    HolidayService.CurrentCountryCode = code;
                    holidays = HolidayService.GetHolidaysForYear(year, mainWindow?.dbService, forceRefresh: true, countryCode: code);
                });`;

const newSyncSave = `                await Task.Run(() =>
                {
                    HolidayService.CurrentCountryCode = code;
                    holidays = HolidayService.GetHolidaysForYear(year, mainWindow?.dbService, forceRefresh: true, countryCode: code);
                });
                mainWindow?.dbService?.SetSetting("SelectedCountry", code);`;

s = s.replace(oldSyncSave.replace(/\r\n/g, '\n'), newSyncSave.replace(/\r\n/g, '\n'));
s = s.replace(oldSyncSave, newSyncSave);

fs.writeFileSync(file, s, 'utf8');
console.log('DeviceSyncConfigWindow updated with persistent country/holiday settings!');
