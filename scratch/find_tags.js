const fs = require('fs');
const xaml = fs.readFileSync('C:/Users/SaidNurs/Desktop/v5/zkteco/MainWindow.xaml', 'utf8');

const regex = /Tag="([^"]+)"/g;
let match;
const tags = new Set();
while ((match = regex.exec(xaml)) !== null) {
    tags.add(match[1]);
}
console.log('Tags found in MainWindow.xaml:', Array.from(tags));
