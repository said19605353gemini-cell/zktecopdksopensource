const fs = require('fs');
let s = fs.readFileSync('C:/Users/SaidNurs/Desktop/v5/zkteco/LocalizationService.cs', 'utf8');

const targetStr = '            }\r\n            ["zh"] = new Dictionary<string, string>';
const targetStrLF = '            }\n            ["zh"] = new Dictionary<string, string>';

if (s.includes(targetStr)) {
    s = s.replace(targetStr, '            },\r\n            ["zh"] = new Dictionary<string, string>');
    fs.writeFileSync('C:/Users/SaidNurs/Desktop/v5/zkteco/LocalizationService.cs', s, 'utf8');
    console.log('Fixed missing comma before [\"zh\"] (CRLF)!');
} else if (s.includes(targetStrLF)) {
    s = s.replace(targetStrLF, '            },\n            ["zh"] = new Dictionary<string, string>');
    fs.writeFileSync('C:/Users/SaidNurs/Desktop/v5/zkteco/LocalizationService.cs', s, 'utf8');
    console.log('Fixed missing comma before [\"zh\"] (LF)!');
} else {
    console.error('Target string not found');
}
