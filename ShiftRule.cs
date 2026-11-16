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
    }

    public class ShiftAttendanceResult
    {
        public string UserId { get; set; }
        public string EmployeeName { get; set; }
        public string Department { get; set; }
        public string ShiftName { get; set; }
        public string DateString { get; set; }
        public string PlannedIn { get; set; }
        public string PlannedOut { get; set; }
        public string ActualFirstIn { get; set; }
        public string ActualLastOut { get; set; }
        public int LateMinutes { get; set; }
        public int EarlyLeaveMinutes { get; set; }

        // Gece 12 Öncesi & Sonrası Çalışma
        public double DayWorkHours { get; set; }
        public double NightWorkHours { get; set; }

        // Net ve Fazla Mesai Hesapları
        public double NetWorkHours { get; set; }       // Gerçekleşen toplam net çalışma (Saat)
        public double CalculatedOvertime { get; set; } // Hesaplanan ham fazla mesai
        public double ApprovedOvertime { get; set; }   // Firmanın kuralına göre ONAYLANAN/İZİN VERİLEN fazla mesai (Limitli)
        public double RejectedOvertime { get; set; }   // Limiti aştığı için KESİLEN/ONAYLANMAYAN fazla mesai
        
        public string Status { get; set; }             // Durum (Normal, Fazla Mesai, Limit Aşıldı, Eksik)
    }
}
