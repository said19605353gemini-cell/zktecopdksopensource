const fs = require('fs');

const locFile = 'C:/Users/SaidNurs/Desktop/v5/zkteco/LocalizationService.cs';
let content = fs.readFileSync(locFile, 'utf8');

const helpSubs = {
    "tr": {
        "HelpSubtitle": "Bu özellik nasıl çalışır ve ne işe yarar?",
        "HelpBtnClose": "Anladım / Kapat",
        "Country_CN": "Çin (Pekin / Şanghay)"
    },
    "en": {
        "HelpSubtitle": "How does this feature work and what does it do?",
        "HelpBtnClose": "Got it / Close",
        "Country_CN": "China (Beijing / Shanghai)"
    },
    "de": {
        "HelpSubtitle": "Wie funktioniert diese Funktion und wozu dient sie?",
        "HelpBtnClose": "Verstanden / Schließen",
        "Country_CN": "China (Peking / Shanghai)"
    },
    "ru": {
        "HelpSubtitle": "Как работает эта функция и для чего она нужна?",
        "HelpBtnClose": "Понятно / Закрыть",
        "Country_CN": "Китай (Пекин / Шанхай)"
    },
    "ar": {
        "HelpSubtitle": "كيف تعمل هذه الميزة وما هي فائدتها؟",
        "HelpBtnClose": "فهمت / إغلاق",
        "Country_CN": "الصين (بكين / شنغهاي)"
    },
    "ku": {
        "HelpSubtitle": "Ev taybetmendî çawa dixebite û ji bo çi ye?",
        "HelpBtnClose": "Fêm kir / Bigire",
        "Country_CN": "Çîn (Pekîn / Şanghay)"
    }
};

for (const [lang, dict] of Object.entries(helpSubs)) {
    const targetCRLF = '["' + lang + '"] = new Dictionary<string, string>\r\n            {';
    const targetLF = '["' + lang + '"] = new Dictionary<string, string>\n            {';
    let isCRLF = content.includes(targetCRLF);
    let target = isCRLF ? targetCRLF : targetLF;
    let nl = isCRLF ? '\r\n' : '\n';

    let insertion = '';
    for (const [k, v] of Object.entries(dict)) {
        insertion += `                ["${k}"] = "${v}",${nl}`;
    }
    content = content.replace(target, target + nl + insertion);
}

fs.writeFileSync(locFile, content, 'utf8');
console.log('Help subtitles and Chinese country added to existing 6 languages!');
