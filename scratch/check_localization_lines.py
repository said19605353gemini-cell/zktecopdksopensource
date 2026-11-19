import io

with io.open(r'C:\Users\SaidNurs\Desktop\v5\zkteco\LocalizationService.cs', 'r', encoding='utf-8') as f:
    lines = f.readlines()

for idx, line in enumerate(lines):
    if 'new Dictionary<string, string>' in line:
        print('Line %d: %s' % (idx + 1, line.strip()))
    if 'BtnDeviceSyncConfig' in line:
        print('Line %d has BtnDeviceSyncConfig' % (idx + 1))
