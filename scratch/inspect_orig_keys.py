# -*- coding: utf-8 -*-
import io, re

# Let's inspect all keys in recovered_loc.cs and what was added
with io.open(r'C:\Users\SaidNurs\Desktop\v5\zkteco\scratch\recovered_loc.cs', 'r', encoding='utf-8') as f:
    orig_text = f.read()

# Let's see all keys in orig_text
m = re.findall(r'\["([^"]+)"\]\s*=\s*"([^"]*)"', orig_text)
print('Total key-values in recovered:', len(m))
unique_keys = []
for k, v in m:
    if k not in unique_keys:
        unique_keys.append(k)
print('Unique keys in recovered:', len(unique_keys))
print(unique_keys)
