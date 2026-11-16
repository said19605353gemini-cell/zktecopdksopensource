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
                var emp = empDict.ContainsKey(userId) ? empDict[userId] : new Employee { Badgenumber = userId, Name = $"Personel {userId}", Department = "Genel" };
                var rule = shiftDict.ContainsKey(emp.ShiftId) ? shiftDict[emp.ShiftId] : defaultShift;

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
                string status = "Normal";

                if (punches.Count > 1 && lastOut > firstIn)
                {
                    DateTime midnight = day.Date.AddDays(1); // Giriş yapılan günün gece 24:00'ı (00:00)

                    if (lastOut <= midnight)
                    {
                        double rawHours = (lastOut - firstIn).TotalHours;
                        double lunch = rule.LunchBreakMinutes / 60.0;
                        dayWorkHours = Math.Max(0, Math.Round(rawHours - lunch, 2));
                        nightWorkHours = 0;
                    }
                    else if (firstIn >= midnight)
                    {
                        double rawHours = (lastOut - firstIn).TotalHours;
                        double lunch = rule.LunchBreakMinutes / 60.0;
                        nightWorkHours = Math.Max(0, Math.Round(rawHours - lunch, 2));
                        dayWorkHours = 0;
                    }
                    else
                    {
                        double rawBefore12 = (midnight - firstIn).TotalHours;
                        double rawAfter12 = (lastOut - midnight).TotalHours;
                        double lunch = rule.LunchBreakMinutes / 60.0;
                        double totalRaw = rawBefore12 + rawAfter12;
                        if (totalRaw > 0)
                        {
                            dayWorkHours = Math.Max(0, Math.Round(rawBefore12 - (lunch * (rawBefore12 / totalRaw)), 2));
                            nightWorkHours = Math.Max(0, Math.Round(rawAfter12 - (lunch * (rawAfter12 / totalRaw)), 2));
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
                            calculatedOt = Math.Round(rawOvertime, 2);

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
                                status = "Fazla Mesai";
                            }
                        }
                        else
                        {
                            status = "Eşik Altı (Mesai Sayılmadı)";
                        }
                    }
                    else if (lateMins > 0 || earlyLeaveMins > 0)
                    {
                        status = "Geç/Erken Eksik";
                    }
                    else
                    {
                        status = "Tam Vardiya";
                    }
                }
                else
                {
                    status = "Eksik (Tek Geçiş)";
                }

                results.Add(new ShiftAttendanceResult
                {
                    UserId = userId,
                    EmployeeName = emp.Name,
                    Department = emp.Department,
                    ShiftName = rule.Name,
                    DateString = day.ToString("yyyy-MM-dd"),
                    PlannedIn = plannedIn.ToString("HH:mm"),
                    PlannedOut = plannedOut.ToString("HH:mm"),
                    ActualFirstIn = firstIn.ToString("HH:mm:ss"),
                    ActualLastOut = punches.Count > 1 ? lastOut.ToString("HH:mm:ss") : "-",
                    LateMinutes = lateMins,
                    EarlyLeaveMinutes = earlyLeaveMins,
                    DayWorkHours = dayWorkHours,
                    NightWorkHours = nightWorkHours,
                    NetWorkHours = netHours,
                    CalculatedOvertime = calculatedOt,
                    ApprovedOvertime = approvedOt,
                    RejectedOvertime = rejectedOt,
                    Status = status
                });
            }

            return results;
        }
    }
}
