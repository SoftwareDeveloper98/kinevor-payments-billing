import { readFile, writeFile, rm, access } from 'node:fs/promises';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { dirname, relative, resolve } from 'node:path';

const here = dirname(fileURLToPath(import.meta.url));
const webRoot = resolve(here, '..');
const packageFile = resolve(webRoot, 'package.json');
const platformDir = resolve(
  webRoot,
  '..',
  '..',
  'kinevor-platform',
  'web',
  'dist',
  'kinevor-platform-web',
);

await access(resolve(platformDir, 'package.json')).catch(() => {
  console.error(
    'Local platform package not found. Build it first:\n'
      + '  cd ../../kinevor-platform/web\n'
      + '  npm install\n'
      + '  npm run build',
  );
  process.exit(1);
});

const original = await readFile(packageFile, 'utf8');
const manifest = JSON.parse(original);
const specifier = `file:${relative(webRoot, platformDir).replaceAll('\\', '/')}`;
manifest.dependencies['@softwaredeveloper98/kinevor-platform-web'] = specifier;

await writeFile(packageFile, `${JSON.stringify(manifest, null, 2)}\n`);

try {
  const npmCli = process.env.npm_execpath;
  const command = npmCli ? process.execPath : (process.platform === 'win32' ? 'npm.cmd' : 'npm');
  const args = npmCli
    ? [npmCli, 'install', '--no-audit', '--no-fund']
    : ['install', '--no-audit', '--no-fund'];
  const result = spawnSync(command, args, { cwd: webRoot, stdio: 'inherit' });
  if (result.error) {
    console.error(result.error.message);
    process.exitCode = 1;
  } else if (result.status !== 0) {
    process.exitCode = result.status ?? 1;
  }
} finally {
  await writeFile(packageFile, original);
  await rm(resolve(webRoot, 'package-lock.json'), { force: true });
}
