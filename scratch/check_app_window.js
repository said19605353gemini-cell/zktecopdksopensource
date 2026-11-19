const { spawn, execSync } = require('child_process');
const path = require('path');

const exe = 'C:\\Users\\SaidNurs\\Desktop\\v5\\zkteco\\dist\\ZKTeco_PDKS_Release_x86\\ZkPdksApp.exe';
const proc = spawn(exe, [], {
    cwd: path.dirname(exe),
    detached: true,
    stdio: 'ignore'
});
proc.unref();

setTimeout(() => {
    try {
        const psOut = execSync('powershell -NoProfile -Command "Get-Process ZkPdksApp -ErrorAction SilentlyContinue | Select-Object Id, ProcessName, MainWindowTitle, MainWindowHandle"').toString();
        console.log('PS Output:\n', psOut);
    } catch (e) {
        console.error(e.message);
    }
}, 3000);
