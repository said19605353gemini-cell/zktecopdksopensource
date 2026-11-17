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

        // YENİ ALANLAR: Firmanın Günlük Mesai Limitleri ve Kuralları
        public double MaxDailyOvertimeHours { get; set; } = 3.0; // Bir günde izin verilen MAKSİMUM fazla mesai saati (Örn: En fazla 3 saat)
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
    }
}
