const fs = require('fs');

const locPath = 'C:/Users/SaidNurs/Desktop/v5/zkteco/LocalizationService.cs';
let content = fs.readFileSync(locPath, 'utf8');

const countryTranslations = {
    "tr": {
        "Country_TR": "Türkiye",
        "Country_DE": "Almanya (Deutschland)",
        "Country_GB": "Birleşik Krallık (UK)",
        "Country_US": "ABD (Amerika Birleşik Devletleri)",
        "Country_RU": "Rusya (Moskova)",
        "Country_AZ": "Azerbaycan (Bakü)",
        "Country_SA": "Suudi Arabistan (Riyad)",
        "Country_AE": "Birleşik Arap Emirlikleri (Dubai)",
        "Country_IQ": "Irak (Bağdat / Erbil)",
        "Country_FR": "Fransa (Paris)",
        "Country_NL": "Hollanda (Amsterdam)",
        "Country_IT": "İtalya (Roma)",
        "Country_KZ": "Kazakistan (Astana)",
        "Country_UZ": "Özbekistan (Taşkent)",
        "DeviceSyncLiveClockFormat": "{0} Canlı Saati: {1:yyyy-MM-dd HH:mm:ss}",
        "DeviceSyncTimeZoneFormat": "Saat Dilimi: {0} · Fallback: UTC{1}",
        "DeviceSyncHolidayInfoFormat": "{0} seçildi. Bu ülkeye ait resmi tatiller Nager.Date API üzerinden puantaj tablosuna entegre edilecektir.",
        "DeviceSyncCalendarInfoFormat": "{0} takvim odağı olarak belirlendi."
    },
    "en": {
        "Country_TR": "Turkey (Türkiye)",
        "Country_DE": "Germany (Deutschland)",
        "Country_GB": "United Kingdom (UK)",
        "Country_US": "United States (US)",
        "Country_RU": "Russia (Moscow)",
        "Country_AZ": "Azerbaijan (Baku)",
        "Country_SA": "Saudi Arabia (Riyadh)",
        "Country_AE": "United Arab Emirates (Dubai)",
        "Country_IQ": "Iraq (Baghdad / Erbil)",
        "Country_FR": "France (Paris)",
        "Country_NL": "Netherlands (Amsterdam)",
        "Country_IT": "Italy (Rome)",
        "Country_KZ": "Kazakhstan (Astana)",
        "Country_UZ": "Uzbekistan (Tashkent)",
        "DeviceSyncLiveClockFormat": "{0} Live Time: {1:yyyy-MM-dd HH:mm:ss}",
        "DeviceSyncTimeZoneFormat": "Time Zone: {0} · Fallback: UTC{1}",
        "DeviceSyncHolidayInfoFormat": "{0} selected. Official public holidays will be fetched via Nager.Date API and integrated into timesheets.",
        "DeviceSyncCalendarInfoFormat": "{0} set as calendar focus."
    },
    "de": {
        "Country_TR": "Türkei (Türkiye)",
        "Country_DE": "Deutschland",
        "Country_GB": "Vereinigtes Königreich (UK)",
        "Country_US": "Vereinigte Staaten (USA)",
        "Country_RU": "Russland (Moskau)",
        "Country_AZ": "Aserbaidschan (Baku)",
        "Country_SA": "Saudi-Arabien (Riad)",
        "Country_AE": "Vereinigte Arabische Emirate (Dubai)",
        "Country_IQ": "Irak (Bagdad / Erbil)",
        "Country_FR": "Frankreich (Paris)",
        "Country_NL": "Niederlande (Amsterdam)",
        "Country_IT": "Italien (Rom)",
        "Country_KZ": "Kasachstan (Astana)",
        "Country_UZ": "Usbekistan (Taschkent)",
        "DeviceSyncLiveClockFormat": "{0} Live-Uhrzeit: {1:yyyy-MM-dd HH:mm:ss}",
        "DeviceSyncTimeZoneFormat": "Zeitzone: {0} · Fallback: UTC{1}",
        "DeviceSyncHolidayInfoFormat": "{0} ausgewählt. Gesetzliche Feiertage werden über die Nager.Date API in die Zeiterfassung integriert.",
        "DeviceSyncCalendarInfoFormat": "{0} als Kalenderfokus festgelegt."
    },
    "ru": {
        "Country_TR": "Турция (Türkiye)",
        "Country_DE": "Германия (Deutschland)",
        "Country_GB": "Великобритания (UK)",
        "Country_US": "США (United States)",
        "Country_RU": "Россия (Москва)",
        "Country_AZ": "Азербайджан (Баку)",
        "Country_SA": "Саудовская Аравия (Эр-Рияд)",
        "Country_AE": "ОАЭ (Дубай)",
        "Country_IQ": "Ирак (Багдад / Эрбиль)",
        "Country_FR": "Франция (Париж)",
        "Country_NL": "Нидерланды (Амстердам)",
        "Country_IT": "Италия (Рим)",
        "Country_KZ": "Казахстан (Астана)",
        "Country_UZ": "Узбекистан (Ташкент)",
        "DeviceSyncLiveClockFormat": "{0} Текущее время: {1:yyyy-MM-dd HH:mm:ss}",
        "DeviceSyncTimeZoneFormat": "Часовой пояс: {0} · Резерв: UTC{1}",
        "DeviceSyncHolidayInfoFormat": "{0} выбрано. Официальные праздники будут загружены через Nager.Date API и применены к табелю.",
        "DeviceSyncCalendarInfoFormat": "{0} выбрано как фокус календаря."
    },
    "ar": {
        "Country_TR": "تركيا (Türkiye)",
        "Country_DE": "ألمانيا (Deutschland)",
        "Country_GB": "المملكة المتحدة (UK)",
        "Country_US": "الولايات المتحدة (US)",
        "Country_RU": "روسيا (موسكو)",
        "Country_AZ": "أذربيجان (باكو)",
        "Country_SA": "المملكة العربية السعودية (الرياض)",
        "Country_AE": "الإمارات العربية المتحدة (دبي)",
        "Country_IQ": "العراق (بغداد / أربيل)",
        "Country_FR": "فرنسا (باريس)",
        "Country_NL": "هولندا (أمستردام)",
        "Country_IT": "إيطاليا (روما)",
        "Country_KZ": "كازاخستان (أستانا)",
        "Country_UZ": "أوزبكستان (طشقند)",
        "DeviceSyncLiveClockFormat": "{0} الوقت المباشر: {1:yyyy-MM-dd HH:mm:ss}",
        "DeviceSyncTimeZoneFormat": "المنطقة الزمنية: {0} · البديل: UTC{1}",
        "DeviceSyncHolidayInfoFormat": "تم اختيار {0}. سيتم جلب العطلات الرسمية عبر Nager.Date API ودمجها في جدول الحضور.",
        "DeviceSyncCalendarInfoFormat": "تم تعيين {0} كتركيز للتقويم."
    },
    "ku": {
        "Country_TR": "Tirkiye (Türkiye)",
        "Country_DE": "Almanya (Deutschland)",
        "Country_GB": "Keyaniya Yekbûyî (UK)",
        "Country_US": "DYA (Amerîka)",
        "Country_RU": "Rûsya (Moskova)",
        "Country_AZ": "Azerbaycan (Bakû)",
        "Country_SA": "Erebistana Siûdî (Riyad)",
        "Country_AE": "Emîrtiyên Yekbûyî yên Erebî (Dubay)",
        "Country_IQ": "Iraq (Bexda / Hewlêr)",
        "Country_FR": "Fransa (Parîs)",
        "Country_NL": "Hollanda (Amsterdam)",
        "Country_IT": "Îtalya (Roma)",
        "Country_KZ": "Kazaxistan (Astana)",
        "Country_UZ": "Ozbekistan (Taşkent)",
        "DeviceSyncLiveClockFormat": "{0} Saeta Zindî: {1:yyyy-MM-dd HH:mm:ss}",
        "DeviceSyncTimeZoneFormat": "Qada Saetê: {0} · Cîgir: UTC{1}",
        "DeviceSyncHolidayInfoFormat": "{0} hat hilbijartin. Betlaneyên fermî dê bi rêya Nager.Date API bikeve nav pûantajê.",
        "DeviceSyncCalendarInfoFormat": "{0} wekî bala salnameyê hate diyarkirin."
    }
};

const langs = ['tr', 'en', 'de', 'ru', 'ar', 'ku'];
for (const lang of langs) {
    const headerCRLF = '["' + lang + '"] = new Dictionary<string, string>\r\n            {';
    const headerLF = '["' + lang + '"] = new Dictionary<string, string>\n            {';
    
    let isCRLF = content.includes(headerCRLF);
    let target = isCRLF ? headerCRLF : headerLF;
    let newline = isCRLF ? '\r\n' : '\n';

    if (!content.includes(target)) {
        console.error('Target not found for lang: ' + lang);
        continue;
    }

    const dict = countryTranslations[lang];
    let insertion = '';
    for (const [k, v] of Object.entries(dict)) {
        const escaped = v.replace(/"/g, '\\"');
        insertion += `                ["${k}"] = "${escaped}",${newline}`;
    }

    content = content.replace(target, target + newline + insertion);
}

fs.writeFileSync(locPath, content, 'utf8');
console.log('LocalizationService updated with country names and device sync formats!');
