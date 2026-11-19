const fs = require('fs');

const csFile = 'C:/Users/SaidNurs/Desktop/v5/zkteco/MainWindow.xaml.cs';
const xamlFile = 'C:/Users/SaidNurs/Desktop/v5/zkteco/MainWindow.xaml';
const locFile = 'C:/Users/SaidNurs/Desktop/v5/zkteco/LocalizationService.cs';

const csContent = fs.readFileSync(csFile, 'utf8');
const locContent = fs.readFileSync(locFile, 'utf8');

const regex = /LocalizationService\.Get\("([^"]+)"\)/g;
let match;
const keysInCs = new Set();
while ((match = regex.exec(csContent)) !== null) {
    keysInCs.add(match[1]);
}

console.log('Unique keys called via LocalizationService.Get in MainWindow.xaml.cs:', keysInCs.size);

const langs = ['tr', 'en', 'de', 'ru', 'ar', 'ku'];
const langMissing = {};
langs.forEach(l => langMissing[l] = []);

// Check each key in each language dictionary in LocalizationService.cs
for (const lang of langs) {
    const langStart = locContent.indexOf('["' + lang + '"] = new Dictionary<string, string>');
    if (langStart === -1) {
        console.error('Lang not found:', lang);
        continue;
    }
    const nextLangStart = locContent.indexOf('new Dictionary<string, string>', langStart + 40);
    const langBlock = nextLangStart !== -1 ? locContent.slice(langStart, nextLangStart) : locContent.slice(langStart);

    for (const k of keysInCs) {
        if (!langBlock.includes('["' + k + '"]')) {
            langMissing[lang].push(k);
        }
    }
}

console.log('Missing keys per language:');
console.log(JSON.stringify(langMissing, null, 2));
