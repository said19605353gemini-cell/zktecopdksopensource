const fs = require('fs');
const x = fs.readFileSync('C:/Users/SaidNurs/Desktop/v5/zkteco/MainWindow.xaml', 'utf8');
const regex = /ToolTip="([^"]+)"/g;
let m;
const tips = new Set();
while ((m = regex.exec(x)) !== null) {
    tips.add(m[1]);
}
console.log('MainWindow tooltips count:', tips.size);
for (const t of tips) {
    console.log('  ', t);
}
