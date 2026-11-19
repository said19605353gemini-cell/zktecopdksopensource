const fs = require('fs');

const servicePath = 'C:/Users/SaidNurs/Desktop/v5/zkteco/LocalizationService.cs';
const scriptPath = 'C:/Users/SaidNurs/Desktop/v5/zkteco/scratch/scratch_localize.js';

// Read script to get the entry definitions
const scriptContent = fs.readFileSync(scriptPath, 'utf8');

// Use node to evaluate the entries variables safely in this process
// We can extract trEntries, enEntries, deEntries, ruEntries, arEntries, kuEntries from scriptContent
const vm = require('vm');
const sandbox = { console, require };
vm.createContext(sandbox);
vm.runInContext(scriptContent.substring(0, scriptContent.indexOf('let content =')), sandbox);

let content = fs.readFileSync(servicePath, 'utf8');

function inject(lang, entries, isLast = false) {
    const marker = '["' + lang + '"] = new Dictionary<string, string>';
    const startIdx = content.indexOf(marker);
    if (startIdx === -1) {
        console.error('Marker not found for', lang);
        return;
    }
    
    // Find next dictionary or closing of Translations
    let endIdx = -1;
    if (isLast) {
        // Last dictionary ends before Translations closes: "\n        };"
        endIdx = content.indexOf('\n        };', startIdx);
        if (endIdx === -1) {
            endIdx = content.indexOf('};', startIdx);
        }
    } else {
        // Find next closing brace of this dictionary: "            },"
        endIdx = content.indexOf('            },', startIdx);
        if (endIdx === -1) {
            endIdx = content.indexOf('},', startIdx);
        }
    }

    if (endIdx === -1) {
        console.error('End not found for', lang);
        return;
    }

    // Now insert entries before endIdx
    const before = content.substring(0, endIdx).trimEnd();
    const after = content.substring(endIdx);
    content = before + ',\n' + entries + '\n' + after;
    console.log('Injected successfully for', lang);
}

// In reverse order: ku (isLast), ar, ru, de, en, tr
inject('ku', sandbox.kuEntries, true);
inject('ar', sandbox.arEntries, false);
inject('ru', sandbox.ruEntries, false);
inject('de', sandbox.deEntries, false);
inject('en', sandbox.enEntries, false);
inject('tr', sandbox.trEntries, false);

fs.writeFileSync(servicePath, content, 'utf8');
console.log('LocalizationService.cs successfully updated!');
