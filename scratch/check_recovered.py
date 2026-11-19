# -*- coding: utf-8 -*-
import io, re

with io.open(r'C:\Users\SaidNurs\Desktop\v5\zkteco\scratch\recovered_loc.cs', 'r', encoding='utf-8') as f:
    text = f.read()

# Let's see what keys are in recovered_loc.cs
langs = ['tr', 'en', 'de', 'ru', 'ar', 'ku']
for lang in langs:
    pattern = r'\["' + lang + r'"\]\s*=\s*new\s+Dictionary<string,\s*string>\s*\{(.*?)\}\s*,\s*\[|\}\s*;\s*public'
    m = re.search(pattern, text, re.DOTALL)
    if m:
        content = m.group(1) if m.group(1) is not None else ''
        keys = re.findall(r'\["([^"]+)"\]\s*=', content)
        print('Recovered lang %s has %d keys' % (lang, len(keys)))
    else:
        print('Recovered lang %s not matched' % lang)
