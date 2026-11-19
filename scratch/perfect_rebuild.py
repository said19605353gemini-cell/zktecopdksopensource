# -*- coding: utf-8 -*-
import io, re

with io.open(r'C:\Users\SaidNurs\Desktop\v5\zkteco\LocalizationService.cs', 'r', encoding='utf-8') as f:
    text = f.read()

# Match all key-values inside dictionary
# Format: ["key"] = "value", or ["key"] = "value"
# We know the languages order: tr, en, de, ru, ar, ku
langs = ['tr', 'en', 'de', 'ru', 'ar', 'ku']

# Let's split text by ["<lang>"] = new Dictionary
parts = re.split(r'\["[a-z]{2}"\]\s*=\s*new\s+Dictionary<string,\s*string>\s*\{?', text)

print('Split parts count:', len(parts))

out = [u'''using System.Collections.Generic;

namespace ZkPdksApp
{
    public static class LocalizationService
    {
        public static string CurrentLanguage { get; set; } = "tr";

        private static readonly Dictionary<string, Dictionary<string, string>> Translations = new Dictionary<string, Dictionary<string, string>>
        {
''']

# parts[1] is tr, parts[2] is en, etc.
for idx, lang in enumerate(langs):
    part = parts[idx + 1] if idx + 1 < len(parts) else u''
    # extract each line with ["
    dict_lines = []
    for line in part.splitlines():
        s = line.strip()
        if s.startswith('["') and '=' in s:
            if not s.endswith(','):
                s += ','
            dict_lines.append(u'                ' + s)
        elif s.startswith('//'):
            dict_lines.append(u'                ' + s)
    
    out.append(u'            ["%s"] = new Dictionary<string, string>\n' % lang)
    out.append(u'            {\n')
    out.append(u'\n'.join(dict_lines) + u'\n')
    if idx == len(langs) - 1:
        out.append(u'            }\n')
    else:
        out.append(u'            },\n')

out.append(u'''        };

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
''')

with io.open(r'C:\Users\SaidNurs\Desktop\v5\zkteco\LocalizationService.cs', 'w', encoding='utf-8') as f:
    f.write(u''.join(out))

print('REBUILT LocalizationService.cs CLEANLY!')
