using System;
using System.Collections.Generic;
using System.Linq;

namespace ZkPdksApp
{
    public static class AttendanceCalculator
    {
        public static List<ShiftAttendanceResult> CalculateAdvancedShiftReport(
            IEnumerable<AttendanceLogModel> rawLogs,
            IEnumerable<Employee> employees,
            IEnumerable<ShiftRule> shifts,
            int year,
            int month)
        {
            var results = new List<ShiftAttendanceResult>();
            var empDict = employees?.ToDictionary(e => e.Badgenumber, e => e) ?? new Dictionary<string, Employee>();
            var shiftDict = shifts?.ToDictionary(s => s.Id, s => s) ?? new Dictionary<int, ShiftRule>();
            var defaultShift = shifts?.FirstOrDefault() ?? new ShiftRule();

            // 1. Ay ve Yıl Filtresi
            var validLogs = new List<(string UserId, DateTime Time, string InOut)>();
            foreach (var log in rawLogs)
            {
                if (DateTime.TryParse(log.LogTime, out DateTime dt))
                {
                    if (dt.Year == year && dt.Month == month)
                    {
                        validLogs.Add((log.UserId, dt, log.InOutMode));
                    }
                }
            }

            // 2. Mükerrer Okutma Filtresi (Duplicate Filter)
            var filteredLogs = new List<(string UserId, DateTime Time, string InOut)>();
            var groupedByUser = validLogs.GroupBy(x => x.UserId);

            foreach (var userGroup in groupedByUser)
            {
                var sorted = userGroup.OrderBy(x => x.Time).ToList();
                int shiftId = empDict.ContainsKey(userGroup.Key) ? empDict[userGroup.Key].ShiftId : 1;
                var rule = shiftDict.ContainsKey(shiftId) ? shiftDict[shiftId] : defaultShift;
                int filterSec = rule.DuplicateFilterSeconds > 0 ? rule.DuplicateFilterSeconds : 60;

                DateTime lastTime = DateTime.MinValue;
                foreach (var log in sorted)
                {
                    if ((log.Time - lastTime).TotalSeconds >= filterSec)
                    {
                        filteredLogs.Add(log);
                        lastTime = log.Time;
                    }
                }
            }

            // 3. Vardiya Analizi, Çalışma Saatleri ve Fazla Mesai Limit Motoru
            var dayGroups = filteredLogs.GroupBy(x => new { x.UserId, Date = x.Time.Date })
                                        .OrderBy(g => g.Key.Date)
                                        .ThenBy(g => g.Key.UserId);

            foreach (var group in dayGroups)
            {
                string userId = group.Key.UserId;
                DateTime day = group.Key.Date;
                var emp = empDict.ContainsKey(userId) ? empDict[userId] : new Employee { Badgenumber = userId, Name = $"Personel {userId}", Department = "Genel", GroupName = "Genel", EmploymentType = "Kadrolu" };
                
                // Gruba veya Personele Özel Vardiya Kuralı Eşleştirmesi:
                // 1. Önce doğrudan o gruba/istihdam türüne atanmış ve "ApplyToAllMatchingEmployees" aktif olan kural aranır.
                // Örneğin: Özlük kartında İstihdam Türü = "Taşeron" veya Grup = "Taşeron" ise; TargetGroup = "Taşeron" olan kural tümünde geçerli olur.
                var rule = shifts.FirstOrDefault(s => s.ApplyToAllMatchingEmployees && 
                                                      (s.TargetGroup.Equals(emp.EmploymentType, StringComparison.OrdinalIgnoreCase) || 
                                                       s.TargetGroup.Equals(emp.GroupName, StringComparison.OrdinalIgnoreCase)));
                
                // 2. Eğer gruba özel toplu kural bulunamazsa, personelin bireysel atanan vardiyası (ShiftId) kullanılır.
                if (rule == null)
                {
                    rule = shiftDict.ContainsKey(emp.ShiftId) ? shiftDict[emp.ShiftId] : defaultShift;
                }

                // Gün Tipi Belirleme (Hafta İçi, Hafta Sonu, Resmi Tatil)
                string dayType = (day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday) ? "Hafta Sonu" : "Hafta İçi";
                double multiplier = (dayType == "Hafta Sonu") ? rule.WeekendMultiplier : rule.WeekdayMultiplier;

                var punches = group.OrderBy(x => x.Time).ToList();
                DateTime firstIn = punches.First().Time;
                DateTime lastOut = punches.Last().Time;

                // Firmanın Belirlediği Giriş & Çıkış Saatleri
                DateTime plannedIn = day.Date + rule.StartTime;
                DateTime plannedOut = day.Date + rule.EndTime;
                if (rule.IsOvernight)
                {
                    plannedOut = day.Date.AddDays(1) + rule.EndTime;
                }

                // Tolerans & Geç Kalma Hesabı
                int lateMins = 0;
                DateTime graceLimit = plannedIn.AddMinutes(rule.GracePeriodMinutes);
                if (firstIn > graceLimit)
                {
                    lateMins = (int)(firstIn - plannedIn).TotalMinutes;
                }

                // Erken Çıkma Hesabı
                int earlyLeaveMins = 0;
                if (punches.Count > 1 && lastOut < plannedOut)
                {
                    earlyLeaveMins = (int)(plannedOut - lastOut).TotalMinutes;
                }

                // Gece 12 (00:00) Öncesi ve Sonrası Süre Hesabı
                double dayWorkHours = 0;
                double nightWorkHours = 0;
                double netHours = 0;
                double calculatedOt = 0;
                double approvedOt = 0;
                double rejectedOt = 0;
                double usedBreakMins = rule.LunchBreakMinutes;
                double allowedBreakMins = rule.LunchBreakMinutes;
                double excessBreakMins = 0;
                string status = "Normal";

                if (punches.Count > 1 && lastOut > firstIn)
                {
                    // 1. SABAH ERKEN GELME HESABI:
                    // Eğer sabah erken gelmeyi mesaiye alma KAPALI ise (varsayılan),
                    // planlanan mesai saatinden (plannedIn) önceki süreler normal çalışma veya mesaiye dahil edilmez.
                    // Eğer AÇIK ise, en fazla belirlenen saat kadar (EarlyInMaxOvertimeHours) geriye gidilip kabul edilir.
                    DateTime effectiveIn = firstIn;
                    if (firstIn < plannedIn)
                    {
                        if (rule.AllowEarlyInOvertime)
                        {
                            // En fazla EarlyInMaxOvertimeHours kadar erken gelme kabul edilir
                            DateTime allowedEarliest = plannedIn.AddHours(-rule.EarlyInMaxOvertimeHours);
                            if (firstIn < allowedEarliest)
                            {
                                effectiveIn = allowedEarliest;
                            }
                        }
                        else
                        {
                            // Sabah erken gelmeler mesaiye sayılmaz -> Mesai başlangıcı olarak plannedIn alınır
                            effectiveIn = plannedIn;
                        }
                    }

                    // 2. AKŞAM ÇIKIŞ VE MESAİ KABUL ARALIĞI:
                    // Akşam mesaisi sadece belirlenen pencere (OvertimeWindowStart ile OvertimeWindowEnd) arasında geçerlidir.
                    // Eğer personel OvertimeWindowEnd'den daha geç çıkarsa, bu saatten sonrası mesaiye sayılmaz (kırpılır).
                    DateTime effectiveOut = lastOut;
                    DateTime otWinStart = day.Date + rule.OvertimeWindowStart;
                    DateTime otWinEnd = day.Date + rule.OvertimeWindowEnd;
                    if (rule.IsOvernight || rule.OvertimeWindowEnd < rule.OvertimeWindowStart)
                    {
                        otWinEnd = day.Date.AddDays(1) + rule.OvertimeWindowEnd;
                    }

                    if (effectiveOut > otWinEnd)
                    {
                        effectiveOut = otWinEnd;
                    }

                    if (effectiveOut < effectiveIn)
                    {
                        effectiveOut = effectiveIn;
                    }

                    DateTime midnight = day.Date.AddDays(1); // Giriş yapılan günün gece 24:00'ı (00:00)

                    // GERÇEK MOLA TAKİBİ (Parmak İzi / Kart ile Yapılan Ara Çıkış & Dönüşler)
                    // Eğer gün içinde 2'den fazla okutma varsa (Giriş, Mola Çıkış, Mola Giriş, vb. Çıkış),
                    // çiftler arasındaki okutmalar (örn: 2. okutma ile 3. okutma arası) personelin fiili mola süresidir.
                    double actualBreakMins = 0;
                    if (punches.Count >= 4)
                    {
                        for (int pi = 1; pi < punches.Count - 1; pi += 2)
                        {
                            if (pi + 1 < punches.Count)
                            {
                                double bMins = (punches[pi + 1].Time - punches[pi].Time).TotalMinutes;
                                if (bMins > 0)
                                {
                                    actualBreakMins += bMins;
                                }
                            }
                        }
                    }

                    allowedBreakMins = rule.LunchBreakMinutes;
                    usedBreakMins = (actualBreakMins > 0) ? Math.Round(actualBreakMins, 1) : allowedBreakMins;
                    excessBreakMins = (actualBreakMins > allowedBreakMins) ? Math.Round(actualBreakMins - allowedBreakMins, 1) : 0;

                    double lunchHoursDeduction = usedBreakMins / 60.0;

                    if (effectiveOut <= midnight)
                    {
                        double rawHours = (effectiveOut - effectiveIn).TotalHours;
                        dayWorkHours = Math.Max(0, Math.Round(rawHours - lunchHoursDeduction, 2));
                        nightWorkHours = 0;
                    }
                    else if (effectiveIn >= midnight)
                    {
                        double rawHours = (effectiveOut - effectiveIn).TotalHours;
                        nightWorkHours = Math.Max(0, Math.Round(rawHours - lunchHoursDeduction, 2));
                        dayWorkHours = 0;
                    }
                    else
                    {
                        double rawBefore12 = (midnight - effectiveIn).TotalHours;
                        double rawAfter12 = (effectiveOut - midnight).TotalHours;
                        double totalRaw = rawBefore12 + rawAfter12;
                        if (totalRaw > 0)
                        {
                            dayWorkHours = Math.Max(0, Math.Round(rawBefore12 - (lunchHoursDeduction * (rawBefore12 / totalRaw)), 2));
                            nightWorkHours = Math.Max(0, Math.Round(rawAfter12 - (lunchHoursDeduction * (rawAfter12 / totalRaw)), 2));
                        }
                    }

                    netHours = Math.Round(dayWorkHours + nightWorkHours, 2);

                    // FİRMANIN ÇALIŞMA SAATİNE GÖRE FAZLA MESAİ VE LİMİT HESABI
                    // Firmanın öngördüğü net çalışma saati:
                    double standardWork = rule.RequiredDailyWorkHours > 0 
                        ? rule.RequiredDailyWorkHours 
                        : Math.Max(0, (plannedOut - plannedIn).TotalHours - (rule.LunchBreakMinutes / 60.0));

                    if (netHours > standardWork)
                    {
                        double rawOvertime = netHours - standardWork;
                        double thresholdHours = rule.MinOvertimeThresholdMinutes / 60.0;

                        // Eğer asgari mesai eşiğini (örn: 30 dk) aşıyorsa mesaiye hak kazanır
                        if (rawOvertime >= thresholdHours)
                        {
                            // Yuvarlama kuralı uygulama (örn: 15, 30 veya 60 dk)
                            if (rule.RoundingMinutes > 0)
                            {
                                double totalOtMinutes = rawOvertime * 60.0;
                                double remainder = totalOtMinutes % rule.RoundingMinutes;
                                double roundedMinutes = (remainder >= (rule.RoundingMinutes / 2.0))
                                    ? totalOtMinutes + (rule.RoundingMinutes - remainder)
                                    : totalOtMinutes - remainder;
                                calculatedOt = Math.Round(Math.Max(0, roundedMinutes / 60.0), 2);
                            }
                            else
                            {
                                calculatedOt = Math.Round(rawOvertime, 2);
                            }

                            // Firmanın günlük izin verdiği azami mesai sınırı (MaxDailyOvertimeHours):
                            if (rule.MaxDailyOvertimeHours > 0 && calculatedOt > rule.MaxDailyOvertimeHours)
                            {
                                approvedOt = rule.MaxDailyOvertimeHours;
                                rejectedOt = Math.Round(calculatedOt - rule.MaxDailyOvertimeHours, 2);
                                status = $"Limit Aşıldı ({approvedOt}s Onay / {rejectedOt}s İptal)";
                            }
                            else
                            {
                                approvedOt = calculatedOt;
                                rejectedOt = 0;
                                status = $"{dayType} Mesaisi";
                            }
                        }
                        else
                        {
                            status = "Eşik Altı (Mesai Sayılmadı)";
                        }
                    }
                    else if (lateMins > 0 || earlyLeaveMins > 0 || netHours < standardWork)
                    {
                        double missingH = Math.Max(0, Math.Round(standardWork - netHours, 2));
                        if (lateMins > 0 && earlyLeaveMins > 0)
                            status = $"Geç Giriş ({lateMins}dk) & Erken Çıkış ({missingH}s Eksik)";
                        else if (lateMins > 0)
                            status = $"Geç Giriş ({lateMins}dk, {missingH}s Eksik)";
                        else if (earlyLeaveMins > 0)
                            status = $"Erken Çıkış ({earlyLeaveMins}dk, {missingH}s Eksik)";
                        else
                            status = $"Eksik Çalışma ({missingH}s Yok)";
                    }
                    else
                    {
                        status = "Tam Vardiya";
                    }
                }
                else
                {
                    double standardWork = rule.RequiredDailyWorkHours > 0 
                        ? rule.RequiredDailyWorkHours 
                        : Math.Max(0, (plannedOut - plannedIn).TotalHours - (rule.LunchBreakMinutes / 60.0));
                    
                    status = $"Eksik (Tek Geçiş / {standardWork}s Yok)";
                }

                double expectedWork = rule.RequiredDailyWorkHours > 0 
                    ? rule.RequiredDailyWorkHours 
                    : Math.Max(0, (plannedOut - plannedIn).TotalHours - (rule.LunchBreakMinutes / 60.0));
                double missingHoursTotal = Math.Max(0, Math.Round(expectedWork - netHours, 2));
                double weightedOt = Math.Round(approvedOt * multiplier, 2);

                    // 12 Öncesi ve Sonrası Mesai Ayrımı (DayOvertime / NightOvertime)
                    double dayOvertime = 0;
                    double nightOvertime = 0;

                    // 3'LÜ MESAİ AYRIMI: Sabah Mesaisi, Akşam Mesaisi, Gece Mesaisi
                    double morningOt = 0;
                    double eveningOt = 0;
                    double nightShiftOt = 0;

                    if (approvedOt > 0)
                    {
                        // 1. Sabah Erken Gelme Mesaisi (Planlanan giriş saatinden önceki çalışma)
                        if (rule.AllowEarlyInOvertime && firstIn < plannedIn)
                        {
                            double rawMorningH = Math.Round((plannedIn - effectiveIn).TotalHours, 2);
                            morningOt = Math.Min(rawMorningH, approvedOt);
                        }

                        // Kalan mesai akşam ve geceye dağıtılır
                        double remainingOt = Math.Max(0, Math.Round(approvedOt - morningOt, 2));

                        // 2. Gece 12'den sonraya sarkan çalışma (Gece Mesaisi)
                        if (nightWorkHours > 0)
                        {
                            nightShiftOt = Math.Min(nightWorkHours, remainingOt);
                            nightOvertime = nightShiftOt;
                        }

                        // 3. Akşam Mesaisi (İş çıkışı ile 24:00 arası)
                        eveningOt = Math.Max(0, Math.Round(remainingOt - nightShiftOt, 2));
                        dayOvertime = Math.Round(morningOt + eveningOt, 2);
                    }

                    results.Add(new ShiftAttendanceResult
                    {
                        UserId = userId,
                        EmployeeName = emp.Name,
                        GroupName = emp.GroupName ?? "Genel",
                        EmploymentType = emp.EmploymentType ?? "Kadrolu",
                        Department = emp.Department,
                        ShiftName = rule.Name,
                        DateString = day.ToString("yyyy-MM-dd"),
                        DayType = dayType,
                        PlannedIn = plannedIn.ToString("HH:mm"),
                        PlannedOut = plannedOut.ToString("HH:mm"),
                        ActualFirstIn = firstIn.ToString("HH:mm:ss"),
                        ActualLastOut = punches.Count > 1 ? lastOut.ToString("HH:mm:ss") : "-",
                        LateMinutes = lateMins,
                        EarlyLeaveMinutes = earlyLeaveMins,
                        MissingWorkHours = missingHoursTotal,
                        DayWorkHours = dayWorkHours,
                        NightWorkHours = nightWorkHours,
                        DayOvertimeHours = dayOvertime,
                        NightOvertimeHours = nightOvertime,
                        MorningOvertimeHours = morningOt,
                        EveningOvertimeHours = eveningOt,
                        NightShiftOvertimeHours = nightShiftOt,
                        ActualBreakMinutes = usedBreakMins,
                        AllowedBreakMinutes = allowedBreakMins,
                        ExcessBreakMinutes = excessBreakMins,
                        NetWorkHours = netHours,
                        CalculatedOvertime = calculatedOt,
                        ApprovedOvertime = approvedOt,
                        RejectedOvertime = rejectedOt,
                        OvertimeMultiplier = multiplier,
                        WeightedOvertimeHours = weightedOt,
                        Status = (excessBreakMins > 0) ? $"{status} [Mola Aşımı: +{excessBreakMins}dk]" : status
                    });
            }

            return results;
        }
    }
}
