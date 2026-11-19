# -*- coding: utf-8 -*-
import io

with io.open(r'C:\Users\SaidNurs\Desktop\v5\zkteco\LocalizationService.cs', 'r', encoding='utf-8') as f:
    lines = f.readlines()

# Let's extract:
# tr_base: 0..148 (line 1 to 149)
# en_base: 149..285 (line 150 to 286)
# de_base: 286..422 (line 287 to 423)
# ru_base: 423..559 (line 424 to 560)
# ar_base: 560..696 (line 561 to 697)
# ku_base: 697..833 (line 698 to 834)

# And the 6 appended blocks:
# block_ku: 834..943 (starts with // --- PANELÊ MÎHENGIYÊN WELAT Û SAETÊ ---)
# block_ar: 944..1053 (starts with // --- لوحة إعدادات الدولة والوقت ---)
# block_ru: 1054..1163 (starts with // --- ПАНЕЛЬ НАСТРОЙКИ СТРАНЫ И ВРЕМЕНИ ---)
# block_de: 1164..1273 (starts with // --- LÄNDER- UND ZEITEINSTELLUNGEN ---)
# block_en: 1274..1383 (starts with // --- COUNTRY AND TIME CONFIG PANEL ---)
# block_tr: 1384..1493 (starts with // --- ÜLKE VE SAAT AYARLARI PANELİ ---)

print('Lines total:', len(lines))
print('Check 834:', repr(lines[834]))
print('Check 944:', repr(lines[944]))
print('Check 1054:', repr(lines[1054]))
print('Check 1164:', repr(lines[1164]))
print('Check 1274:', repr(lines[1274]))
print('Check 1384:', repr(lines[1384]))
print('Check 1494:', repr(lines[1494]))
