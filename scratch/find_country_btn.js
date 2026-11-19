const fs = require('fs');
const s = fs.readFileSync('C:/Users/SaidNurs/Desktop/v5/zkteco/MainWindow.xaml.cs', 'utf8');
const lines = s.split('\n');
lines.forEach((l, i) => {
    if (l.includes('UpdateCountryButtonLabels')) {
        console.log(i);
        console.log(lines.slice(i, i + 35).join('\n'));
    }
});
