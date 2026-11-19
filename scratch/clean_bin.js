const fs = require('fs');
const path = require('path');

const binDir = 'C:/Users/SaidNurs/Desktop/v5/zkteco/bin/Release/net48';
const dllerDir = path.join(binDir, 'dller');
if (!fs.existsSync(dllerDir)) {
    fs.mkdirSync(dllerDir, { recursive: true });
}

// Move dlls
const files = fs.readdirSync(binDir);
for (const f of files) {
    if (f.toLowerCase().endsWith('.dll')) {
        const src = path.join(binDir, f);
        const dest = path.join(dllerDir, f);
        fs.copyFileSync(src, dest);
        fs.unlinkSync(src);
    }
}

// Move x86 / x64 directories if at root
['x86', 'x64'].forEach(sub => {
    const srcSub = path.join(binDir, sub);
    const destSub = path.join(dllerDir, sub);
    if (fs.existsSync(srcSub)) {
        if (!fs.existsSync(destSub)) fs.mkdirSync(destSub, { recursive: true });
        const subFiles = fs.readdirSync(srcSub);
        for (const sf of subFiles) {
            fs.copyFileSync(path.join(srcSub, sf), path.join(destSub, sf));
            fs.unlinkSync(path.join(srcSub, sf));
        }
        try { fs.rmdirSync(srcSub); } catch (e) {}
    }
});

// Remove leftover runner files if any
['Runner.cs', 'Runner.exe'].forEach(rf => {
    const p = path.join(binDir, rf);
    if (fs.existsSync(p)) fs.unlinkSync(p);
});

console.log('Cleaned root. Current contents of binDir:');
console.log(fs.readdirSync(binDir));
