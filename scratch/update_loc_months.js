const fs = require('fs');
const file = 'C:/Users/SaidNurs/Desktop/v5/zkteco/LocalizationService.cs';
let content = fs.readFileSync(file, 'utf8');

const additions = {
    'tr': '                ["Month_All"] = "0 - Tüm Aylar",',
    'en': '                ["Month_All"] = "0 - All Months",',
    'de': '                ["Month_All"] = "0 - Alle Monate",',
    'ru': '                ["Month_All"] = "0 - Все месяцы",',
    'ar': '                ["Month_All"] = "0 - جميع الأشهر",',
    'ku': '                ["Month_All"] = "0 - Hemû Meh",'
};

for (const [lang, line] of Object.entries(additions)) {
    const targetCRLF = '["' + lang + '"] = new Dictionary<string, string>\r\n            {';
    const targetLF = '["' + lang + '"] = new Dictionary<string, string>\n            {';
    if (content.includes(targetCRLF)) {
        content = content.replace(targetCRLF, targetCRLF + '\r\n' + line);
    } else if (content.includes(targetLF)) {
        content = content.replace(targetLF, targetLF + '\n' + line);
    } else {
        console.error('Target not found for lang: ' + lang);
    }
}

fs.writeFileSync(file, content, 'utf8');
console.log('LocalizationService updated successfully');
