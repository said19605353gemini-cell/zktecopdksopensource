const fs = require('fs');
let s = fs.readFileSync('C:/Users/SaidNurs/Desktop/v5/zkteco/MainWindow.xaml.cs', 'utf8');

const search = 'ApplyLocalization();\r\n\r\n                // Yeni ülkeye göre tatilleri';
const searchLF = 'ApplyLocalization();\n\n                // Yeni ülkeye göre tatilleri';

const additionCRLF = 'if (dbService != null)\r\n                {\r\n                    dbService.SetSetting("SelectedLanguage", lang);\r\n                    dbService.SetSetting("SelectedCountry", countryCode);\r\n                }\r\n\r\n                ApplyLocalization();\r\n\r\n                // Yeni ülkeye göre tatilleri';
const additionLF = 'if (dbService != null)\n                {\n                    dbService.SetSetting("SelectedLanguage", lang);\n                    dbService.SetSetting("SelectedCountry", countryCode);\n                }\n\n                ApplyLocalization();\n\n                // Yeni ülkeye göre tatilleri';

if (s.includes(search)) {
    s = s.replace(search, additionCRLF);
} else if (s.includes(searchLF)) {
    s = s.replace(searchLF, additionLF);
} else {
    console.error('Search target not found!');
    process.exit(1);
}

fs.writeFileSync('C:/Users/SaidNurs/Desktop/v5/zkteco/MainWindow.xaml.cs', s, 'utf8');
console.log('Successfully inserted SetSetting into CbLanguage_SelectionChanged');
