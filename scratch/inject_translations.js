const fs = require('fs');

const servicePath = 'C:/Users/SaidNurs/Desktop/v5/zkteco/LocalizationService.cs';
let content = fs.readFileSync(servicePath, 'utf8');
const msgTrans = JSON.parse(fs.readFileSync('C:/Users/SaidNurs/Desktop/v5/zkteco/scratch/msg_translations.json', 'utf8'));

const langs = ['tr', 'en', 'es', 'zh', 'fr', 'de', 'ru', 'ar', 'ku'];

langs.forEach(lang => {
    // Find ["lang"] = new Dictionary<string, string> { ... }
    const marker = `["${lang}"] = new Dictionary<string, string>`;
    const idx = content.indexOf(marker);
    if (idx !== -1) {
        // find opening brace {
        const braceIdx = content.indexOf('{', idx);
        if (braceIdx !== -1) {
            let insertStr = '\n';
            for (const key of Object.keys(msgTrans)) {
                const val = msgTrans[key][lang] || msgTrans[key]['tr'];
                // escape quotes and backslashes for c#
                const escaped = val.replace(/\\/g, '\\\\').replace(/"/g, '\\"').replace(/\n/g, '\\n');
                insertStr += `                ["${key}"] = "${escaped}",\n`;
            }
            content = content.slice(0, braceIdx + 1) + insertStr + content.slice(braceIdx + 1);
            console.log('Inserted translations for lang:', lang);
        }
    }
});

fs.writeFileSync(servicePath, content, 'utf8');
console.log('LocalizationService updated successfully!');
