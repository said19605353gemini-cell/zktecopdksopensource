const fs = require('fs');

const servicePath = 'C:/Users/SaidNurs/Desktop/v5/zkteco/LocalizationService.cs';
const scriptPath = 'C:/Users/SaidNurs/Desktop/v5/zkteco/scratch/scratch_localize.js';

// Restore clean base first
fs.copyFileSync('C:/Users/SaidNurs/Desktop/v5/zkteco/scratch/recovered_loc.cs', servicePath);

// Prepare the script module
let scriptContent = fs.readFileSync(scriptPath, 'utf8');
scriptContent = scriptContent.replace('let content = fs.readFileSync(servicePath', 'module.exports = { trEntries, enEntries, deEntries, ruEntries, arEntries, kuEntries };\nlet content = fs.readFileSync(servicePath');

fs.writeFileSync('C:/Users/SaidNurs/Desktop/v5/zkteco/scratch/entries_module.js', scriptContent, 'utf8');

const { trEntries, enEntries, deEntries, ruEntries, arEntries, kuEntries } = require('./entries_module.js');

console.log('trEntries len:', trEntries.length);
console.log('enEntries len:', enEntries.length);
console.log('deEntries len:', deEntries.length);
console.log('ruEntries len:', ruEntries.length);
console.log('arEntries len:', arEntries.length);
console.log('kuEntries len:', kuEntries.length);

let content = fs.readFileSync(servicePath, 'utf8');

function inject(lang, entries, isLast) {
    const marker = '["' + lang + '"] = new Dictionary<string, string>';
    const startIdx = content.indexOf(marker);
    if (startIdx === -1) {
        console.error('Marker not found for', lang);
        return;
    }
    
    let endIdx = -1;
    if (isLast) {
        endIdx = content.indexOf('\n        };', startIdx);
        if (endIdx === -1) endIdx = content.indexOf('};', startIdx);
    } else {
        endIdx = content.indexOf('            },', startIdx);
        if (endIdx === -1) endIdx = content.indexOf('},', startIdx);
    }

    if (endIdx === -1) {
        console.error('End not found for', lang);
        return;
    }

    const before = content.substring(0, endIdx).trimEnd();
    const after = content.substring(endIdx);
    content = before + ',\n' + entries + '\n' + after;
    console.log('Injected successfully for', lang);
}

inject('ku', kuEntries, true);
inject('ar', arEntries, false);
inject('ru', ruEntries, false);
inject('de', deEntries, false);
inject('en', enEntries, false);
inject('tr', trEntries, false);

fs.writeFileSync(servicePath, content, 'utf8');
console.log('LocalizationService.cs successfully updated!');
