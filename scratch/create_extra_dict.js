const fs = require('fs');

const extraDict = {
    "Unit_Person": {
        tr: "Kişi",
        en: "Staff",
        es: "Personas",
        zh: "人",
        fr: "Personnes",
        de: "Personen",
        ru: "Сотрудников",
        ar: "شخص",
        ku: "Kes"
    },
    "Unit_Hour": {
        tr: "Sa",
        en: "h",
        es: "h",
        zh: "小时",
        fr: "h",
        de: "Std",
        ru: "ч",
        ar: "س",
        ku: "Se"
    },
    "Unit_Minute": {
        tr: "Dk",
        en: "min",
        es: "min",
        zh: "分钟",
        fr: "min",
        de: "Min",
        ru: "мин",
        ar: "د",
        ku: "Xû"
    },
    "Unit_PunchCount": {
        tr: "{0} Basım Kaydı",
        en: "{0} Punch Logs",
        es: "{0} Registros de Marcas",
        zh: "{0} 条打卡记录",
        fr: "{0} Enregistrements de Pointage",
        de: "{0} Buchungssätze",
        ru: "{0} записей проходов",
        ar: "{0} سجل بصمة",
        ku: "{0} Qeydên Derbasbûnê"
    },
    "Unit_PassCount": {
        tr: "{0} Geçiş Kaydı",
        en: "{0} Attendance Logs",
        es: "{0} Registros de Asistencia",
        zh: "{0} 条通行记录",
        fr: "{0} Lignes de Présence",
        de: "{0} Anwesenheitsprotokolle",
        ru: "{0} записей посещаемости",
        ar: "{0} سجل حركة",
        ku: "{0} Qeydên Derbasbûnê"
    },
    "Conn_Active": {
        tr: "Bağlandı (Aktif)",
        en: "Connected (Active)",
        es: "Conectado (Activo)",
        zh: "已连接 (在线)",
        fr: "Connecté (Actif)",
        de: "Verbunden (Aktiv)",
        ru: "Подключено (Активно)",
        ar: "متصل (نشط)",
        ku: "Girêdayî ye (Çalak)"
    },
    "Conn_Connecting": {
        tr: "Bağlanıyor...",
        en: "Connecting...",
        es: "Conectando...",
        zh: "正在连接...",
        fr: "Connexion en cours...",
        de: "Verbinde...",
        ru: "Подключение...",
        ar: "جارٍ الاتصال...",
        ku: "Tê girêdan..."
    },
    "Conn_Failed": {
        tr: "Bağlanamadı",
        en: "Connection Failed",
        es: "Conexión Fallida",
        zh: "连接失败",
        fr: "Échec de connexion",
        de: "Verbindung fehlgeschlagen",
        ru: "Не подключено",
        ar: "فشل الاتصال",
        ku: "Girêdan çênebû"
    },
    "Conn_Error": {
        tr: "Hata Oluştu",
        en: "Error Occurred",
        es: "Ocurrió un Error",
        zh: "发生错误",
        fr: "Une erreur est survenue",
        de: "Fehler aufgetreten",
        ru: "Произошла ошибка",
        ar: "حدث خطأ",
        ku: "Çewtî derket"
    },
    "Conn_NoConnection": {
        tr: "Bağlantı Yok",
        en: "No Connection",
        es: "Sin Conexión",
        zh: "未连接",
        fr: "Pas de connexion",
        de: "Keine Verbindung",
        ru: "Нет соединения",
        ar: "لا يوجد اتصال",
        ku: "Girêdan Tune"
    },
    "Punch_In": {
        tr: "Giriş",
        en: "Check-In",
        es: "Entrada",
        zh: "上班打卡(进)",
        fr: "Entrée",
        de: "Kommen",
        ru: "Вход",
        ar: "دخول",
        ku: "Ketin"
    },
    "Punch_Out": {
        tr: "Çıkış",
        en: "Check-Out",
        es: "Salida",
        zh: "下班打卡(出)",
        fr: "Sortie",
        de: "Gehen",
        ru: "Выход",
        ar: "خروج",
        ku: "Derketin"
    },
    "Punch_BreakOut": {
        tr: "Mola Çıkışı",
        en: "Break Out",
        es: "Salida Descanso",
        zh: "外出休息",
        fr: "Sortie Pause",
        de: "Pause Beginn",
        ru: "Выход на перерыв",
        ar: "خروج استراحة",
        ku: "Derketina Bêhnvedanê"
    },
    "Punch_BreakIn": {
        tr: "Mola Dönüşü",
        en: "Break In",
        es: "Retorno Descanso",
        zh: "休息返回",
        fr: "Retour Pause",
        de: "Pause Ende",
        ru: "Возврат с перерыва",
        ar: "عودة من الاستراحة",
        ku: "Vegera Bêhnvedanê"
    },
    "Punch_OtIn": {
        tr: "Fazla Mesai Giriş",
        en: "Overtime In",
        es: "Entrada Horas Extra",
        zh: "加班签到",
        fr: "Entrée Heures Supp",
        de: "Überstunden Beginn",
        ru: "Вход сверхурочно",
        ar: "دخول عمل إضافي",
        ku: "Ketina Zêdedemiyê"
    },
    "Punch_OtOut": {
        tr: "Fazla Mesai Çıkış",
        en: "Overtime Out",
        es: "Salida Horas Extra",
        zh: "加班签退",
        fr: "Sortie Heures Supp",
        de: "Überstunden Ende",
        ru: "Выход сверхурочно",
        ar: "خروج عمل إضافي",
        ku: "Derketina Zêdedemiyê"
    },
    "Verify_Finger": {
        tr: "Parmak İzi",
        en: "Fingerprint",
        es: "Huella Digital",
        zh: "指纹识别",
        fr: "Empreinte",
        de: "Fingerabdruck",
        ru: "Отпечаток пальца",
        ar: "بصمة الإصبع",
        ku: "Şopa Tilî"
    },
    "Verify_Card": {
        tr: "Kart (RFID)",
        en: "Card (RFID)",
        es: "Tarjeta (RFID)",
        zh: "感应卡(RFID)",
        fr: "Carte (RFID)",
        de: "Karte (RFID)",
        ru: "Карта (RFID)",
        ar: "بطاقة (RFID)",
        ku: "Qert (RFID)"
    },
    "Verify_Face": {
        tr: "Yüz Tanıma",
        en: "Facial Recognition",
        es: "Reconocimiento Facial",
        zh: "人脸识别",
        fr: "Reconnaissance Faciale",
        de: "Gesichtserkennung",
        ru: "Распознавание лиц",
        ar: "بصمة الوجه",
        ku: "Nasîna Rû"
    },
    "Verify_Pin": {
        tr: "PIN / Şifre",
        en: "PIN / Password",
        es: "PIN / Contraseña",
        zh: "工号/密码",
        fr: "Code PIN / Mot de passe",
        de: "PIN / Passwort",
        ru: "ПИН / Пароль",
        ar: "رمز PIN / كلمة المرور",
        ku: "PIN / Şîfre"
    },
    "Verify_Other": {
        tr: "Şifre / Diğer",
        en: "Other / Password",
        es: "Otro / Clave",
        zh: "其他 / 密码",
        fr: "Autre / Mot de passe",
        de: "Andere / Passwort",
        ru: "Другое / Пароль",
        ar: "أخرى / كلمة مرور",
        ku: "Yên din / Şîfre"
    },
    "DayType_Weekday": {
        tr: "Hafta İçi",
        en: "Weekday",
        es: "Día Laborable",
        zh: "工作日(平日)",
        fr: "Jour de semaine",
        de: "Werktag",
        ru: "Будний день",
        ar: "يوم عمل عادي",
        ku: "Rojên Heyfteyê"
    },
    "DayType_Weekend": {
        tr: "Hafta Sonu",
        en: "Weekend",
        es: "Fin de Semana",
        zh: "周末双休",
        fr: "Week-end",
        de: "Wochenende",
        ru: "Выходной день",
        ar: "عطلة نهاية الأسبوع",
        ku: "Dawiya Heyfteyê"
    },
    "DayType_PublicHoliday": {
        tr: "Resmi Tatil",
        en: "Public Holiday",
        es: "Feriado Oficial",
        zh: "法定节假日",
        fr: "Jour férié",
        de: "Gesetzlicher Feiertag",
        ru: "Государственный праздник",
        ar: "عطلة رسمية",
        ku: "Bêhnvedana Fermî"
    },
    "Status_FullShift": {
        tr: "Tam Vardiya",
        en: "Full Shift Completed",
        es: "Turno Completo",
        zh: "正常全勤",
        fr: "Shift Complet",
        de: "Vollschicht",
        ru: "Полная смена",
        ar: "دوام كامل مكتمل",
        ku: "Nobeta Temam"
    },
    "Status_UnderThreshold": {
        tr: "Eşik Altı (Mesai Sayılmadı)",
        en: "Below Threshold (No Overtime)",
        es: "Bajo Umbral (Sin Horas Extra)",
        zh: "未达起算阈值 (不计加班)",
        fr: "Sous le seuil (Heures supp non comptées)",
        de: "Unter Schwellenwert (Keine Überstunden)",
        ru: "Ниже порога (без переработки)",
        ar: "أقل من الحد الأدنى (لا يوجد إضافي)",
        ku: "Binê Sînorê (Zêdedemî Nehat Hesibandin)"
    },
    "Status_IrregularSinglePunch": {
        tr: "⚠️ Düzensiz Basım (Tek Basım / Çıkış Yok)",
        en: "⚠️ Irregular Punch (Single Punch / Missing Out)",
        es: "⚠️ Marca Irregular (Marca Única / Falta Salida)",
        zh: "⚠️ 异常打卡 (单次打卡 / 缺签退)",
        fr: "⚠️ Pointage Irrégulier (Pointage Unique / Sortie Manquante)",
        de: "⚠️ Unvollständige Buchung (Nur eine Buchung / Kein Gehen)",
        ru: "⚠️ Неполная отметка (Одна отметка / Нет выхода)",
        ar: "⚠️ بصمة غير مكتملة (بصمة واحدة / بدون خروج)",
        ku: "⚠️ Derbasbûna Duzensiz (Yek Derbasbûn / Derketin Tune)"
    }
};

fs.writeFileSync('C:/Users/SaidNurs/Desktop/v5/zkteco/scratch/extra_dict.json', JSON.stringify(extraDict, null, 2), 'utf8');
console.log('Saved extra dict.');
