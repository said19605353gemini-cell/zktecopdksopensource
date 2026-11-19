const fs = require('fs');

const esDict = JSON.parse(fs.readFileSync('C:/Users/SaidNurs/Desktop/v5/zkteco/scratch/es_dict.json', 'utf8'));
const frDict = JSON.parse(fs.readFileSync('C:/Users/SaidNurs/Desktop/v5/zkteco/scratch/fr_dict.json', 'utf8'));

// Build dictionary string for C#
function makeCSharpDict(lang, dict) {
    let block = `            ["${lang}"] = new Dictionary<string, string>\r\n            {\r\n`;
    for (const [k, v] of Object.entries(dict)) {
        const escaped = v.replace(/"/g, '\\"');
        block += `                ["${k}"] = "${escaped}",\r\n`;
    }
    block += `            },\r\n`;
    return block;
}

const locFile = 'C:/Users/SaidNurs/Desktop/v5/zkteco/LocalizationService.cs';
let s = fs.readFileSync(locFile, 'utf8');

// Also add Country_ES and Country_FR keys to other languages if missing
const extraCountries = {
    "tr": { "Country_ES": "İspanya (Madrid)", "Country_FR": "Fransa (Paris)" },
    "en": { "Country_ES": "Spain (Madrid)", "Country_FR": "France (Paris)" },
    "de": { "Country_ES": "Spanien (Madrid)", "Country_FR": "Frankreich (Paris)" },
    "ru": { "Country_ES": "Испания (Мадрид)", "Country_FR": "Франция (Париж)" },
    "ar": { "Country_ES": "إسبانيا (مدريد)", "Country_FR": "فرنسا (باريس)" },
    "ku": { "Country_ES": "Spanya (Madrîd)", "Country_FR": "Fransa (Parîs)" },
    "zh": { "Country_ES": "西班牙 (马德里)", "Country_FR": "法国 (巴黎)" }
};

for (const [l, pairs] of Object.entries(extraCountries)) {
    const targetCRLF = '["' + l + '"] = new Dictionary<string, string>\r\n            {';
    const targetLF = '["' + l + '"] = new Dictionary<string, string>\n            {';
    let isCRLF = s.includes(targetCRLF);
    let target = isCRLF ? targetCRLF : targetLF;
    let nl = isCRLF ? '\r\n' : '\n';
    if (s.includes(target)) {
        let ins = '';
        for (const [k, v] of Object.entries(pairs)) {
            if (!s.includes(`["${k}"] =`)) {
                ins += `                ["${k}"] = "${v}",${nl}`;
            }
        }
        if (ins) s = s.replace(target, target + nl + ins);
    }
}

// Now insert es and fr before closing translations
const lastDictEnd = s.lastIndexOf('        };');
if (lastDictEnd !== -1) {
    const newBlocks = makeCSharpDict('es', esDict) + makeCSharpDict('fr', frDict);
    s = s.slice(0, lastDictEnd) + newBlocks + s.slice(lastDictEnd);
    fs.writeFileSync(locFile, s, 'utf8');
    console.log('Successfully injected Spanish and French dictionaries into LocalizationService.cs!');
} else {
    console.error('Could not find closing };');
}
