const fs = require('fs');

const servicePath = 'C:/Users/SaidNurs/Desktop/v5/zkteco/LocalizationService.cs';
let content = fs.readFileSync(servicePath, 'utf8');
const extraTrans = JSON.parse(fs.readFileSync('C:/Users/SaidNurs/Desktop/v5/zkteco/scratch/extra_dict.json', 'utf8'));

const langs = ['tr', 'en', 'es', 'zh', 'fr', 'de', 'ru', 'ar', 'ku'];

langs.forEach(lang => {
    const marker = `["${lang}"] = new Dictionary<string, string>`;
    const idx = content.indexOf(marker);
    if (idx !== -1) {
        const braceIdx = content.indexOf('{', idx);
        if (braceIdx !== -1) {
            let insertStr = '\n';
            for (const key of Object.keys(extraTrans)) {
                const val = extraTrans[key][lang] || extraTrans[key]['tr'];
                const escaped = val.replace(/\\/g, '\\\\').replace(/"/g, '\\"').replace(/\n/g, '\\n');
                insertStr += `                ["${key}"] = "${escaped}",\n`;
            }
            content = content.slice(0, braceIdx + 1) + insertStr + content.slice(braceIdx + 1);
            console.log('Inserted extra translations for lang:', lang);
        }
    }
});

fs.writeFileSync(servicePath, content, 'utf8');
console.log('LocalizationService updated with extra translations!');
