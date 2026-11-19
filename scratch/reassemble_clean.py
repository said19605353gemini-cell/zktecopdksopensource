# -*- coding: utf-8 -*-
import io

with io.open(r'C:\Users\SaidNurs\Desktop\v5\zkteco\LocalizationService.cs', 'r', encoding='utf-8') as f:
    lines = f.readlines()

head = lines[0:10] # up to line 10 (translations opening brace)

tr_base = lines[14:152]
block_tr = lines[153:263]

en_base = lines[265:401]
block_en = lines[402:512]

de_base = lines[514:650]
block_de = lines[651:761]

ru_base = lines[763:899]
block_ru = lines[900:1010]

ar_base = lines[1012:1148]
block_ar = lines[1149:1259]

ku_base = lines[1261:1397]
block_ku = lines[1398:1508]

tail = lines[1509:] # }; and Get method

def build_dict(name, base_lines, block_lines, is_last=False):
    res = []
    res.append(u'            ["%s"] = new Dictionary<string, string>\n' % name)
    res.append(u'            {\n')
    for l in base_lines:
        res.append(l)
    for l in block_lines:
        res.append(l)
    if is_last:
        res.append(u'            }\n')
    else:
        res.append(u'            },\n')
    return res

new_lines = []
new_lines.extend(head)
new_lines.extend(build_dict('tr', tr_base, block_tr))
new_lines.extend(build_dict('en', en_base, block_en))
new_lines.extend(build_dict('de', de_base, block_de))
new_lines.extend(build_dict('ru', ru_base, block_ru))
new_lines.extend(build_dict('ar', ar_base, block_ar))
new_lines.extend(build_dict('ku', ku_base, block_ku, is_last=True))
new_lines.extend(tail)

out_text = u''.join(new_lines)
with io.open(r'C:\Users\SaidNurs\Desktop\v5\zkteco\LocalizationService.cs', 'w', encoding='utf-8') as f:
    f.write(out_text)

print('CLEAN REASSEMBLED LocalizationService.cs')
