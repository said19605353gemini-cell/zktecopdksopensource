const fs = require('fs');
const path = require('path');
const execSync = require('child_process').execSync;

const testDir = 'C:/Users/SaidNurs/Desktop/v5/zkteco/test_standalone';
if (fs.existsSync(testDir)) fs.rmSync(testDir, { recursive: true, force: true });
fs.mkdirSync(testDir);

fs.copyFileSync('C:/Users/SaidNurs/Desktop/v5/zkteco/bin/Release/net48/ZkPdksApp.exe', path.join(testDir, 'ZkPdksApp.exe'));
fs.copyFileSync('C:/Users/SaidNurs/Desktop/v5/zkteco/bin/Release/net48/ZkPdksApp.exe.config', path.join(testDir, 'ZkPdksApp.exe.config'));

const child = require('child_process').spawn(path.join(testDir, 'ZkPdksApp.exe'), [], { detached: true, stdio: 'ignore' });
child.unref();

setTimeout(() => {
    try {
        const out = execSync('powershell -Command "Get-Process ZkPdksApp -ErrorAction SilentlyContinue | Select-Object Id, ProcessName"').toString();
        console.log('Standalone check result:');
        console.log(out);
        execSync('powershell -Command "Stop-Process -Name ZkPdksApp -Force -ErrorAction SilentlyContinue"');
    } catch (e) {
        console.log('Error:', e.message);
    }
    try {
        fs.rmSync(testDir, { recursive: true, force: true });
    } catch {}
}, 2500);
