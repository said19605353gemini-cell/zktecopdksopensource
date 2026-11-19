const fs = require('fs');
let s = fs.readFileSync('C:/Users/SaidNurs/Desktop/v5/zkteco/MainWindow.xaml.cs', 'utf8');

const targetStr = 'else if (lang.Equals("ku", StringComparison.OrdinalIgnoreCase)) countryCode = "TR";';
const newStr = 'else if (lang.Equals("zh", StringComparison.OrdinalIgnoreCase)) countryCode = "CN";\r\n                else if (lang.Equals("ku", StringComparison.OrdinalIgnoreCase)) countryCode = "TR";';

if (s.includes(targetStr)) {
    s = s.replace(targetStr, newStr);
    fs.writeFileSync('C:/Users/SaidNurs/Desktop/v5/zkteco/MainWindow.xaml.cs', s, 'utf8');
    console.log('Successfully added zh country sync in CbLanguage_SelectionChanged!');
} else {
    console.error('Target not found');
}
