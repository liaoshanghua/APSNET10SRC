/**
 * 从 EasyManufacture.Web/Web.config 导出 ConnectionStrings-Profiles.json
 * 用法：node scripts/Extract-ConnectionProfiles.js
 */
const fs = require('fs');
const path = require('path');

const root = path.resolve(__dirname, '..');
const webConfig = path.resolve(root, '../EasyManufacture.Web/Web.config');
const outFile = path.resolve(root, 'docs/ConnectionStrings-Profiles.json');

const lines = fs.readFileSync(webConfig, 'utf8').split(/\r?\n/);
const decode = (s) => (s || '').replace(/&quot;/g, '"').trim();

const profiles = [];
let currentName = '';
let buffer = '';
let inComment = false;
let inConn = false;

function flushBuffer() {
  if (!buffer) return;
  const sql = buffer.match(/name="MSSQLConnectionString"[\s\S]*?connectionString="([^"]*)"/);
  if (!sql) { buffer = ''; return; }
  const scm = buffer.match(/name="MSSQLConnectionStringSCM"[\s\S]*?connectionString="([^"]*)"/);
  const mssql = decode(sql[1]);
  if (!mssql) { buffer = ''; return; }
  const name = currentName || `未命名_${profiles.length + 1}`;
  if (!profiles.some((p) => p.mssql === mssql)) {
    profiles.push({ name, enabled: false, mssql, scm: scm ? decode(scm[1]) : '' });
  }
  buffer = '';
  currentName = '';
}

for (const line of lines) {
  const t = line.trim();
  if (t === '<connectionStrings>') { inConn = true; continue; }
  if (t === '</connectionStrings>') { flushBuffer(); break; }
  if (!inConn) continue;

  const singleComment = t.match(/^<!--([^<-][^>]*)-->$/);
  if (singleComment) {
    flushBuffer();
    currentName = singleComment[1].trim();
    continue;
  }

  if (t.startsWith('<!--') && !t.endsWith('-->')) {
    inComment = true;
    const inline = t.replace(/^<!--/, '').trim();
    if (inline && !inline.startsWith('<')) currentName = inline;
    buffer = t + '\n';
    continue;
  }
  if (inComment) {
    buffer += line + '\n';
    if (!currentName) {
      const plain = t.replace(/^<!--?/, '').trim();
      if (plain && !plain.startsWith('<') && !plain.includes('connectionString')) currentName = plain;
    }
    if (t.includes('-->')) {
      inComment = false;
      flushBuffer();
    }
    continue;
  }

  if (t.includes('MSSQLConnectionString') || t.includes('EasyManufactureEntities')) {
    buffer += line + '\n';
    if (t.includes('/>') || t.includes('</add>')) flushBuffer();
  }
}

profiles.forEach((p) => { if (p.name === '盈瑞丰') p._defaultActive = true; });

fs.writeFileSync(outFile, JSON.stringify({
  _readme: '从 EasyManufacture.Web/Web.config 迁移。切换：复制 mssql 到 appsettings.json → ConnectionStrings.MSSQLConnectionString',
  _source: 'EasyManufacture.Web/Web.config',
  profiles,
}, null, 2), 'utf8');

console.log(`Wrote ${profiles.length} profiles to ${outFile}`);
