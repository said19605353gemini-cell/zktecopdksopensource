const fs = require('fs');
const path = require('path');

function organizeFolder(targetDir) {
    console.log('Organizing:', targetDir);
    const dllerDir = path.join(targetDir, 'dller');
    if (!fs.existsSync(dllerDir)) {
        fs.mkdirSync(dllerDir);
    }

    // Move all .dll files in targetDir into dllerDir
    const items = fs.readdirSync(targetDir);
    for (const item of items) {
        if (item.toLowerCase().endsWith('.dll')) {
            const src = path.join(targetDir, item);
            const dest = path.join(dllerDir, item);
            fs.copyFileSync(src, dest);
            fs.unlinkSync(src);
            console.log('  Moved to dller:', item);
        }
    }

    // Also copy SQLite native Interop DLLs into dller folder (both root of dller and x86/x64)
    const x86Dir = path.join(targetDir, 'x86');
    if (fs.existsSync(x86Dir)) {
        const x86Dest = path.join(dllerDir, 'x86');
        if (!fs.existsSync(x86Dest)) fs.mkdirSync(x86Dest);
        for (const file of fs.readdirSync(x86Dir)) {
            fs.copyFileSync(path.join(x86Dir, file), path.join(x86Dest, file));
            // also put directly in dller for standard 32-bit lookup
            fs.copyFileSync(path.join(x86Dir, file), path.join(dllerDir, file));
        }
        // Remove x86 directory from root if desired, or keep as fallback
        try { fs.rmSync(x86Dir, { recursive: true, force: true }); } catch {}
    }

    const x64Dir = path.join(targetDir, 'x64');
    if (fs.existsSync(x64Dir)) {
        const x64Dest = path.join(dllerDir, 'x64');
        if (!fs.existsSync(x64Dest)) fs.mkdirSync(x64Dest);
        for (const file of fs.readdirSync(x64Dir)) {
            fs.copyFileSync(path.join(x64Dir, file), path.join(x64Dest, file));
        }
        try { fs.rmSync(x64Dir, { recursive: true, force: true }); } catch {}
    }
}

organizeFolder('C:/Users/SaidNurs/Desktop/v5/zkteco/bin/x86/Release/net48');
organizeFolder('C:/Users/SaidNurs/Desktop/v5/zkteco/bin/Release/net48');

console.log('Successfully moved all DLLs into "dller" subfolder!');
