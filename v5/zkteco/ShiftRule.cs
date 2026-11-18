using System;

namespace ZkPdksApp
{
    public class ShiftRule
    {
        public int Id { get; set; }
        public string Name { get; set; } // Örn: Firma Standart (08:30 - 18:00)
        public TimeSpan StartTime { get; set; } = new TimeSpan(8, 30, 0); // İşe Giriş Saati
        public TimeSpan EndTime { get; set; } = new TimeSpan(18, 0, 0);   // İşten Çıkış Saati
        public int GracePeriodMinutes { get; set; } = 10; // Geç kalma toleransı (dk)
        public int EarlyInToleranceMinutes { get; set; } = 15; // Erken gelme toleransı (dk)
        public int LunchBreakMinutes { get; set; } = 60; // Otomatik yemek molası (dk)
        public int DuplicateFilterSeconds { get; set; } = 60; // Mükerrer okutma filtresi (sn)
        public bool IsOvernight { get; set; } = false; // Gece yarısını aşan vardiya mı?

        // YENİ ALANLAR: Firmanın Günlük, Haftalık ve Aylık Mesai Limitleri
        public bool DisableOvertimeLimits { get; set; } = false; // Fazladan mesai hiç silinmesin / kırpılmasın (Tüm mesailer eksiksiz hakedişe sayılsın)
        public double MaxDailyOvertimeHours { get; set; } = 3.0; // Bir günde izin verilen MAKSİMUM fazla mesai saati (0: limitsiz)
        public double MaxWeeklyOvertimeHours { get; set; } = 15.0; // Bir haftada izin verilen MAKSİMUM fazla mesai saati (0: limitsiz)
        public double MaxMonthlyOvertimeHours { get; set; } = 45.0; // Bir ayda izin verilen MAKSİMUM fazla mesai saati (0: limitsiz)
        public bool ExcludeOvernightOvertime { get; set; } = false; // Akşamdan sabaha kadar olan (gece 24:00+ veya gece vardiyası) fazla mesailer silinsin / sayılmasın mı?
        public double MinOvertimeThresholdMinutes { get; set; } = 30; // Fazla mesai sayılması için gereken asgari süre (Örn: 30 dk altı yuvarlansın/sayılmasın)
        public double RequiredDailyWorkHours { get; set; } = 8.0; // Firmanın günlük zorunlu net çalışma saati

        // SABAH & AKŞAM MESAİ KABUL PENCERESİ VE SINIRLARI
        public bool AllowEarlyInOvertime { get; set; } = false; // Sabah vaktinden önce erken gelmeler mesaiye sayılsın mı?
        public double EarlyInMaxOvertimeHours { get; set; } = 1.0; // Sabah mesaisinden önce en fazla kaç saat mesaiye dahil edilebilir (Örn: En fazla 1 saat)
        public TimeSpan OvertimeWindowStart { get; set; } = new TimeSpan(18, 0, 0); // Akşam mesai sayılmaya başlama saati (Örn: 18:00)
        public TimeSpan OvertimeWindowEnd { get; set; } = new TimeSpan(22, 0, 0);   // Akşam mesainin son bulduğu / kabul edildiği bitiş saati (Örn: 22:00)

        // MESAİ ÇARPANLARI VE GRUP EŞLEŞTİRMESİ
        public string TargetGroup { get; set; } = "Tümü"; // Bu kural hangi gruba uygulanacak? (Örn: Taşeron, Üretim, Yönetim, Tümü)
        public bool ApplyToAllMatchingEmployees { get; set; } = true; // Bu gruptaki/istihdam türündeki tüm personellere bu kural otomatik uygulansın mı?
        public double WeekdayMultiplier { get; set; } = 1.5; // Hafta içi mesai katsayısı (%50 fazlası: 1.5)
        public double WeekendMultiplier { get; set; } = 2.0; // Hafta sonu mesai katsayısı (%100: 2.0)
        public double HolidayMultiplier { get; set; } = 2.0; // Resmi tatil katsayısı
        public int RoundingMinutes { get; set; } = 15; // Yuvarlama kuralı (0, 15, 30, 60 dk)
    }

