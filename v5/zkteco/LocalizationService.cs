using System.Collections.Generic;

namespace ZkPdksApp
{
    public static class LocalizationService
    {
        public static string CurrentLanguage { get; set; } = "tr";

        private static readonly Dictionary<string, Dictionary<string, string>> Translations = new Dictionary<string, Dictionary<string, string>>
        {
            ["tr"] = new Dictionary<string, string>
            {
                ["AppTitle"] = "ZKTeco PDKS ENTERPRISE",
                ["AppSubtitle"] = "Taşeron & Personel Yönetimi • Gece/Gündüz Ayrımı • Çoklu Vardiya & Hakediş Puantajı",
                ["Connect"] = "Cihaza Bağlan",
                ["GetLogs"] = "Logları Çek",
                ["SyncDeviceTime"] = "Cihaz Saatini İnternetle Eşitle",
                ["Employees"] = "Personel Özlük",
                ["Groups"] = "Grup & Statü Yönetimi",
                ["Shifts"] = "Mesai & Taşeron Kuralları",
                
                ["KpiConnection"] = "CİHAZ VE HAT",
                ["KpiDeviceTime"] = "CİHAZ SAATİ (RTC)",
                ["KpiEmpCount"] = "KADRO & TAŞERON",
                ["KpiTotalOt"] = "TOPLAM GENEL MESAİ",
                ["KpiSatHours"] = "CUMARTESİ ÇALIŞMA",
                ["KpiSunHours"] = "PAZAR & TATİL MESAİ",
                
                ["TabReport"] = "  Puantaj & Mesai Raporu  ",
                ["TabLogs"] = "  Ham Geçiş Hareketleri  ",
                ["TabEmpPanel"] = "  Personel Bilgi & Özet Paneli  ",
                
                ["Year"] = "Yıl:",
                ["Month"] = "Ay:",
                ["Day"] = "Gün:",
                ["Group"] = "Grup:",
                ["Type"] = "Tür:",
                ["Status"] = "Statü:",
                ["SearchName"] = "Ad Soyad Ara...",
                ["SearchTc"] = "T.C. No Ara...",
                
                ["BtnLatestPeriod"] = "Son Veri Dönemine Git",
                ["BtnInternetTime"] = "Türkiye Canlı Tarih/Saat",
                ["BtnUpdateHolidays"] = "Tatilleri Güncelle",
                ["BtnCalculate"] = "Hesapla",
                ["BtnClearFilters"] = "Filtreleri Sıfırla",
                ["BtnExportMonthly"] = "Bordro Excel'e Aktar",
                ["BtnExportDaily"] = "Gün Gün Puantaj Excel'e Aktar",
                ["BtnRefreshDb"] = "Veritabanını Yenile",
                ["BtnExportAllLogs"] = "Tüm Hareketleri Excel'e Aktar",
                ["BtnExportEmpDetail"] = "Bu Personelin Aylık Detayını Excel'e Aktar",
                
                ["InspectEmp"] = "İncelenecek Personel:",
                ["EmpSummaryTitle"] = "SEÇİLİ AY TOPLAMLARI",
                ["WorkedDays"] = "Çalıştığı Gün Sayısı:",
                ["TotalNet"] = "Toplam Net Çalışma:",
                ["WeekdayOt"] = "Hafta İçi Mesai:",
                ["SaturdayOt"] = "Cumartesi Çalışma:",
                ["SundayOt"] = "Pazar & Tatil Çalışma:",
                ["MorningOt"] = "Sabah Mesaisi (Saat):",
                ["EveningOt"] = "Akşam Mesaisi (Saat):",
                ["NightOt"] = "Gece Mesaisi (24:00+):",
                ["ExcessBreak"] = "Toplam Mola Aşımı:",
                ["WeightedOt"] = "Toplam Hakediş Mesai:",
                ["MissingWork"] = "Toplam Eksik Çalışma:",

                // Gün Tipleri
                ["DayFilter_All"] = "Tüm Günler",
                ["DayFilter_Holidays"] = "Sadece Resmi Tatiller",
                ["DayFilter_HolidaysWeekend"] = "Resmi Tatil & Hafta Sonu",
                ["DayFilter_Weekend"] = "Cumartesi & Pazar (Hafta Sonu)",
                ["DayFilter_Sat"] = "Sadece Cumartesi",
                ["DayFilter_Sun"] = "Sadece Pazar",
                ["DayFilter_Weekdays"] = "Sadece Hafta İçi (Normal Günler)",

                // Statüler
                ["Status_All"] = "Tümü (Tüm Personel)",
                ["Status_Active"] = "Sadece Aktif Olanlar",
                ["Status_Passive"] = "Pasif (Ayrılanlar)",
                ["Status_OnLeave"] = "İzinli Olanlar"
            },
            ["en"] = new Dictionary<string, string>
            {
                ["AppTitle"] = "ZKTeco Time Attendance ENTERPRISE",
                ["AppSubtitle"] = "Subcontractor & Staff Management • Day/Night Split • Shift Rules & Payroll",
                ["Connect"] = "Connect Device",
                ["GetLogs"] = "Get Logs",
                ["SyncDeviceTime"] = "Sync Device Time (Internet)",
                ["Employees"] = "Employee Profiles",
                ["Groups"] = "Group & Status",
                ["Shifts"] = "Shift & Overtime Rules",
                
                ["KpiConnection"] = "DEVICE & NETWORK",
                ["KpiDeviceTime"] = "DEVICE TIME (RTC)",
                ["KpiEmpCount"] = "TOTAL STAFF",
                ["KpiTotalOt"] = "TOTAL OVERTIME",
                ["KpiSatHours"] = "SATURDAY WORK",
                ["KpiSunHours"] = "SUNDAY & HOLIDAY",
                
                ["TabReport"] = "  Attendance & Overtime Report  ",
                ["TabLogs"] = "  Raw Punch Logs  ",
                ["TabEmpPanel"] = "  Staff 360° Summary Panel  ",
                
                ["Year"] = "Year:",
                ["Month"] = "Month:",
                ["Day"] = "Day:",
                ["Group"] = "Group:",
                ["Type"] = "Type:",
                ["Status"] = "Status:",
                ["SearchName"] = "Search Name...",
                ["SearchTc"] = "Search ID/TC...",
                
                ["BtnLatestPeriod"] = "Go to Latest Log Period",
                ["BtnInternetTime"] = "Live Turkey Time/Date",
                ["BtnUpdateHolidays"] = "Update Holidays",
                ["BtnCalculate"] = "Calculate",
                ["BtnClearFilters"] = "Reset Filters",
                ["BtnExportMonthly"] = "Export Payroll Excel",
                ["BtnExportDaily"] = "Export Daily Timesheet Excel",
                ["BtnRefreshDb"] = "Refresh Database",
                ["BtnExportAllLogs"] = "Export All Logs Excel",
                ["BtnExportEmpDetail"] = "Export Staff Monthly Excel",
                
                ["InspectEmp"] = "Select Employee:",
                ["EmpSummaryTitle"] = "MONTHLY TOTALS",
                ["WorkedDays"] = "Worked Days Count:",
                ["TotalNet"] = "Total Net Work:",
                ["WeekdayOt"] = "Weekday Overtime:",
                ["SaturdayOt"] = "Saturday Work:",
                ["SundayOt"] = "Sunday & Holiday Work:",
                ["MorningOt"] = "Morning Overtime (Hrs):",
                ["EveningOt"] = "Evening Overtime (Hrs):",
                ["NightOt"] = "Night Overtime (24:00+):",
                ["ExcessBreak"] = "Total Excess Break:",
                ["WeightedOt"] = "Weighted Overtime Total:",
                ["MissingWork"] = "Total Missing Work:",

                ["DayFilter_All"] = "All Days",
                ["DayFilter_Holidays"] = "Only Public Holidays",
                ["DayFilter_HolidaysWeekend"] = "Holidays & Weekends",
                ["DayFilter_Weekend"] = "Saturday & Sunday (Weekend)",
                ["DayFilter_Sat"] = "Only Saturday",
                ["DayFilter_Sun"] = "Only Sunday",
                ["DayFilter_Weekdays"] = "Weekdays Only (Normal)",

                ["Status_All"] = "All (All Staff)",
                ["Status_Active"] = "Only Active Staff",
                ["Status_Passive"] = "Passive (Resigned)",
                ["Status_OnLeave"] = "On Leave"
            },
            ["de"] = new Dictionary<string, string>
            {
                ["AppTitle"] = "ZKTeco Zeiterfassung ENTERPRISE",
                ["AppSubtitle"] = "Mitarbeiter- & Schichtverwaltung • Tag/Nacht-Trennung • Abrechnung",
                ["Connect"] = "Gerät verbinden",
                ["GetLogs"] = "Protokolle abrufen",
                ["SyncDeviceTime"] = "Gerätezeit synchronisieren",
                ["Employees"] = "Mitarbeiterdaten",
                ["Groups"] = "Gruppen & Status",
                ["Shifts"] = "Schicht- & Überstundenregeln",
                
                ["KpiConnection"] = "GERÄT & VERBINDUNG",
                ["KpiDeviceTime"] = "GERÄTEZEIT (RTC)",
                ["KpiEmpCount"] = "MITARBEITER GESAMT",
                ["KpiTotalOt"] = "ÜBERSTUNDEN GESAMT",
                ["KpiSatHours"] = "SAMSTAGSARBEIT",
                ["KpiSunHours"] = "SONNTAG & FEIERTAG",
                
                ["TabReport"] = "  Zeiterfassung & Überstunden  ",
                ["TabLogs"] = "  Rohbuchungen  ",
                ["TabEmpPanel"] = "  Mitarbeiter-Übersicht  ",
                
                ["Year"] = "Jahr:",
                ["Month"] = "Monat:",
                ["Day"] = "Tag:",
                ["Group"] = "Gruppe:",
                ["Type"] = "Art:",
                ["Status"] = "Status:",
                ["SearchName"] = "Name suchen...",
                ["SearchTc"] = "ID suchen...",
                
                ["BtnLatestPeriod"] = "Zum letzten Zeitraum",
                ["BtnInternetTime"] = "Live Internetzeit",
                ["BtnUpdateHolidays"] = "Feiertage aktualisieren",
                ["BtnCalculate"] = "Berechnen",
                ["BtnClearFilters"] = "Filter zurücksetzen",
                ["BtnExportMonthly"] = "Abrechnung Excel",
                ["BtnExportDaily"] = "Tagesbericht Excel",
                ["BtnRefreshDb"] = "Datenbank aktualisieren",
                ["BtnExportAllLogs"] = "Alle Buchungen Excel",
                ["BtnExportEmpDetail"] = "Monatsübersicht Excel",
                
                ["InspectEmp"] = "Mitarbeiter auswählen:",
                ["EmpSummaryTitle"] = "MONATSSUMMEN",
                ["WorkedDays"] = "Arbeitstage:",
                ["TotalNet"] = "Nettoarbeitszeit:",
                ["WeekdayOt"] = "Wochentagsüberstunden:",
                ["SaturdayOt"] = "Samstagsarbeit:",
                ["SundayOt"] = "Sonn- & Feiertagsarbeit:",
                ["MorningOt"] = "Frühüberstunden:",
                ["EveningOt"] = "Abendüberstunden:",
                ["NightOt"] = "Nachtarbeit (24:00+):",
                ["ExcessBreak"] = "Pausenüberschreitung:",
                ["WeightedOt"] = "Gewichtete Überstunden:",
                ["MissingWork"] = "Fehlzeiten gesamt:",

                ["DayFilter_All"] = "Alle Tage",
                ["DayFilter_Holidays"] = "Nur Feiertage",
                ["DayFilter_HolidaysWeekend"] = "Feiertage & Wochenende",
                ["DayFilter_Weekend"] = "Samstag & Sonntag",
                ["DayFilter_Sat"] = "Nur Samstag",
                ["DayFilter_Sun"] = "Nur Sonntag",
                ["DayFilter_Weekdays"] = "Nur Werktage",

                ["Status_All"] = "Alle Mitarbeiter",
                ["Status_Active"] = "Nur Aktive",
                ["Status_Passive"] = "Ausgeschieden",
                ["Status_OnLeave"] = "Beurlaubt"
            },
            ["ru"] = new Dictionary<string, string>
            {
                ["AppTitle"] = "ZKTeco Учет Рабочего Времени",
                ["AppSubtitle"] = "Управление персоналом • Дневные/Ночные смены • Табель и расчет",
                ["Connect"] = "Подключить",
                ["GetLogs"] = "Загрузить логи",
                ["SyncDeviceTime"] = "Синхронизировать время",
                ["Employees"] = "Сотрудники",
                ["Groups"] = "Группы и статусы",
                ["Shifts"] = "Правила смен и переработок",
                
                ["KpiConnection"] = "УСТРОЙСТВО И СЕТЬ",
                ["KpiDeviceTime"] = "ВРЕМЯ УСТРОЙСТВА",
                ["KpiEmpCount"] = "ВСЕГО СОТРУДНИКОВ",
                ["KpiTotalOt"] = "ВСЕГО ПЕРЕРАБОТОК",
                ["KpiSatHours"] = "СУББОТНИЕ ЧАСЫ",
                ["KpiSunHours"] = "ВОСКРЕСЕНЬЕ И ПРАЗДНИКИ",
                
                ["TabReport"] = "  Табель и переработки  ",
                ["TabLogs"] = "  Журнал проходов  ",
                ["TabEmpPanel"] = "  Панель сотрудника  ",
                
                ["Year"] = "Год:",
                ["Month"] = "Месяц:",
                ["Day"] = "День:",
                ["Group"] = "Группа:",
                ["Type"] = "Тип:",
                ["Status"] = "Статус:",
                ["SearchName"] = "Поиск по имени...",
                ["SearchTc"] = "Поиск по ID/ИНН...",
                
                ["BtnLatestPeriod"] = "К последнему периоду",
                ["BtnInternetTime"] = "Время по интернету",
                ["BtnUpdateHolidays"] = "Обновить праздники",
                ["BtnCalculate"] = "Рассчитать",
                ["BtnClearFilters"] = "Сбросить фильтры",
                ["BtnExportMonthly"] = "Экспорт в Excel (Табель)",
                ["BtnExportDaily"] = "Экспорт посуточного табеля",
                ["BtnRefreshDb"] = "Обновить базу",
                ["BtnExportAllLogs"] = "Экспорт всех логов",
                ["BtnExportEmpDetail"] = "Экспорт данных сотрудника",
                
                ["InspectEmp"] = "Сотрудник:",
                ["EmpSummaryTitle"] = "ИТОГИ ЗА МЕСЯЦ",
                ["WorkedDays"] = "Отработано дней:",
                ["TotalNet"] = "Чистое время:",
                ["WeekdayOt"] = "Переработки в будни:",
                ["SaturdayOt"] = "Работа в субботу:",
                ["SundayOt"] = "Воскресенье и праздники:",
                ["MorningOt"] = "Утренняя переработка:",
                ["EveningOt"] = "Вечерняя переработка:",
                ["NightOt"] = "Ночные часы (24:00+):",
                ["ExcessBreak"] = "Превышение перерывов:",
                ["WeightedOt"] = "Итоговая переработка:",
                ["MissingWork"] = "Недоработка:",

                ["DayFilter_All"] = "Все дни",
                ["DayFilter_Holidays"] = "Только праздники",
                ["DayFilter_HolidaysWeekend"] = "Праздники и выходные",
                ["DayFilter_Weekend"] = "Суббота и воскресенье",
                ["DayFilter_Sat"] = "Только суббота",
                ["DayFilter_Sun"] = "Только воскресенье",
                ["DayFilter_Weekdays"] = "Только будние дни",

                ["Status_All"] = "Все сотрудники",
                ["Status_Active"] = "Только активные",
                ["Status_Passive"] = "Уволенные",
                ["Status_OnLeave"] = "В отпуске"
            },
            ["ar"] = new Dictionary<string, string>
            {
                ["AppTitle"] = "نظام الحضور والانصراف ZKTeco",
                ["AppSubtitle"] = "إدارة الموظفين والورديات • حساب العمل الإضافي والرواتب",
                ["Connect"] = "اتصال بالجهاز",
                ["GetLogs"] = "سحب السجلات",
                ["SyncDeviceTime"] = "مزامنة وقت الجهاز",
                ["Employees"] = "بيانات الموظفين",
                ["Groups"] = "المجموعات والحالات",
                ["Shifts"] = "قواعد الورديات",
                
                ["KpiConnection"] = "حالة الاتصال",
                ["KpiDeviceTime"] = "وقت الجهاز (RTC)",
                ["KpiEmpCount"] = "إجمالي الموظفين",
                ["KpiTotalOt"] = "إجمالي الساعات الإضافية",
                ["KpiSatHours"] = "عمل السبت",
                ["KpiSunHours"] = "عمل الأحد والعطلات",
                
                ["TabReport"] = "  تقرير الحضور والعمل الإضافي  ",
                ["TabLogs"] = "  سجلات الحركات  ",
                ["TabEmpPanel"] = "  ملخص الموظف  ",
                
                ["Year"] = "السنة:",
                ["Month"] = "الشهر:",
                ["Day"] = "اليوم:",
                ["Group"] = "المجموعة:",
                ["Type"] = "النوع:",
                ["Status"] = "الحالة:",
                ["SearchName"] = "بحث بالاسم...",
                ["SearchTc"] = "بحث بالرقم...",
                
                ["BtnLatestPeriod"] = "الانتقال لآخر فترة",
                ["BtnInternetTime"] = "الوقت المباشر (إنترنت)",
                ["BtnUpdateHolidays"] = "تحديث العطلات",
                ["BtnCalculate"] = "احتساب",
                ["BtnClearFilters"] = "إعادة تعيين الفلاتر",
                ["BtnExportMonthly"] = "تصدير الرواتب إلى Excel",
                ["BtnExportDaily"] = "تصدير التقرير اليومي",
                ["BtnRefreshDb"] = "تحديث قاعدة البيانات",
                ["BtnExportAllLogs"] = "تصدير السجلات كلها",
                ["BtnExportEmpDetail"] = "تصدير تفاصيل الموظف",
                
                ["InspectEmp"] = "الموظف المختار:",
                ["EmpSummaryTitle"] = "إجمالي الشهر المحدد",
                ["WorkedDays"] = "أيام العمل:",
                ["TotalNet"] = "صافي ساعات العمل:",
                ["WeekdayOt"] = "إضافي أيام العمل:",
                ["SaturdayOt"] = "ساعات السبت:",
                ["SundayOt"] = "ساعات الأحد والعطلات:",
                ["MorningOt"] = "إضافي الصباح:",
                ["EveningOt"] = "إضافي المساء:",
                ["NightOt"] = "العمل الليلي (24:00+):",
                ["ExcessBreak"] = "تجاوز وقت الاستراحة:",
                ["WeightedOt"] = "إجمالي الاستحقاق الإضافي:",
                ["MissingWork"] = "ساعات العمل المفقودة:",

                ["DayFilter_All"] = "جميع الأيام",
                ["DayFilter_Holidays"] = "العطل الرسمية فقط",
                ["DayFilter_HolidaysWeekend"] = "العطل وعطلة نهاية الأسبوع",
                ["DayFilter_Weekend"] = "السبت والأحد",
                ["DayFilter_Sat"] = "السبت فقط",
                ["DayFilter_Sun"] = "الأحد فقط",
                ["DayFilter_Weekdays"] = "أيام العمل العادية فقط",

                ["Status_All"] = "الكل",
                ["Status_Active"] = "النشطين فقط",
                ["Status_Passive"] = "المستقيلين",
                ["Status_OnLeave"] = "في إجازة"
            },
            ["ku"] = new Dictionary<string, string>
            {
                ["AppTitle"] = "Pergala Şopandina Karmendan ZKTeco",
                ["AppSubtitle"] = "Rêveberiya Karmendan • Şeva/Rojê • Hesabkirina Meaş û Mesaiyê",
                ["Connect"] = "Bi Amûrê Ve Girêbide",
                ["GetLogs"] = "Tomaran Bikişîne",
                ["SyncDeviceTime"] = "Saeta Amûrê Hevdem Bike",
                ["Employees"] = "Karmend & Nasname",
                ["Groups"] = "Kom & Rewş",
                ["Shifts"] = "Zêdekarî & Şift",
                
                ["KpiConnection"] = "REWŞA GİRÊDANÊ",
                ["KpiDeviceTime"] = "SAETA AMÛRÊ (RTC)",
                ["KpiEmpCount"] = "HEJMARA KARMENDAN",
                ["KpiTotalOt"] = "TOMA MESAÎYA GIŞTÎ",
                ["KpiSatHours"] = "XEBATA ŞEMİYÊ",
                ["KpiSunHours"] = "MESAÎYA ROJA YEKSÊ & BÎHNBEDANÊ",
                
                ["TabReport"] = "  Rapora Pûantaj & Mesaî  ",
                ["TabLogs"] = "  Tevgerên Ketin/Derketinê  ",
                ["TabEmpPanel"] = "  Panela Agahiyên Karmend  ",
                
                ["Year"] = "Sal:",
                ["Month"] = "Meh:",
                ["Day"] = "Roj:",
                ["Group"] = "Kom:",
                ["Type"] = "Cure:",
                ["Status"] = "Rewş:",
                ["SearchName"] = "Nav û Paşnav Bigerin...",
                ["SearchTc"] = "Nasname / TC Bigerin...",
                
                ["BtnLatestPeriod"] = "Here Serdema Dawî",
                ["BtnInternetTime"] = "Saeta Zindî (Înternet)",
                ["BtnUpdateHolidays"] = "Bêhnvedanan Nû Bike",
                ["BtnCalculate"] = "Hesab Bike",
                ["BtnClearFilters"] = "Parzûnan Paqij Bike",
                ["BtnExportMonthly"] = "Bordro Veguhezîne Excel",
                ["BtnExportDaily"] = "Pûantaja Rojane Excel",
                ["BtnRefreshDb"] = "Daneyan Nû Bike",
                ["BtnExportAllLogs"] = "Hemû Tomaran Excel",
                ["BtnExportEmpDetail"] = "Hûrguliyên Karmend Excel",
                
                ["InspectEmp"] = "Karmendê Hilbijartî:",
                ["EmpSummaryTitle"] = "KOMÊ MEHA HILBIJARTÎ",
                ["WorkedDays"] = "Rojên Xebitî:",
                ["TotalNet"] = "Tevahiya Xebata Net:",
                ["WeekdayOt"] = "Mesaîya Navrojê:",
                ["SaturdayOt"] = "Xebata Şemiyê:",
                ["SundayOt"] = "Xebata Yekşem & Betlaneyê:",
                ["MorningOt"] = "Mesaîya Sibehê:",
                ["EveningOt"] = "Mesaîya Êvarê:",
                ["NightOt"] = "Mesaîya Şevê (24:00+):",
                ["ExcessBreak"] = "Zêdebûna Bêhnvedanê:",
                ["WeightedOt"] = "Mesaîya Mafdar:",
                ["MissingWork"] = "Xebata Kêm:",

                ["DayFilter_All"] = "Hemû Roj",
                ["DayFilter_Holidays"] = "Tenê Bêhnvedanên Fermî",
                ["DayFilter_HolidaysWeekend"] = "Bêhnvedan & Dawiya Hefteyê",
                ["DayFilter_Weekend"] = "Şemî & Yekşem (Dawiya Hefteyê)",
                ["DayFilter_Sat"] = "Tenê Şemî",
                ["DayFilter_Sun"] = "Tenê Yekşem",
                ["DayFilter_Weekdays"] = "Tenê Rojên Hefteyê (Rojên Asayî)",

                ["Status_All"] = "Hemû Karmend",
                ["Status_Active"] = "Tenê Yên Çalak",
                ["Status_Passive"] = "Yên Veqetiyayî",
                ["Status_OnLeave"] = "Yên Di Destûrê De"
            }
        };

        public static string Get(string key)
        {
            if (Translations.ContainsKey(CurrentLanguage) && Translations[CurrentLanguage].ContainsKey(key))
            {
                return Translations[CurrentLanguage][key];
            }
            if (Translations["tr"].ContainsKey(key))
            {
                return Translations["tr"][key];
            }
            return key;
        }
    }
}
