# -*- coding: utf-8 -*-
import io, re

# Read lines from current and from recovered
with io.open(r'C:\Users\SaidNurs\Desktop\v5\zkteco\scratch\recovered_loc.cs', 'r', encoding='utf-8') as f:
    rec_text = f.read()

# Let's extract dictionaries from recovered_loc.cs
# ["tr"], ["en"], ["de"], ["ru"], ["ar"], ["ku"]
rec_langs = ['tr', 'en', 'de', 'ru', 'ar', 'ku']
rec_dicts = {}
for lang in rec_langs:
    m = re.search(r'\["' + lang + r'"\]\s*=\s*new\s+Dictionary<string,\s*string>\s*\{(.*?)\}\s*,\s*\[|\}\s*;\s*public', rec_text, re.DOTALL)
    if m and m.group(1):
        entries = dict(re.findall(r'\["([^"]+)"\]\s*=\s*"([^"]*)"', m.group(1)))
        rec_dicts[lang] = entries
        print('Recovered lang %s has %d entries' % (lang, len(entries)))
    else:
        rec_dicts[lang] = {}
        print('Recovered lang %s has 0 entries' % lang)
