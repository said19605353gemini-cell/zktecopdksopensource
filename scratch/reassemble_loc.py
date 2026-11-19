# -*- coding: utf-8 -*-
import io

with io.open(r'C:\Users\SaidNurs\Desktop\v5\zkteco\LocalizationService.cs', 'r', encoding='utf-8') as f:
    lines = f.readlines()

# Head (lines 0..10): class definition & header up to ["tr"] = new Dictionary...
head = lines[0:11] # line 1..11

# tr_base: 11..148 (line 12..149)
tr_base = lines[11:149]

# en_base: 150..285 (line 151..286)
en_base = lines[150:286]

# de_base: 287..422 (line 288..423)
de_base = lines[287:423]

# ru_base: 424..559 (line 425..560)
ru_base = lines[424:560]

# ar_base: 561..696 (line 562..697)
ar_base = lines[561:697]

# ku_base: 698..833 (line 699..834)
ku_base = lines[698:834]

# The 6 blocks:
block_ku = lines[834:944]
block_ar = lines[944:1054]
block_ru = lines[1054:1164]
block_de = lines[1164:1274]
block_en = lines[1274:1384]
block_tr = lines[1384:1494]

tail = lines[1494:] # line 1495..end ( }; and public static string Get... )

def build_dict(name, base_lines, block_lines):
    res = []
    res.append(u'            ["%s"] = new Dictionary<string, string>\n' % name)
    res.append(u'            {\n')
    for l in base_lines:
        res.append(l)
    for l in block_lines:
        res.append(l)
    # Ensure last line before closing has no trailing comma issues if it's the end of dictionary
    res.append(u'            },\n')
    return res

new_lines = []
new_lines.extend(head)
# tr
new_lines.extend(build_dict('tr', tr_base, block_tr))
# en
new_lines.extend(build_dict('en', en_base, block_en))
# de
new_lines.extend(build_dict('de', de_base, block_de))
# ru
new_lines.extend(build_dict('ru', ru_base, block_ru))
# ar
new_lines.extend(build_dict('ar', ar_base, block_ar))
# ku
new_lines.extend(build_dict('ku', ku_base, block_ku))

# Remove trailing comma on last dictionary before closing Translations
if new_lines[-1] == u'            },\n':
    new_lines[-1] = u'            }\n'

new_lines.extend(tail)

out_text = u''.join(new_lines)
with io.open(r'C:\Users\SaidNurs\Desktop\v5\zkteco\LocalizationService.cs', 'w', encoding='utf-8') as f:
    f.write(out_text)

print('SUCCESSFULLY REASSEMBLED LocalizationService.cs')