    public class EmployeeShiftSchedule
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int ShiftId { get; set; }
        public string ShiftName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Description { get; set; }
    }

    public class LeaveRecord
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string LeaveType { get; set; } // Yıllık İzin, Rapor, Mazeret, Ücretsiz, Resmi Tatil vb.
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public double Hours { get; set; } = 8.0;
        public bool IsPaid { get; set; } = true;
        public string Description { get; set; }
    }

    public class ShiftAttendanceResult
    {
        public string UserId { get; set; }
        public string EmployeeName { get; set; }
        public string GroupName { get; set; }
        public string EmploymentType { get; set; } // Taşeron, Kadrolu, Ziyaretçi
        public string Department { get; set; }
        public string ShiftName { get; set; }
        public string DateString { get; set; }
        public string DayType { get; set; } = "Hafta İçi"; // Hafta İçi, Hafta Sonu, Resmi Tatil
        public string PlannedIn { get; set; }
        public string PlannedOut { get; set; }
        public string ActualFirstIn { get; set; }
        public string ActualLastOut { get; set; }
        public int LateMinutes { get; set; }
        public int EarlyLeaveMinutes { get; set; }
        public double MissingWorkHours { get; set; }   // Gün içinde eksik/yapılmayan çalışma saati (Geç girme, erken çıkma kaynaklı)

        // Gece 12 Öncesi & Sonrası Çalışma
        public double DayWorkHours { get; set; }
        public double NightWorkHours { get; set; }

        // Gece 12 Öncesi & Sonrası Mesai Ayrımı
        public double DayOvertimeHours { get; set; }   // 12 Öncesi Yapılan Mesai (Saat)
        public double NightOvertimeHours { get; set; } // 12 Sonrası Yapılan Mesai (Saat)

        // 3'LÜ MESAİ AYRIMI: Sabah Mesaisi, Akşam Mesaisi, Gece Mesaisi
        public double MorningOvertimeHours { get; set; } // Sabah erken gelme mesaisi (Saat)
        public double EveningOvertimeHours { get; set; } // Akşam mesaisi (İş çıkışı ile 24:00 arası) (Saat)
        public double NightShiftOvertimeHours { get; set; } // Gece mesaisi (24:00 sonrası) (Saat)

        // Saat & Dakika Formatlı Metin Gösterimleri (Örn: "2 Sa 30 Dk")
        public string DayWorkHoursDisplay => FormatHoursToHourMinute(DayWorkHours);
        public string NightWorkHoursDisplay => FormatHoursToHourMinute(NightWorkHours);
        public string NetWorkHoursDisplay => FormatHoursToHourMinute(NetWorkHours);
        public string MorningOvertimeHoursDisplay => FormatHoursToHourMinute(MorningOvertimeHours);
        public string EveningOvertimeHoursDisplay => FormatHoursToHourMinute(EveningOvertimeHours);
        public string NightShiftOvertimeHoursDisplay => FormatHoursToHourMinute(NightShiftOvertimeHours);
        public string ApprovedOvertimeDisplay => FormatHoursToHourMinute(ApprovedOvertime);
        public string WeightedOvertimeHoursDisplay => FormatHoursToHourMinute(WeightedOvertimeHours);
        public string RejectedOvertimeDisplay => FormatHoursToHourMinute(RejectedOvertime);
        public string MissingWorkHoursDisplay => FormatHoursToHourMinute(MissingWorkHours);

        public static string FormatHoursToHourMinute(double hours)
        {
            if (hours <= 0) return "0 Sa 0 Dk";
            int totalMins = (int)Math.Round(hours * 60.0);
            int h = totalMins / 60;
            int m = totalMins % 60;
            if (h == 0) return $"{m} Dk";
            if (m == 0) return $"{h} Sa";
            return $"{h} Sa {m} Dk";
        }

        // Mola Takibi (Parmak İzi / Kart ile Ara Giriş-Çıkışlar)
        public double ActualBreakMinutes { get; set; }   // Gerçekleşen toplam mola süresi (dk)
        public double AllowedBreakMinutes { get; set; }  // İzin verilen mola süresi (dk)
        public double ExcessBreakMinutes { get; set; }   // Mola aşım süresi (dk) (İzin verileni aşan kısım)

        // Net ve Fazla Mesai Hesapları
        public double NetWorkHours { get; set; }       // Gerçekleşen toplam net çalışma (Saat)
        public double CalculatedOvertime { get; set; } // Hesaplanan ham fazla mesai
        public double ApprovedOvertime { get; set; }   // Firmanın kuralına göre ONAYLANAN/İZİN VERİLEN fazla mesai (Limitli)
        public double RejectedOvertime { get; set; }   // Limiti aştığı için KESİLEN/ONAYLANMAYAN fazla mesai
        
        // Bordro & Puantaj Çarpanlı Mesai
        public double OvertimeMultiplier { get; set; } = 1.5; // Uygulanan çarpan
        public double WeightedOvertimeHours { get; set; }     // Çarpanlı hakediş saati (Örn: 2 saat * 1.5 = 3 saat hakediş)
        public double LeaveHours { get; set; }                 // İzin saati
        public string LeaveType { get; set; }                 // İzin türü

        public string Status { get; set; }             // Durum (Normal, Fazla Mesai, Limit Aşıldı, Eksik)
        public string EmployeeStatus { get; set; } = "Aktif"; // Aktif, Pasif (Ayrıldı), İzinli

        // Özlük Kartı Alanları (Bordro & Mesai Eşleşmesi)
        public string TcNo { get; set; }
        public string Company { get; set; }
        public string Branch { get; set; }
        public string Unit { get; set; }
        public string Title { get; set; }
        public string CardNo { get; set; }
        public string Phone { get; set; }
        public string CompanyPhone { get; set; } // Şirket / Kurumsal Hat
        public string HireDate { get; set; }  // İşe Giriş / Aktif Olma Tarihi
        public string LeaveDate { get; set; } // İşten Ayrılış / Pasif Olma Tarihi
    }
}
