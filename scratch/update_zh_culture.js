const fs = require('fs');
let s = fs.readFileSync('C:/Users/SaidNurs/Desktop/v5/zkteco/MainWindow.xaml.cs', 'utf8');

const targetStr = '(lang == "ku" ? "ku-TR" : "tr-TR")';
const newStr = '(lang == "zh" ? "zh-CN" : (lang == "ku" ? "ku-TR" : "tr-TR"))';

if (s.includes(targetStr)) {
    s = s.replace(targetStr, newStr);
    fs.writeFileSync('C:/Users/SaidNurs/Desktop/v5/zkteco/MainWindow.xaml.cs', s, 'utf8');
    console.log('Successfully updated cultureName for zh!');
} else {
    console.error('Target not found for cultureName');
}
