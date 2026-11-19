const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');

const msbuild = 'C:\\Program Files\\Microsoft Visual Studio\\18\\Community\\MSBuild\\Current\\Bin\\MSBuild.exe';
const projectPath = 'C:\\Users\\SaidNurs\\Desktop\\v5\\zkteco\\ZkPdksApp.csproj';
const distBase = 'C:\\Users\\SaidNurs\\Desktop\\v5\\zkteco\\dist';

if (!fs.existsSync(distBase)) {
    fs.mkdirSync(distBase, { recursive: true });
}

const platforms = ['x86', 'x64', 'ARM64', 'AnyCPU'];

function cleanAndPackage(sourceDir, targetDir) {
    if (!fs.existsSync(targetDir)) {
        fs.mkdirSync(targetDir, { recursive: true });
    }
    const dllerDir = path.join(targetDir, 'dller');
    if (!fs.existsSync(dllerDir)) {
        fs.mkdirSync(dllerDir, { recursive: true });
    }

    const items = fs.readdirSync(sourceDir);
    for (const item of items) {
        const srcPath = path.join(sourceDir, item);
        const stat = fs.statSync(srcPath);

        if (stat.isDirectory()) {
            if (item === 'x86' || item === 'x64') {
                const destSub = path.join(dllerDir, item);
                if (!fs.existsSync(destSub)) fs.mkdirSync(destSub, { recursive: true });
                const subFiles = fs.readdirSync(srcPath);
                for (const sf of subFiles) {
                    fs.copyFileSync(path.join(srcPath, sf), path.join(destSub, sf));
                }
            } else if (item === 'dller') {
                // If already has dller, copy its content
                const subFiles = fs.readdirSync(srcPath);
                for (const sf of subFiles) {
                    const srcSf = path.join(srcPath, sf);
                    const destSf = path.join(dllerDir, sf);
                    if (fs.statSync(srcSf).isDirectory()) {
                        if (!fs.existsSync(destSf)) fs.mkdirSync(destSf, { recursive: true });
                        for (const ssf of fs.readdirSync(srcSf)) {
                            fs.copyFileSync(path.join(srcSf, ssf), path.join(destSf, ssf));
                        }
                    } else {
                        fs.copyFileSync(srcSf, destSf);
                    }
                }
            }
        } else {
            const ext = path.extname(item).toLowerCase();
            if (ext === '.dll') {
                fs.copyFileSync(srcPath, path.join(dllerDir, item));
            } else if (ext === '.exe' || ext === '.config' || ext === '.pdb' || ext === '.db') {
                // Only keep main exe, config
                if (item.startsWith('ZkPdksApp.')) {
                    fs.copyFileSync(srcPath, path.join(targetDir, item));
                }
            }
        }
    }
}

for (const platform of platforms) {
    console.log(`\n========================================`);
    console.log(`BUILDING: Configuration=Release, Platform=${platform}`);
    console.log(`========================================`);
    try {
        const cmd = `"${msbuild}" "${projectPath}" /p:Configuration=Release /p:Platform=${platform} /t:Rebuild /verbosity:minimal`;
        execSync(cmd, { stdio: 'inherit' });

        // Identify build output dir
        // Usually bin/Release/net48 or bin/<platform>/Release/net48 or bin/Release/<platform>/net48
        let candidateDirs = [
            `C:\\Users\\SaidNurs\\Desktop\\v5\\zkteco\\bin\\${platform}\\Release\\net48`,
            `C:\\Users\\SaidNurs\\Desktop\\v5\\zkteco\\bin\\Release\\${platform}\\net48`,
            `C:\\Users\\SaidNurs\\Desktop\\v5\\zkteco\\bin\\Release\\net48`
        ];

        let foundDir = candidateDirs.find(d => fs.existsSync(path.join(d, 'ZkPdksApp.exe')));
        if (!foundDir) {
            console.error(`Could not locate output for ${platform}!`);
            continue;
        }

        console.log(`Found output in: ${foundDir}`);
        const targetDist = path.join(distBase, `ZKTeco_PDKS_Release_${platform}`);
        cleanAndPackage(foundDir, targetDist);

        console.log(`Platform ${platform} packaged to: ${targetDist}`);
        console.log(`Root contents: `, fs.readdirSync(targetDist));
        console.log(`dller contents count: `, fs.readdirSync(path.join(targetDist, 'dller')).length);
    } catch (err) {
        console.error(`Build failed for ${platform}:`, err.message);
    }
}
