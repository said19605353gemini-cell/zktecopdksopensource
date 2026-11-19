const fs = require('fs');
let s = fs.readFileSync('C:/Users/SaidNurs/Desktop/v5/zkteco/MainWindow.xaml.cs', 'utf8');

const target = 'dbService = new DatabaseService();                RestoreSavedLanguageAndCountry();\r\n';
const targetNormal = 'dbService = new DatabaseService();\n                RestoreSavedLanguageAndCountry();\r\n';
const targetNoNL = 'dbService = new DatabaseService();RestoreSavedLanguageAndCountry();';

// Let's do line based replacement:
const lines = s.split('\n');
let foundIdx = -1;
for (let i = 0; i < lines.length; i++) {
    if (lines[i].includes('RestoreSavedLanguageAndCountry();') && i < 100) {
        foundIdx = i;
        break;
    }
}
console.log('foundIdx in lines:', foundIdx);
if (foundIdx !== -1) {
    // remove this line
    lines.splice(foundIdx, 1);
    // find RefreshAllData(); line
    for (let j = 0; j < lines.length; j++) {
        if (lines[j].includes('RefreshAllData();') && j < 100) {
            lines.splice(j + 1, 0, '                RestoreSavedLanguageAndCountry();\r');
            break;
        }
    }
    fs.writeFileSync('C:/Users/SaidNurs/Desktop/v5/zkteco/MainWindow.xaml.cs', lines.join('\n'), 'utf8');
    console.log('Reordered successfully via line split!');
}
