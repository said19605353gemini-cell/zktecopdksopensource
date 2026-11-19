const fs = require('fs');

const servicePath = 'C:/Users/SaidNurs/Desktop/v5/zkteco/LocalizationService.cs';
fs.copyFileSync('C:/Users/SaidNurs/Desktop/v5/zkteco/scratch/recovered_loc.cs', servicePath);

let scriptContent = fs.readFileSync('C:/Users/SaidNurs/Desktop/v5/zkteco/scratch/scratch_localize.js', 'utf8');
scriptContent = scriptContent.substring(0, scriptContent.indexOf('let content = fs.readFileSync'));
scriptContent += '\nmodule.exports = { trEntries, enEntries, deEntries, ruEntries, arEntries, kuEntries };\n';
fs.writeFileSync('C:/Users/SaidNurs/Desktop/v5/zkteco/scratch/clean_data.js', scriptContent, 'utf8');

const { trEntries, enEntries, deEntries, ruEntries, arEntries, kuEntries } = require('./clean_data.js');

let content = fs.readFileSync(servicePath, 'utf8');

function insertForLang(lang, entries) {
    const marker = '["' + lang + '"] = new Dictionary<string, string>';
    const idx = content.indexOf(marker);
    if (idx === -1) {
        console.error('Marker not found for:', lang);
        return;
    }
    const nextBracket = content.indexOf('{', idx);
    // Find matching closing brace
    let depth = 0;
    let closeIdx = -1;
    for (let i = nextBracket; i < content.length; i++) {
        if (content[i] === '{') depth++;
        else if (content[i] === '}') {
            depth--;
            if (depth === 0) {
                closeIdx = i;
                break;
            }
        }
    }
    
    if (closeIdx === -1) {
        console.error('Matching closing brace not found for:', lang);
        return;
    }
    
    const before = content.substring(0, closeIdx).trimEnd();
    const after = content.substring(closeIdx);
    content = before + ',\n' + entries.trim() + '\n            ' + after;
    console.log('Successfully injected for:', lang);
}

insertForLang('ku', kuEntries);
insertForLang('ar', arEntries);
insertForLang('ru', ruEntries);
insertForLang('de', deEntries);
insertForLang('en', enEntries);
insertForLang('tr', trEntries);

fs.writeFileSync(servicePath, content, 'utf8');
console.log('Done!');
