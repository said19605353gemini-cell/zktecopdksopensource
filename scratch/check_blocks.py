import io

with io.open(r'C:\Users\SaidNurs\Desktop\v5\zkteco\LocalizationService.cs', 'r', encoding='utf-8') as f:
    lines = f.readlines()

for idx, line in enumerate(lines):
    if line.strip().startswith('["BtnDeviceSyncConfig"]'):
        val = line.split('=')[1].strip() if '=' in line else ''
        print('Line %d: val len %d' % (idx + 1, len(val)))
        for p in range(max(0, idx-3), idx):
            print('   prev line %d: %s' % (p+1, repr(lines[p].strip())))
