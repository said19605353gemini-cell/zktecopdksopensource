const fs = require('fs');

const s = fs.readFileSync('C:/Users/SaidNurs/Desktop/v5/zkteco/LocalizationService.cs', 'utf8');
const trStart = s.indexOf('["tr"] = new Dictionary<string, string>');
const nextLang = s.indexOf('["en"] = new Dictionary<string, string>');
const trBlock = s.slice(trStart, nextLang);

const regex = /\["([^"]+)"\]\s*=\s*"((?:\\.|[^"\\])*)"/g;
let m;
const trDict = {};
while ((m = regex.exec(trBlock)) !== null) {
    trDict[m[1]] = m[2];
}

console.log('Total keys in tr:', Object.keys(trDict).length);
fs.writeFileSync('C:/Users/SaidNurs/Desktop/v5/zkteco/scratch/tr_keys.json', JSON.stringify(trDict, null, 2), 'utf8');
