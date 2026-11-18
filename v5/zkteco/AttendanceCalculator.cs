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
            int month,
            Dictionary<string, string> holidays = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            IEnumerable<EmployeeShiftSchedule> shiftSchedules = null)
        {
            var results = new List<ShiftAttendanceResult>();
            var empList = employees?.ToList() ?? new List<Employee>();
            if (empList.Count == 0)
            {
                return results; // Hiç personel yoksa boş sonuç dön
            }

            var empDict = empList.ToDictionary(e => e.Badgenumber, e => e);
            var allowedUserIds = new HashSet<string>(empDict.Keys);
            var shiftDict = shifts?.ToDictionary(s => s.Id, s => s) ?? new Dictionary<int, ShiftRule>();
            var defaultShift = shifts?.FirstOrDefault() ?? new ShiftRule();

            // 1. Tarih Aralığı veya Ay/Yıl ve Filtrelenen Personel ID Filtresi
            var validLogs = new List<(string UserId, DateTime Time, string InOut)>();
            foreach (var log in rawLogs)
            {
                if (!allowedUserIds.Contains(log.UserId))
                {
                    continue; // Seçilen filtredeki personele ait değilse hesaplamaya katma
                }

                if (DateTime.TryParse(log.LogTime, out DateTime dt))
                {
                    if (startDate.HasValue && endDate.HasValue)
                    {
                        // İki Tarih Arası Filtresi (Günü kapsayacak şekilde)
                        if (dt.Date >= startDate.Value.Date && dt.Date <= endDate.Value.Date)
                        {
                            validLogs.Add((log.UserId, dt, log.InOutMode));
                        }
                    }
                    else
                    {
                        // Ay / Yıl Filtresi
                        if (dt.Year == year && dt.Month == month)
                        {
                            validLogs.Add((log.UserId, dt, log.InOutMode));
                        }
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
                var emp = empDict.ContainsKey(userId) ? empDict[userId] : new Employee { Badgenumber = userId, Name = $"Personel {userId}", Department = "Genel", GroupName = "Genel", EmploymentType = "Kadrolu", Status = "Aktif" };
                
                // Gruba veya Personele Özel Vardiya Kuralı Eşleştirmesi:
                // 0. EN YÜKSEK ÖNCELİK: Tarih Bazlı Vardiya Çizelgesi (Örn: 1. hafta 1. vardiya, sonraki hafta 2. vardiya)
                ShiftRule rule = null;
                if (shiftSchedules != null)
                {
                    var activeSched = shiftSchedules
                        .Where(s => s.UserId == userId && s.StartDate.Date <= day.Date && (!s.EndDate.HasValue || s.EndDate.Value.Date >= day.Date))
                        .OrderByDescending(s => s.StartDate)
                        .FirstOrDefault();

                    if (activeSched != null && shiftDict.ContainsKey(activeSched.ShiftId))
                    {
                        rule = shiftDict[activeSched.ShiftId];
                    }
                }

                // 1. Eğer o tarihe özel çizelge yoksa, personelin Özlük Kartında bireysel olarak atanmış vardiyası (ShiftId) kontrol edilir.
                if (rule == null && emp.ShiftId > 0 && shiftDict.ContainsKey(emp.ShiftId))
                {
                    rule = shiftDict[emp.ShiftId];
                }

                // 2. Eğer personelin özel bir ShiftId'si yoksa veya bulunamazsa, 
                // doğrudan o gruba/istihdam türüne atanmış "ApplyToAllMatchingEmployees" kuralı aranır (Örn: TargetGroup = "Taşeron" veya "Üretim").
                if (rule == null)
                {
                    rule = shifts.FirstOrDefault(s => s.ApplyToAllMatchingEmployees && 
                                                      !string.IsNullOrEmpty(s.TargetGroup) && 
                                                      !s.TargetGroup.Equals("Tümü", StringComparison.OrdinalIgnoreCase) &&
                                                      (s.TargetGroup.Equals(emp.EmploymentType, StringComparison.OrdinalIgnoreCase) || 
                                                       s.TargetGroup.Equals(emp.GroupName, StringComparison.OrdinalIgnoreCase)));
                }

                // 3. Hiçbiri eşleşmezse sistem varsayılan vardiyası (Shift 1 veya ilk aktif vardiya) uygulanır.
                if (rule == null)
                {
                    rule = defaultShift;
                }

                // Gün Tipi Belirleme (Hafta İçi, Hafta Sonu, Resmi Tatil)
                string dateKey = day.ToString("yyyy-MM-dd");
                bool isHoliday = holidays != null && holidays.ContainsKey(dateKey);
                string holidayName = isHoliday ? holidays[dateKey] : null;

                string dayType;
                double multiplier;

                if (isHoliday)
                {
                    dayType = $"Resmi Tatil ({holidayName})";
                    multiplier = rule.HolidayMultiplier > 0 ? rule.HolidayMultiplier : 2.0;
                }
                else if (day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday)
                {
                    dayType = "Hafta Sonu";
                    multiplier = rule.WeekendMultiplier;
                }
                else
                {
                    dayType = "Hafta İçi";
                    multiplier = rule.WeekdayMultiplier;
                }

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
                DateTime effectiveIn = firstIn;

                if (punches.Count > 1 && lastOut > firstIn)
                {
                    // 1. SABAH ERKEN GELME HESABI:
                    // Eğer sabah erken gelmeyi mesaiye alma KAPALI ise (varsayılan),
                    // planlanan mesai saatinden (plannedIn) önceki süreler normal çalışma veya mesaiye dahil edilmez.
                    // Eğer AÇIK ise, en fazla belirlenen saat kadar (EarlyInMaxOvertimeHours) geriye gidilip kabul edilir.
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
                    // Eğer personel OvertimeWindowEnd'den daha geç çıkarsa ve limitler aktifse, bu saatten sonrası mesaiye sayılmaz (kırpılır).
                    // Eğer DisableOvertimeLimits seçiliyse (fazladan mesai silinmesin) kırpma yapılmaz, fiili çıkış saati korunur.
                    DateTime effectiveOut = lastOut;
                    if (!rule.DisableOvertimeLimits)
                    {
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

                        // KURAL: Eğer 'ExcludeOvernightOvertime' (Akşamdan sabaha olan mesailer silinsin) seçilmişse
                        // 24:00 (gece yarısı) sonrasına sarkan mesailer silinir ve kırpılır
                        if (rule.ExcludeOvernightOvertime && effectiveOut > day.Date.AddDays(1))
                        {
                            effectiveOut = day.Date.AddDays(1);
                        }
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
                            // Eğer 'DisableOvertimeLimits' (fazladan mesai silinmesin) aktifse hiçbir kesinti yapılmaz.
                            if (!rule.DisableOvertimeLimits && rule.MaxDailyOvertimeHours > 0 && calculatedOt > rule.MaxDailyOvertimeHours)
                            {
                                approvedOt = rule.MaxDailyOvertimeHours;
                                rejectedOt = Math.Round(calculatedOt - rule.MaxDailyOvertimeHours, 2);
                                status = $"Limit Aşıldı ({approvedOt}s Onay / {rejectedOt}s İptal)";
                            }
                            else
                            {
                                approvedOt = calculatedOt;
                                rejectedOt = 0;
                                status = isHoliday ? $"{holidayName} Çalışması (Resmi Tatil)" : $"{dayType} Mesaisi";
                            }
                        }
                        else
                        {
                            status = isHoliday ? $"{holidayName} (Eşik Altı)" : "Eşik Altı (Mesai Sayılmadı)";
                        }
                    }
                    else if (isHoliday && netHours > 0)
                    {
                        // Resmi tatilde çalışma var
                        status = $"{holidayName} Çalışması";
                    }
                    else if (lateMins > 0 || earlyLeaveMins > 0 || netHours < standardWork)
                    {
                        double missingH = Math.Max(0, Math.Round(standardWork - netHours, 2));
                        if (lateMins > 0 && earlyLeaveMins > 0)
                            status = $"⚠️ Düzensiz Geçiş: Geç Giriş ({lateMins}dk) & Erken Çıkış ({missingH}s Eksik)";
                        else if (lateMins > 0)
                            status = $"⚠️ Düzensiz: Geç Giriş ({lateMins}dk, {missingH}s Eksik)";
                        else if (earlyLeaveMins > 0)
                            status = $"⚠️ Düzensiz: Erken Çıkış ({earlyLeaveMins}dk, {missingH}s Eksik)";
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
                    
                    status = $"⚠️ Düzensiz Basım (Tek Basım / Çıkış Yok, {standardWork}s Yok)";
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
                        // 1. Sabah Erken Gelme Mesaisi (Gece 00:00 ile Planlanan Giriş Saati arası)
                        if (firstIn < plannedIn)
                        {
                            if (rule.AllowEarlyInOvertime)
                            {
                                double rawMorningH = Math.Round((plannedIn - effectiveIn).TotalHours, 2);
                                morningOt = Math.Min(rawMorningH, approvedOt);
                            }
                            else
                            {
                                // Eğer kuralda izin verilmişse veya fiili erken gelme varsa
                                double rawMorningH = Math.Round((plannedIn - firstIn).TotalHours, 2);
                                morningOt = Math.Min(rawMorningH, approvedOt);
                            }
                        }

                        // Kalan mesai akşam (çıkış - 24:00) ve geceye (24:00+) dağıtılır
                        double remainingOt = Math.Max(0, Math.Round(approvedOt - morningOt, 2));

                        // 2. Gece 12'den sonraya sarkan çalışma (Gece 24:00 Sonrası Mesaisi)
                        if (nightWorkHours > 0)
                        {
                            nightShiftOt = Math.Min(nightWorkHours, remainingOt);
                            nightOvertime = nightShiftOt;
                        }

                        // 3. Akşam Mesaisi (Akşam çıkış saati ile Gece 24:00 arası fark)
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
                        TcNo = emp.TcNo,
                        Company = emp.Company,
                        Branch = emp.Branch,
                        Unit = emp.Unit,
                        Title = emp.Title,
                        CardNo = emp.CardNo,
                        Phone = emp.Phone,
                        CompanyPhone = emp.CompanyPhone,
                        HireDate = emp.HireDate,
                        LeaveDate = emp.LeaveDate,
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
                        Status = (excessBreakMins > 0) ? $"{status} [Mola Aşımı: +{excessBreakMins}dk]" : status,
                        EmployeeStatus = emp.Status ?? "Aktif"
                    });
            }

            // HAFTALIK VE AYLIK MESAİ SINIRLARI MOTORU (MaxWeeklyOvertimeHours & MaxMonthlyOvertimeHours):
            // Kişi bazında haftalık ve aylık toplam mesailer kontrol edilir, limiti aşan kısımlar RejectedOvertime'a aktarılır.
            var resultsByUser = results.GroupBy(r => r.UserId);
            foreach (var userGroup in resultsByUser)
            {
                string userId = userGroup.Key;
                var emp = empDict.ContainsKey(userId) ? empDict[userId] : null;
                int shiftId = emp?.ShiftId ?? 1;
                var rule = shiftDict.ContainsKey(shiftId) ? shiftDict[shiftId] : defaultShift;

                // 1. Haftalık Limit Kontrolü (Pazartesi-Pazar dilimi)
                if (!rule.DisableOvertimeLimits && rule.MaxWeeklyOvertimeHours > 0)
                {
                    var byWeek = userGroup.GroupBy(r =>
                    {
                        if (DateTime.TryParse(r.DateString, out DateTime dt))
                        {
                            var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
                            return cal.GetWeekOfYear(dt, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                        }
                        return 0;
                    });

                    foreach (var week in byWeek)
                    {
                        double weeklyAcc = 0;
                        foreach (var dayRec in week.OrderBy(r => r.DateString))
                        {
                            if (dayRec.ApprovedOvertime > 0)
                            {
                                if (weeklyAcc + dayRec.ApprovedOvertime > rule.MaxWeeklyOvertimeHours)
                                {
                                    double allowedInWeek = Math.Max(0, rule.MaxWeeklyOvertimeHours - weeklyAcc);
                                    double overWeek = Math.Round(dayRec.ApprovedOvertime - allowedInWeek, 2);
                                    dayRec.ApprovedOvertime = allowedInWeek;
                                    dayRec.RejectedOvertime = Math.Round(dayRec.RejectedOvertime + overWeek, 2);
                                    dayRec.WeightedOvertimeHours = Math.Round(dayRec.ApprovedOvertime * dayRec.OvertimeMultiplier, 2);
                                    dayRec.Status = $"{dayRec.Status} [Haftalık Limit Aşıldı: Max {rule.MaxWeeklyOvertimeHours}s]";
                                }
                                weeklyAcc += dayRec.ApprovedOvertime;
                            }
                        }
                    }
                }

                // 2. Aylık Limit Kontrolü
                if (!rule.DisableOvertimeLimits && rule.MaxMonthlyOvertimeHours > 0)
                {
                    double monthlyAcc = 0;
                    foreach (var dayRec in userGroup.OrderBy(r => r.DateString))
                    {
                        if (dayRec.ApprovedOvertime > 0)
                        {
                            if (monthlyAcc + dayRec.ApprovedOvertime > rule.MaxMonthlyOvertimeHours)
                            {
                                double allowedInMonth = Math.Max(0, rule.MaxMonthlyOvertimeHours - monthlyAcc);
                                double overMonth = Math.Round(dayRec.ApprovedOvertime - allowedInMonth, 2);
                                dayRec.ApprovedOvertime = allowedInMonth;
                                dayRec.RejectedOvertime = Math.Round(dayRec.RejectedOvertime + overMonth, 2);
                                dayRec.WeightedOvertimeHours = Math.Round(dayRec.ApprovedOvertime * dayRec.OvertimeMultiplier, 2);
                                dayRec.Status = $"{dayRec.Status} [Aylık Limit Aşıldı: Max {rule.MaxMonthlyOvertimeHours}s]";
                            }
                            monthlyAcc += dayRec.ApprovedOvertime;
                        }
                    }
                }
            }

            return results;
        }
    }
}
