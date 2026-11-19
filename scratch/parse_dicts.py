# -*- coding: utf-8 -*-
import io

with io.open(r'C:\Users\SaidNurs\Desktop\v5\zkteco\LocalizationService.cs', 'r', encoding='utf-8') as f:
    text = f.read()

# Let's cleanly separate the 6 dictionaries.
# Notice each dictionary is of format:
# ["tr"] = new Dictionary<string, string> { ... },
# Let's inspect where the base definitions originally were.

# Let's write a python parser to extract key-values from each section!
import re

# We can find all keys and values in each language block
# Let's inspect the file lines
lines = text.splitlines(True)

# Find all dictionary assignments
# ["<lang>"] = new Dictionary<string, string>
dict_indices = []
for i, l in enumerate(lines):
    m = re.search(r'\["([a-z]{2})"\]\s*=\s*new\s+Dictionary', l)
    if m:
        dict_indices.append((i, m.group(1)))

print('Found dicts:', dict_indices)
