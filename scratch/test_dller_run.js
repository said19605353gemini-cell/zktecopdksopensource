const cp = require('child_process');
const exe = 'C:/Users/SaidNurs/Desktop/v5/zkteco/bin/Release/net48/ZkPdksApp.exe';
const child = cp.spawn(exe, [], { detached: true, stdio: 'ignore' });
child.unref();

setTimeout(() => {
    try {
        const out = cp.execSync('tasklist /FI "IMAGENAME eq ZkPdksApp.exe" /FO CSV').toString();
        console.log('Running process result:');
        console.log(out);
        cp.execSync('taskkill /F /IM ZkPdksApp.exe');
        console.log('App successfully verified running from dller folder!');
    } catch (e) {
        console.error('Error:', e.message);
    }
}, 3000);
