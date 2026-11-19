const fs = require('fs');

const locContent = fs.readFileSync('C:/Users/SaidNurs/Desktop/v5/zkteco/LocalizationService.cs', 'utf8');
const tags = [
  'IpPort',              'Connect',
  'GetLogs',             'SyncDeviceTime',
  'Employees',           'Groups',
  'Shifts',              'ShiftSchedule',
  'IrregularPunches',    'DeviceSyncConfig',
  'PeriodYear',          'PeriodMonth',
  'DateRange',           'TodayFilter',
  'DayFilter',           'SyncLatestPeriod',
  'SyncInternetTimeBtn', 'RefreshHolidays',
  'GroupFilter',         'TypeFilter',
  'StatusFilter',        'SearchNameTc',
  'Calculate',           'ClearFilters',
  'ExportMonthly',       'ExportDaily',
  'RefreshDb',           'ExportAllLogs',
  'EmpCardSelect',       'ExportEmpDetail'
];

const langs = ['tr', 'en', 'de', 'ru', 'ar', 'ku'];
langs.forEach(lang => {
    const langStart = locContent.indexOf('["' + lang + '"] = new Dictionary<string, string>');
    const nextLangStart = locContent.indexOf('new Dictionary<string, string>', langStart + 40);
    const langBlock = nextLangStart !== -1 ? locContent.slice(langStart, nextLangStart) : locContent.slice(langStart);

    const missingTags = [];
    tags.forEach(t => {
        const titleKey = '["HelpTitle_' + t + '"]';
        const descKey = '["HelpDesc_' + t + '"]';
        if (!langBlock.includes(titleKey) || !langBlock.includes(descKey)) {
            missingTags.push(t);
        }
    });
    console.log(`Lang ${lang} missing help for tags:`, missingTags);
});
