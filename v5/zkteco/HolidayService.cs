using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace ZkPdksApp
{
    public class HolidayItem
    {
        public string Date { get; set; } // yyyy-MM-dd
        public string LocalName { get; set; }
        public string Name { get; set; }
    }

    public static class HolidayService
    {
        // Yıl bazında tatil önbelleği
        private static readonly Dictionary<int, Dictionary<string, string>> CachedHolidays 
            = new Dictionary<int, Dictionary<string, string>>();

        /// <summary>
        /// Belirtilen yıla ait resmi tatilleri önce yerel veritabanı/önbellekten, 
        /// yoksa internetten (Nager.Date API) çeker. İnternet yoksa sabit Türkiye tatillerini kullanır.
        /// Dönen sözlükte: Key = "yyyy-MM-dd", Value = Tatilin Adı (Örn: "Ramazan Bayramı 1. Gün")
        /// </summary>
        public static Dictionary<string, string> GetHolidaysForYear(int year, DatabaseService db = null, bool forceRefresh = false)
        {
            if (!forceRefresh && CachedHolidays.ContainsKey(year) && CachedHolidays[year].Count > 0)
            {
                return CachedHolidays[year];
            }

            // 1. Veritabanında kayıtlı mı kontrol et
            if (!forceRefresh && db != null)
            {
                var dbHolidays = db.GetHolidaysByYear(year);
                if (dbHolidays != null && dbHolidays.Count > 0)
                {
                    CachedHolidays[year] = dbHolidays;
                    return dbHolidays;
                }
            }

            // 2. İnternetten çekmeyi dene (Nager.Date Public Holidays API - TR)
            var holidays = FetchFromInternet(year);

            // 3. Eğer internetten çekilemediyse bilinen sabit Türkiye tatillerini ekle
            if (holidays == null || holidays.Count == 0)
            {
                holidays = GetFallbackHolidays(year);
            }

            // 4. Veritabanına kaydet
            if (db != null && holidays.Count > 0)
            {
                db.SaveHolidays(year, holidays);
            }

            CachedHolidays[year] = holidays;
            return holidays;
        }

        private static Dictionary<string, string> FetchFromInternet(int year)
        {
            var result = new Dictionary<string, string>();
            try
            {
                string url = $"https://date.nager.at/api/v3/PublicHolidays/{year}/TR";
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.UserAgent = "ZkPdksApp-HolidayClient/1.0";
                request.Timeout = 6000; // 6 saniye zaman aşımı

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream(), System.Text.Encoding.UTF8))
                {
                    string json = reader.ReadToEnd();
                    // Nager.Date formatı: "date":"2026-04-23" ... "localName":"Ulusal Egemenlik ve Çocuk Bayramı"
                    var matches = Regex.Matches(json, "\"date\"\\s*:\\s*\"(?<date>[^\"]+)\"[^}]*?\"localName\"\\s*:\\s*\"(?<name>[^\"]+)\"");
                    foreach (Match m in matches)
                    {
                        string d = m.Groups["date"].Value;
                        string rawName = m.Groups["name"].Value;
                        string name = Regex.Unescape(rawName);
                        if (!string.IsNullOrEmpty(d) && !result.ContainsKey(d))
                        {
                            result[d] = name;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"İnternetten tatil verisi çekilirken hata (Yıl {year}): {ex.Message}");
            }
            return result;
        }

        /// <summary>
        /// İnternet bağlantısı olmasa bile Türkiye'deki sabit resmi tatiller ve 
        /// 2024-2028 yılları için bilinen dini bayram takvimi
        /// </summary>
        public static Dictionary<string, string> GetFallbackHolidays(int year)
        {
            var map = new Dictionary<string, string>
            {
                [$"{year}-01-01"] = "Yılbaşı",
                [$"{year}-04-23"] = "Ulusal Egemenlik ve Çocuk Bayramı",
                [$"{year}-05-01"] = "Emek ve Dayanışma Günü",
                [$"{year}-05-19"] = "Atatürk'ü Anma, Gençlik ve Spor Bayramı",
                [$"{year}-07-15"] = "Demokrasi ve Millî Birlik Günü",
                [$"{year}-08-30"] = "Zafer Bayramı",
                [$"{year}-10-29"] = "Cumhuriyet Bayramı"
            };

            // Dini Bayramlar (Hicri Takvim Kayması Nedeniyle Yıllara Göre)
            if (year == 2024)
            {
                map["2024-04-10"] = "Ramazan Bayramı 1. Gün";
                map["2024-04-11"] = "Ramazan Bayramı 2. Gün";
                map["2024-04-12"] = "Ramazan Bayramı 3. Gün";
                map["2024-06-16"] = "Kurban Bayramı 1. Gün";
                map["2024-06-17"] = "Kurban Bayramı 2. Gün";
                map["2024-06-18"] = "Kurban Bayramı 3. Gün";
                map["2024-06-19"] = "Kurban Bayramı 4. Gün";
            }
            else if (year == 2025)
            {
                map["2025-03-30"] = "Ramazan Bayramı 1. Gün";
                map["2025-03-31"] = "Ramazan Bayramı 2. Gün";
                map["2025-04-01"] = "Ramazan Bayramı 3. Gün";
                map["2025-06-06"] = "Kurban Bayramı 1. Gün";
                map["2025-06-07"] = "Kurban Bayramı 2. Gün";
                map["2025-06-08"] = "Kurban Bayramı 3. Gün";
                map["2025-06-09"] = "Kurban Bayramı 4. Gün";
            }
            else if (year == 2026)
            {
                map["2026-03-20"] = "Ramazan Bayramı 1. Gün";
                map["2026-03-21"] = "Ramazan Bayramı 2. Gün";
                map["2026-03-22"] = "Ramazan Bayramı 3. Gün";
                map["2026-05-27"] = "Kurban Bayramı 1. Gün";
                map["2026-05-28"] = "Kurban Bayramı 2. Gün";
                map["2026-05-29"] = "Kurban Bayramı 3. Gün";
                map["2026-05-30"] = "Kurban Bayramı 4. Gün";
            }
            else if (year == 2027)
            {
                map["2027-03-10"] = "Ramazan Bayramı 1. Gün";
                map["2027-03-11"] = "Ramazan Bayramı 2. Gün";
                map["2027-03-12"] = "Ramazan Bayramı 3. Gün";
                map["2027-05-17"] = "Kurban Bayramı 1. Gün";
                map["2027-05-18"] = "Kurban Bayramı 2. Gün";
                map["2027-05-19"] = "Kurban Bayramı 3. Gün";
                map["2027-05-20"] = "Kurban Bayramı 4. Gün";
            }

            return map;
        }

        /// <summary>
        /// Türkiye'ye göre (UTC+3) güncel canlı internet tarih ve saatini çeker.
        /// İnternete erişilemezse veya veri bozulursa null döner (böylece çağıran kod uyarı verip yerel PC saatine geçebilir).
        /// </summary>
        public static (DateTime Time, bool IsFromInternet, string ErrorMessage) GetCurrentTurkeyDateTimeSafe()
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                string[] checkUrls = new[]
                {
                    "https://date.nager.at",
                    "https://www.cloudflare.com",
                    "https://www.microsoft.com"
                };

                foreach (var url in checkUrls)
                {
                    try
                    {
                        var req = (HttpWebRequest)WebRequest.Create(url);
                        req.Method = "HEAD";
                        req.Timeout = 4000;
                        req.UserAgent = "ZkPdksApp/1.0";
                        using (var resp = (HttpWebResponse)req.GetResponse())
                        {
                            string dateHeader = resp.Headers["Date"];
                            if (!string.IsNullOrEmpty(dateHeader) && DateTime.TryParse(dateHeader, out DateTime parsedUtc))
                            {
                                // Yıl kontrolü (bozuk veri koruması: yıl 2020 ile 2040 arasında olmalı)
                                if (parsedUtc.Year < 2020 || parsedUtc.Year > 2040)
                                {
                                    continue; // Geçersiz veya bozuk yanıt
                                }

                                DateTime utc = parsedUtc.ToUniversalTime();
                                try
                                {
                                    var trZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
                                    return (TimeZoneInfo.ConvertTimeFromUtc(utc, trZone), true, null);
                                }
                                catch
                                {
                                    return (utc.AddHours(3), true, null);
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"GetCurrentTurkeyDateTimeSafe internet hatası: {ex.Message}");
            }

            // İnternet yoksa veya yanıt bozulduysa yerel bilgisayar saatine dön
            return (DateTime.Now, false, "İnternet bağlantısı kurulamadı veya zaman sunucusundan geçerli yanıt alınamadı.");
        }

        public static DateTime GetCurrentTurkeyDateTime()
        {
            return GetCurrentTurkeyDateTimeSafe().Time;
        }
    }
}
