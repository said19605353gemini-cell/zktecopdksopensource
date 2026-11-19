const fs = require('fs');
const p = 'C:/Users/SaidNurs/Desktop/v5/zkteco/LocalizationService.cs';
let s = fs.readFileSync(p, 'utf8');

const additions = {
    tr: {
        BtnLiveTimeFormat: '{0} Canlı Saat',
        BtnUpdateHolidaysFormat: '{0} Tatilleri Güncelle',
        TooltipSyncClock: '{0} {1} resmi saatini ({2}) internetten çeker ve cihaz saatini günceller',
        TooltipRefreshHolidays: '{0} {1} resmi tatillerini internetten çeker ve puantaja uygular'
    },
    en: {
        BtnLiveTimeFormat: '{0} Live Clock',
        BtnUpdateHolidaysFormat: '{0} Update Holidays',
        TooltipSyncClock: 'Syncs device clock with {0} {1} official internet time ({2})',
        TooltipRefreshHolidays: 'Downloads {0} {1} public holidays from internet and applies to timesheet'
    },
    de: {
        BtnLiveTimeFormat: '{0} Live-Uhr',
        BtnUpdateHolidaysFormat: '{0} Feiertage aktualisieren',
        TooltipSyncClock: 'Synchronisiert die Gerätezeit mit der offiziellen Internetzeit von {0} {1} ({2})',
        TooltipRefreshHolidays: 'Lädt die gesetzlichen Feiertage von {0} {1} herunter und wendet sie an'
    },
    ru: {
        BtnLiveTimeFormat: '{0} Онлайн-время',
        BtnUpdateHolidaysFormat: '{0} Обновить праздники',
        TooltipSyncClock: 'Синхронизирует время устройства с официальным временем {0} {1} ({2})',
        TooltipRefreshHolidays: 'Загружает праздники {0} {1} из интернета и применяет к табелю'
    },
    ar: {
        BtnLiveTimeFormat: '{0} الوقت المباشر',
        BtnUpdateHolidaysFormat: '{0} تحديث العطلات',
        TooltipSyncClock: 'مزامنة وقت الجهاز مع التوقيت الرسمي لدولة {0} {1} ({2})',
        TooltipRefreshHolidays: 'تحميل العطلات الرسمية لدولة {0} {1} وتطبيقها على تقرير الدوام'
    },
    ku: {
        BtnLiveTimeFormat: '{0} Saeta Zindî',
        BtnUpdateHolidaysFormat: '{0} Betlaneyan Nû Bike',
        TooltipSyncClock: 'Saeta amûrê bi saeta fermî ya înternetê ya {0} {1} ({2}) re wekhev dike',
        TooltipRefreshHolidays: 'Betlaneyên fermî yên {0} {1} ji înternetê dikişîne û li pûantajê zêde dike'
    }
};

for (const lang of Object.keys(additions)) {
    const marker = '["' + lang + '"] = new Dictionary<string, string>';
    const idx = s.indexOf(marker);
    if (idx === -1) {
        console.error('Marker not found for', lang);
        continue;
    }
    const nextBrace = s.indexOf('{', idx);
    let lines = [];
    for (const [k, v] of Object.entries(additions[lang])) {
        lines.push('                ["' + k + '"] = "' + v + '",');
    }
    const insertText = '\n' + lines.join('\n');
    s = s.substring(0, nextBrace + 1) + insertText + s.substring(nextBrace + 1);
}

fs.writeFileSync(p, s, 'utf8');
console.log('Added button format strings in 6 languages successfully!');
