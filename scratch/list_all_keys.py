# -*- coding: utf-8 -*-
import io, re

# Let's inspect all keys in the current LocalizationService.cs
with io.open(r'C:\Users\SaidNurs\Desktop\v5\zkteco\LocalizationService.cs', 'r', encoding='utf-8') as f:
    cur_text = f.read()

m = re.findall(r'\["([^"]+)"\]\s*=\s*"([^"]*)"', cur_text)
unique_keys = []
for k, v in m:
    if k not in unique_keys:
        unique_keys.append(k)
print('Unique keys in current LocalizationService:', len(unique_keys))
for k in unique_keys:
    print(k)
