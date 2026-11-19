# -*- coding: utf-8 -*-
import io, zipfile

zip_path = r'C:\Users\SaidNurs\Desktop\18.11.2026yedekDesktop.zip'
with zipfile.ZipFile(zip_path, 'r') as z:
    for name in z.namelist():
        if 'LocalizationService.cs' in name:
            print('Found inside zip:', name)
            content = z.read(name)
            with io.open(r'C:\Users\SaidNurs\Desktop\v5\zkteco\scratch\recovered_loc.cs', 'wb') as out:
                out.write(content)
            print('Saved to recovered_loc.cs, size %d' % len(content))
            break
