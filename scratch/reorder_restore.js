const fs = require('fs');
let s = fs.readFileSync('C:/Users/SaidNurs/Desktop/v5/zkteco/MainWindow.xaml.cs', 'utf8');

const oldBlockCRLF = 'dbService = new DatabaseService();\r\n                RestoreSavedLanguageAndCountry();\r\n                zkService = new ZkService();\r\n\r\n                InitDateComboboxes();\r\n                SyncCalendarToLatestLogDate(false);\r\n                RefreshAllData();';
const newBlockCRLF = 'dbService = new DatabaseService();\r\n                zkService = new ZkService();\r\n\r\n                InitDateComboboxes();\r\n                SyncCalendarToLatestLogDate(false);\r\n                RefreshAllData();\r\n\r\n                RestoreSavedLanguageAndCountry();';

if (s.includes(oldBlockCRLF)) {
    s = s.replace(oldBlockCRLF, newBlockCRLF);
    fs.writeFileSync('C:/Users/SaidNurs/Desktop/v5/zkteco/MainWindow.xaml.cs', s, 'utf8');
    console.log('Successfully reordered RestoreSavedLanguageAndCountry after UI init (CRLF)!');
} else {
    console.error('CRLF block not found!');
}
