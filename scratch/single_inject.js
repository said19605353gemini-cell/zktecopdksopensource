const fs = require('fs');

const servicePath = 'C:/Users/SaidNurs/Desktop/v5/zkteco/LocalizationService.cs';
fs.copyFileSync('C:/Users/SaidNurs/Desktop/v5/zkteco/scratch/recovered_loc.cs', servicePath);

const { trEntries, enEntries, deEntries, ruEntries, arEntries, kuEntries } = require('./entries_module.js');

let content = fs.readFileSync(servicePath, 'utf8');

const dicts = [
    { lang: 'tr', entries: trEntries },
    { lang: 'en', entries: enEntries },
    { lang: 'de', entries: deEntries },
    { lang: 'ru', entries: ruEntries },
    { lang: 'ar', entries: arEntries },
    { lang: 'ku', entries: kuEntries, isLast: true }
];

for (const d of dicts) {
    const marker = '["' + d.lang + '"] = new Dictionary<string, string>';
    const startIdx = content.indexOf(marker);
    if (startIdx === -1) {
        console.error('Marker not found for', d.lang);
        continue;
    }
    
    let endIdx = -1;
    if (d.isLast) {
        endIdx = content.indexOf('\n        };', startIdx);
        if (endIdx === -1) endIdx = content.indexOf('};', startIdx);
    } else {
        endIdx = content.indexOf('\n            },', startIdx);
        if (endIdx === -1) endIdx = content.indexOf('},', startIdx);
    }

    if (endIdx === -1) {
        console.error('End not found for', d.lang);
        continue;
    }

    const before = content.substring(0, endIdx).trimEnd();
    const after = content.substring(endIdx);
    content = before + ',\n' + d.entries.trim() + after;
    console.log('Inserted cleanly for', d.lang);
}

fs.writeFileSync(servicePath, content, 'utf8');
console.log('Done single pass injection!');
