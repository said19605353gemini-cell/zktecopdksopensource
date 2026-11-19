# -*- coding: utf-8 -*-
import io, re

with io.open(r'C:\Users\SaidNurs\Desktop\v5\zkteco\scratch\backup_1127.cs', 'r', encoding='utf-8') as f:
    text = f.read()

# Let's inspect what dictionaries or blocks are inside backup_1127.cs
matches = re.findall(r'\["([a-z]{2})"\]\s*=\s*new\s+Dictionary', text)
print('backup_1127 dicts:', matches)
# Let's find all occurrences of HelpTitle_ in backup_1127.cs
print('HelpTitle_ count in backup_1127:', text.count('HelpTitle_'))
