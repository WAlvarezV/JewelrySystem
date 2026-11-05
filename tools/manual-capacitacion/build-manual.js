// Genera la versión de capacitación (.docx) de un manual de usuario a partir de su ManualUsuario.md.
// Uso: node build-manual.js [modulo]   (por defecto: pqrs → config/pqrs.js). Ver README.md.
const fs = require('fs');
const path = require('path');
const {
  Document, Packer, Paragraph, TextRun, ImageRun, Table, TableRow, TableCell, WidthType, BorderStyle,
  ShadingType, AlignmentType, VerticalAlign, HeightRule, Header, Footer, PageNumber, TableOfContents,
  Bookmark, PageReference, LevelFormat, PositionalTab, PositionalTabAlignment, PositionalTabRelativeTo,
  PositionalTabLeader, TableLayoutType,
} = require('docx');

const MODULE = (process.argv[2] || 'inventario').toLowerCase();
const CFG = require(`./config/${MODULE}.js`);
const ROOT = path.resolve(__dirname, '../..');
const MANUAL = path.join(ROOT, CFG.manualPath);
const REPO = path.dirname(MANUAL);
const LOGO = path.join(__dirname, 'assets/logo-encabezado.png');
const OUT = path.join(__dirname, 'out', `${CFG.outputName}.docx`);

// ---------- estilo ----------
const BRAND = '5B1426', BRAND_SOFT = 'FAF0F2', INK = '1D232B', MUTED = '5B6470', RULE = 'C9D3DD';
const FONT = 'Arial';
const CONTENT_W = 12240 - 1701 - 1418; // 9121 dxa
const PX_W = 600, PX_H_MAX = 400, PX_H_VISTA = 600; // tamaño máximo de figura (px a 96 ppp)

const CALLOUT = {
  nota: { fill: 'EAF3FA', bar: BRAND, label: BRAND },
  importante: { fill: 'FFF4E0', bar: 'C77700', label: '8A5300' },
  seguridad: { fill: 'EEF0F3', bar: '4A5563', label: '323A46' },
};

// ---------- utilidades ----------
const stripEmoji = (s) => s
  .replace(/[\p{Extended_Pictographic}\u{1F3FB}-\u{1F3FF}\uFE0F\u200D]/gu, '')
  .replace(/^[ \t]+|[ \t]+$/g, '')
  .replace(/ {2,}/g, ' ');
const bmName = (id) => 'bm_' + id.replace(/[^A-Za-z0-9]/g, '_');

function pngSize(file) {
  const b = fs.readFileSync(file);
  return { w: b.readUInt32BE(16), h: b.readUInt32BE(20), data: b };
}
function fit(w, h, maxH = PX_H_MAX) {
  const s = Math.min(1, PX_W / w, maxH / h);
  return { width: Math.round(w * s), height: Math.round(h * s) };
}

// ---------- pase 1: parsear el markdown a bloques ----------
const md = fs.readFileSync(MANUAL, 'utf8').replace(/\r/g, '');
const lines = md.split('\n');

const chapters = [];
const ids = {}; // id -> { num, bm, chapter }
let ch = null, pendingIds = [], mode = null, para = [], h2Count = 0, lastNumbered = null;

function registerIds(num, extra) {
  const all = [...pendingIds, ...(extra || [])];
  pendingIds = [];
  if (!all.length) return null;
  const bm = bmName(all[0]);
  for (const id of all) ids[id] = { num, bm, chapter: ch.num };
  return bm;
}
function flushPara() {
  if (!para.length) return;
  const text = para.join('\n');
  para = [];
  if (mode === 'faq') {
    const m = /^\*\*(.+?)\*\*\s*\n([\s\S]*)$/.exec(text);
    if (m) { ch.blocks.push({ t: 'faq', q: stripEmoji(m[1]), a: [{ t: 'p', text: m[2] }] }); return; }
    const last = ch.blocks[ch.blocks.length - 1];
    if (last && last.t === 'faq') { last.a.push({ t: 'p', text }); return; }
  }
  ch.blocks.push({ t: 'p', text });
}

for (let i = 0; i < lines.length; i++) {
  const raw = lines[i];
  const line = raw.trim();

  let m;
  if ((m = /^<details id="([^"]+)"[^>]*>/.exec(line))) {
    flushPara();
    ch = { num: chapters.length + 1, blocks: [], title: '', id: m[1] };
    chapters.push(ch); h2Count = 0; mode = null; pendingIds = [m[1]];
    continue;
  }
  if (!ch) continue;
  if ((m = /<summary><h2[^>]*>(.*)<\/h2><\/summary>/.exec(line))) {
    ch.title = stripEmoji(m[1]).replace(/^Proceso \d+:\s*/, '');
    ch.bm = registerIds(String(ch.num));
    lastNumbered = String(ch.num);
    continue;
  }
  if (line.startsWith('<!--') || line === '<br/>' || line === '---' || line === '</details>') { flushPara(); continue; }
  if ((m = /^<div id="([^"]+)"><\/div>$/.exec(line))) { flushPara(); pendingIds.push(m[1]); continue; }
  if (line === '') { flushPara(); continue; }

  if ((m = /^### (.*)$/.exec(line))) {
    flushPara();
    const t = stripEmoji(m[1]);
    if (/^Objetivo del Proceso/i.test(t)) { ch.blocks.push({ t: 'label', text: 'Objetivo del proceso' }); mode = null; continue; }
    let title = t, n;
    if ((n = /^Paso (\d+):\s*(.*)$/.exec(t))) { h2Count = +n[1]; title = n[2]; }
    else { h2Count += 1; }
    if (/Problemas Frecuentes/i.test(t)) { mode = 'faq'; title = 'Problemas frecuentes'; } else mode = null;
    const num = `${ch.num}.${h2Count}`;
    lastNumbered = num;
    ch.blocks.push({ t: 'h2', num, title, bm: registerIds(num) });
    continue;
  }
  if ((m = /^#### (.*)$/.exec(line))) {
    flushPara();
    const t = stripEmoji(m[1]);
    if (/^¿Qué necesita hacer\?/.test(t)) { mode = 'tasks'; ch.blocks.push({ t: 'tasks', items: [] }); continue; }
    mode = null;
    let n = /^(\d+(?:\.\d+)*)\.?\s+(.*)$/.exec(t);
    if (n) {
      const num = `${ch.num}.${n[1]}`;
      lastNumbered = num;
      ch.blocks.push({ t: 'h3', num, title: n[2], bm: registerIds(num) });
    } else {
      ch.blocks.push({ t: 'h3', num: null, title: t, bm: registerIds(lastNumbered) });
    }
    continue;
  }
  if (line.startsWith('>')) {
    flushPara();
    const group = [];
    let j = i;
    while (j < lines.length && lines[j].trim().startsWith('>')) { group.push(lines[j].trim().replace(/^>\s?/, '')); j++; }
    i = j - 1;
    ch.blocks.push({ t: 'quote', lines: group });
    continue;
  }
  if ((m = /^!\[([^\]]*)\]\(([^)]+)\)/.exec(line))) {
    flushPara();
    let cap = m[1];
    const nx = (lines[i + 1] || '').trim();
    const cm = /^\*(.*)\*$/.exec(nx);
    if (cm) { cap = stripEmoji(cm[1]); i++; }
    // Leyenda de las marcas numeradas de la captura: "*🔢 1 Texto · 2 Texto*" en la línea siguiente.
    const lm = /^\*🔢\s*(.*)\*$/.exec((lines[i + 1] || '').trim());
    let legend;
    if (lm) { legend = lm[1]; i++; }
    ch.blocks.push({ t: 'fig', src: path.join(REPO, m[2]), cap, legend, maxH: legend ? PX_H_VISTA : PX_H_MAX });
    continue;
  }
  if (line.startsWith('|')) {
    flushPara();
    const rows = [];
    let j = i;
    while (j < lines.length && lines[j].trim().startsWith('|')) {
      const cells = lines[j].trim().replace(/^\||\|$/g, '').split('|').map((c) => c.trim());
      if (!cells.every((c) => /^:?-+:?$/.test(c))) rows.push(cells);
      j++;
    }
    i = j - 1;
    ch.blocks.push({ t: 'table', rows });
    continue;
  }
  if ((m = /^(\s*)(\*|\d+\.)\s+(.*)$/.exec(raw))) {
    flushPara();
    const level = m[1].length >= 2 ? 1 : 0;
    const ordered = /\d/.test(m[2]);
    const text = m[3];
    if (mode === 'tasks') {
      const lk = /^\[([^\]]+)\]\(#([^)]+)\)/.exec(text);
      ch.blocks[ch.blocks.length - 1].items.push({ text: lk[1], id: lk[2] });
      continue;
    }
    const item = { level, ordered, text };
    const target = mode === 'faq' && ch.blocks[ch.blocks.length - 1].t === 'faq' ? ch.blocks[ch.blocks.length - 1].a : ch.blocks;
    const last = target[target.length - 1];
    if (last && last.t === 'list') last.items.push(item); else target.push({ t: 'list', items: [item] });
    continue;
  }
  para.push(raw.replace(/\s+$/, (s) => (s.length >= 2 ? '  ' : '')));
}
flushPara();

// Vistas con datos de demostración (config.vistas): se insertan al final de la sección indicada,
// antes del siguiente título del mismo nivel o superior. Las imágenes las genera vistas/render-vistas.js.
const VISTAS_DIR = path.join(__dirname, 'assets/vistas');
for (const v of CFG.vistas || []) {
  const ref = ids[v.after];
  if (!ref) throw new Error(`config.vistas: no existe la sección "${v.after}" en el manual`);
  const src = path.join(VISTAS_DIR, v.file);
  if (!fs.existsSync(src)) throw new Error(`config.vistas: falta ${src}. Ejecute: npm run vistas`);
  const chapter = chapters[ref.chapter - 1];
  const at = chapter.blocks.findIndex((b) => b.bm === ref.bm && (b.t === 'h2' || b.t === 'h3'));
  const stop = chapter.blocks[at].t === 'h2' ? ['h2'] : ['h2', 'h3'];
  let end = chapter.blocks.findIndex((b, i) => i > at && stop.includes(b.t));
  if (end < 0) end = chapter.blocks.length;
  chapter.blocks.splice(end, 0, { t: 'fig', src, cap: `${v.caption} (datos de demostración)`, legend: v.legend, maxH: PX_H_VISTA });
}

// ---------- pase 2: render ----------
let olInstance = 0;

function inline(text, base = {}) {
  const runs = [];
  let bold = false, ital = false, code = false, buf = '';
  const st = () => ({ bold: base.bold || bold, italics: base.italics || ital, color: base.color, size: base.size, font: code ? 'Consolas' : undefined });
  const flush = () => { if (buf) { runs.push(new TextRun({ text: buf, ...st() })); buf = ''; } };
  text = stripEmoji(text);
  for (let i = 0; i < text.length;) {
    if (text.startsWith('**', i)) { flush(); bold = !bold; i += 2; continue; }
    if (text[i] === '*') { flush(); ital = !ital; i++; continue; }
    if (text[i] === '`') { flush(); code = !code; i++; continue; }
    if (text[i] === '\n') { flush(); runs.push(new TextRun({ text: '', break: 1 })); i++; continue; }
    if (text[i] === '[') {
      const m = /^\[([^\]]+)\]\(([^)]+)\)/.exec(text.slice(i));
      if (m) {
        flush();
        runs.push(...inline(m[1], { ...base, bold: base.bold || bold, italics: base.italics || ital }));
        const ref = ids[m[2].replace(/^#/, '')];
        if (ref) runs.push(...xref(ref, st()));
        i += m[0].length; continue;
      }
    }
    buf += text[i]; i++;
  }
  flush();
  return runs;
}
function xref(ref, style = {}) {
  const where = ref.num.includes('.') ? ref.num : `capítulo ${ref.num}`;
  return [
    new TextRun({ text: ` (véase ${where}, pág. `, ...style, bold: false }),
    new PageReference(ref.bm),
    new TextRun({ text: ')', ...style, bold: false }),
  ];
}
const P = (text, opts = {}) => new Paragraph({ children: inline(text, opts.run || {}), spacing: { after: 120 }, ...opts.p });

function border(color = RULE, size = 4) { return { style: BorderStyle.SINGLE, size, color }; }
const NOB = { style: BorderStyle.NONE, size: 0, color: 'FFFFFF' };
const allBorders = (b) => ({ top: b, bottom: b, left: b, right: b, insideHorizontal: b, insideVertical: b });

function cell(children, width, opts = {}) {
  return new TableCell({
    children, width: { size: width, type: WidthType.DXA },
    margins: { top: 80, bottom: 80, left: 120, right: 120 },
    verticalAlign: opts.vAlign || VerticalAlign.TOP,
    shading: opts.fill ? { type: ShadingType.CLEAR, color: 'auto', fill: opts.fill } : undefined,
    borders: opts.borders,
    columnSpan: opts.span,
  });
}
function grid(rows, widths, { header = true, headFill = BRAND_SOFT } = {}) {
  return new Table({
    width: { size: widths.reduce((a, b) => a + b, 0), type: WidthType.DXA },
    columnWidths: widths,
    borders: allBorders(border()),
    rows: rows.map((r, ri) => new TableRow({
      tableHeader: header && ri === 0,
      cantSplit: true,
      children: r.map((c, ci) => cell(
        Array.isArray(c) ? c : [new Paragraph({ children: inline(c, header && ri === 0 ? { bold: true, color: INK } : {}), spacing: { after: 0 } })],
        widths[ci], { fill: header && ri === 0 ? headFill : undefined },
      )),
    })),
  });
}
const spacer = (after = 120) => new Paragraph({ children: [], spacing: { after } });

function renderList(list) {
  const out = [];
  const inst = ++olInstance;
  for (const it of list.items) {
    const ref = it.ordered && it.level === 0 ? 'ordered' : 'bullets';
    out.push(new Paragraph({
      children: inline(it.text),
      numbering: { reference: ref, level: it.level, instance: ref === 'ordered' ? inst : undefined },
      spacing: { after: 60 },
    }));
  }
  out.push(spacer(60));
  return out;
}

function renderQuote(q) {
  // Caja de roles / prerrequisitos
  const kvClean = q.lines.map((l) => /^\*\*(Roles Responsables|Usuarios|Prerrequisitos):\*\*\s*(.*)$/.exec(stripEmoji(l)));
  if (kvClean.every(Boolean)) {
    return [grid(kvClean.map((m) => [[new Paragraph({ children: [new TextRun({ text: m[1], bold: true, color: BRAND })], spacing: { after: 0 } })],
      [new Paragraph({ children: inline(m[2].trim()), spacing: { after: 0 } })]]), [2300, CONTENT_W - 2300], { header: false }), spacer(160)];
  }
  const first = q.lines[0];
  const kind = /⚠/.test(first) ? 'importante' : /🔒/.test(first) ? 'seguridad' : /📌/.test(first) ? 'importante' : 'nota';
  let text = stripEmoji(q.lines.join('\n').replace(/\n\s*\n/g, '\n'));
  let label = kind === 'importante' ? 'Importante' : kind === 'seguridad' ? 'Seguridad' : 'Nota';
  const lm = /^\*\*(.+?):\*\*\s*/.exec(text);
  if (lm) { label = lm[1]; text = text.slice(lm[0].length); text = text.charAt(0).toUpperCase() + text.slice(1); }
  const c = CALLOUT[kind];
  return [new Table({
    width: { size: CONTENT_W, type: WidthType.DXA }, columnWidths: [CONTENT_W],
    rows: [new TableRow({ cantSplit: true, children: [cell([
      new Paragraph({ children: [new TextRun({ text: label.toUpperCase(), bold: true, size: 17, color: c.label, characterSpacing: 20 })], spacing: { after: 40 } }),
      new Paragraph({ children: inline(text), spacing: { after: 0 } }),
    ], CONTENT_W, { fill: c.fill, borders: { top: NOB, bottom: NOB, right: NOB, left: border(c.bar, 24) } })] })],
  }), spacer(160)];
}

function renderFlow(n) {
  const steps = (CFG.flows || {})[n];
  if (!steps) return [];
  const AW = 320;
  const sw = Math.floor((CONTENT_W - AW * (steps.length - 1)) / steps.length);
  const widths = [];
  const cells = [];
  steps.forEach((s, i) => {
    if (i) { widths.push(AW); cells.push(cell([new Paragraph({ alignment: AlignmentType.CENTER, children: [new TextRun({ text: '›', bold: true, size: 32, color: BRAND })] })], AW, { vAlign: VerticalAlign.CENTER, borders: allBorders(NOB) })); }
    widths.push(sw);
    cells.push(cell([
      new Paragraph({ alignment: AlignmentType.CENTER, spacing: { after: 20 }, children: [new TextRun({ text: String(i + 1), bold: true, size: 22, color: BRAND })] }),
      new Paragraph({ alignment: AlignmentType.CENTER, spacing: { after: 0 }, children: [new TextRun({ text: s, size: 17, color: INK })] }),
    ], sw, { fill: BRAND_SOFT, vAlign: VerticalAlign.CENTER, borders: allBorders(border(BRAND, 6)) }));
  });
  const total = widths.reduce((a, b) => a + b, 0);
  return [
    new Paragraph({ children: [new TextRun({ text: 'Resumen del proceso', bold: true, color: BRAND, size: 22 })], spacing: { before: 120, after: 100 }, keepNext: true }),
    new Table({ width: { size: total, type: WidthType.DXA }, columnWidths: widths, borders: allBorders(NOB), rows: [new TableRow({ cantSplit: true, children: cells })] }),
    spacer(200),
  ];
}

function renderTasks(b) {
  const rows = [['¿Qué necesita hacer?', 'Sección', 'Pág.']];
  for (const it of b.items) {
    const ref = ids[it.id];
    rows.push([
      [new Paragraph({ children: inline(it.text), spacing: { after: 0 } })],
      [new Paragraph({ children: [new TextRun(ref.num.includes('.') ? ref.num : `Cap. ${ref.num}`)], spacing: { after: 0 } })],
      [new Paragraph({ alignment: AlignmentType.RIGHT, children: [new PageReference(ref.bm)], spacing: { after: 0 } })],
    ]);
  }
  return [grid(rows, [CONTENT_W - 1300 - 900, 1300, 900]), spacer(200)];
}

const figCount = {};
function renderFig(b) {
  const { w, h, data } = pngSize(b.src);
  const size = fit(w, h, b.maxH);
  figCount[b.ch] = (figCount[b.ch] || 0) + 1;
  const out = [
    new Paragraph({ alignment: AlignmentType.CENTER, keepNext: true, spacing: { before: 120, after: 60 },
      children: [new ImageRun({ type: 'png', data, transformation: size, altText: { title: b.cap, description: b.cap, name: path.basename(b.src) } })] }),
    new Paragraph({ alignment: AlignmentType.CENTER, keepNext: !!b.legend, spacing: { after: b.legend ? 60 : 200 },
      children: [new TextRun({ text: `Figura ${b.ch}.${figCount[b.ch]}. `, bold: true, size: 18, color: INK }), new TextRun({ text: b.cap, size: 18, color: MUTED })] }),
  ];
  if (b.legend) {
    // Leyenda de las marcas numeradas de la vista: "1 Texto · 2 Texto".
    const runs = [];
    b.legend.split('·').map((s) => s.trim()).filter(Boolean).forEach((item, i) => {
      const m = /^(\d+)\s+(.*)$/.exec(item);
      if (i) runs.push(new TextRun({ text: '   ', size: 17 }));
      runs.push(new TextRun({ text: ` ${m ? m[1] : i + 1} `, bold: true, size: 15, color: 'FFFFFF', shading: { type: ShadingType.CLEAR, color: 'auto', fill: 'EA580C' } }));
      // Espacio de no separación: el número no queda al final de una línea y su texto en la siguiente.
      runs.push(new TextRun({ text: ` ${m ? m[2] : item}`, size: 17, color: INK }));
    });
    out.push(new Paragraph({ alignment: AlignmentType.CENTER, spacing: { after: 220, line: 300 }, children: runs }));
  }
  return out;
}

function renderFaqs(items) {
  const rows = [['Situación', 'Qué hacer']];
  for (const f of items) {
    const ans = [];
    for (const a of f.a) {
      if (a.t === 'p') ans.push(new Paragraph({ children: inline(a.text), spacing: { after: 60 } }));
      else ans.push(...renderList(a));
    }
    rows.push([[new Paragraph({ children: inline(f.q, { bold: true }), spacing: { after: 0 } })], ans]);
  }
  return [grid(rows, [3200, CONTENT_W - 3200]), spacer(200)];
}

function heading(level, num, title, bm, opts = {}) {
  const text = num ? `${num}  ${title}` : title;
  const run = new TextRun(text);
  return new Paragraph({ heading: level, children: [bm ? new Bookmark({ id: bm, children: [run] }) : run], ...opts });
}

function renderChapter(c) {
  const out = [heading('Heading1', `${c.num}.`, c.title, c.bm, { pageBreakBefore: true })];
  const faqs = [];
  for (const b of c.blocks) {
    b.ch = c.num;
    if (b.t === 'faq') { faqs.push(b); continue; }
    if (faqs.length) { out.push(...renderFaqs(faqs.splice(0))); }
    switch (b.t) {
      case 'label': out.push(new Paragraph({ children: [new TextRun({ text: b.text, bold: true, color: BRAND, size: 22 })], spacing: { before: 120, after: 60 }, keepNext: true })); break;
      case 'p': out.push(P(b.text)); break;
      case 'h2': out.push(heading('Heading2', b.num, b.title, b.bm)); break;
      case 'h3': out.push(heading('Heading3', b.num, b.title, b.bm)); break;
      case 'tasks': out.push(...renderFlow(c.num), ...renderTasks(b)); break;
      case 'list': out.push(...renderList(b)); break;
      case 'quote': out.push(...renderQuote(b)); break;
      case 'fig': out.push(...renderFig(b)); break;
      case 'table': out.push(grid(b.rows, [2000, 4321, CONTENT_W - 2000 - 4321]), spacer(160)); break;
    }
  }
  if (faqs.length) out.push(...renderFaqs(faqs));
  return out;
}

// ---------- portada, contenido, guía, glosario ----------
function cover() {
  const { info } = CFG.cover;
  return [
    spacer(1800),
    new Paragraph({ children: [new TextRun({ text: 'VERSIÓN DE CAPACITACIÓN', bold: true, size: 20, color: BRAND, characterSpacing: 60 })], spacing: { after: 240 } }),
    new Paragraph({ children: [new TextRun({ text: CFG.cover.title, bold: true, size: 56, color: INK })], spacing: { after: 60 } }),
    new Paragraph({ children: [new TextRun({ text: CFG.cover.subtitle, bold: true, size: 56, color: BRAND })], spacing: { after: 200 } }),
    new Paragraph({ children: [new TextRun({ text: CFG.cover.tagline, size: 26, color: MUTED })],
      border: { bottom: { style: BorderStyle.SINGLE, size: 18, color: BRAND, space: 12 } }, spacing: { after: 2400 } }),
    new Table({
      width: { size: CONTENT_W, type: WidthType.DXA }, columnWidths: [2000, CONTENT_W - 2000],
      borders: { top: NOB, left: NOB, right: NOB, bottom: border(), insideHorizontal: border(), insideVertical: NOB },
      rows: info.map(([k, v]) => new TableRow({ children: [
        cell([new Paragraph({ children: [new TextRun({ text: k, bold: true, color: MUTED, size: 19 })], spacing: { after: 0 } })], 2000),
        cell([new Paragraph({ children: [new TextRun({ text: v, size: 19 })], spacing: { after: 0 } })], CONTENT_W - 2000),
      ] })),
    }),
  ];
}

function contents() {
  return [
    new Paragraph({ pageBreakBefore: true, children: [new TextRun({ text: 'Contenido', bold: true, size: 32, color: BRAND })], spacing: { after: 240 } }),
    new TableOfContents('Contenido', { hyperlink: true, headingStyleRange: '1-2' }),
  ];
}

function howTo() {
  const map = [['Proceso', 'Quién lo realiza', 'Dónde se inicia', 'Pág.']];
  for (const c of chapters) {
    const pm = (CFG.processMap || {})[c.num] || { who: '', where: '' };
    map.push([
      [new Paragraph({ children: [new TextRun({ text: `${c.num}. ${c.title}`, bold: true })], spacing: { after: 0 } })],
      [new Paragraph({ children: inline(pm.who), spacing: { after: 0 } })],
      [new Paragraph({ children: inline(pm.where), spacing: { after: 0 } })],
      [new Paragraph({ alignment: AlignmentType.RIGHT, children: [new PageReference(c.bm)], spacing: { after: 0 } })],
    ]);
  }
  const conv = [
    ['Convención', 'Significado'],
    ['**"Radicar Solicitud"**', 'Texto en negrita y entre comillas: nombre exacto de un botón, pestaña o campo, tal como aparece en pantalla.'],
    ['*(véase 2.5, pág. 18)*', 'Remite a otra sección del manual donde se amplía el tema.'],
    ['Recuadro **NOTA**', 'Recomendación o aclaración útil para el paso.'],
    ['Recuadro **IMPORTANTE**', 'Regla que, si se pasa por alto, produce un error o un resultado no esperado.'],
    ['**Figura 2.1**', 'Imagen de la pantalla de referencia. Los datos que se ven son de ejemplo.'],
    ['Marcas numeradas', 'Recuadros naranjas numerados que ubican los elementos de la pantalla. La leyenda bajo la figura explica cada número.'],
    ['**Resumen del proceso**', 'Secuencia de pasos al inicio de cada capítulo, para ubicarse antes de entrar en detalle.'],
  ];
  return [
    new Paragraph({ heading: 'Heading1', pageBreakBefore: true, children: [new TextRun('Cómo usar este manual')] }),
    P(CFG.intro),
    new Paragraph({ children: [new TextRun({ text: 'Mapa de procesos', bold: true, color: BRAND, size: 22 })], spacing: { before: 200, after: 100 }, keepNext: true }),
    grid(map, [3000, 2700, CONTENT_W - 3000 - 2700 - 700, 700]),
    spacer(200),
    new Paragraph({ children: [new TextRun({ text: 'Convenciones', bold: true, color: BRAND, size: 22 })], spacing: { before: 200, after: 100 }, keepNext: true }),
    grid(conv, [2600, CONTENT_W - 2600]),
  ];
}

function glossary() {
  return [
    new Paragraph({ heading: 'Heading1', pageBreakBefore: true, children: [new TextRun('Glosario')] }),
    grid([['Término', 'Definición'], ...CFG.glossary.map(([t, d]) => [[new Paragraph({ children: [new TextRun({ text: t, bold: true })], spacing: { after: 0 } })], [new Paragraph({ children: inline(d), spacing: { after: 0 } })]])], [2900, CONTENT_W - 2900]),
  ];
}

// ---------- encabezado (réplica de la plantilla) y pie ----------
function header() {
  const logo = pngSize(LOGO).data;
  const hb = allBorders(border('000000', 4));
  const t = (text, size, bold) => new Paragraph({ alignment: AlignmentType.CENTER, spacing: { after: 0 }, children: [new TextRun({ text, size, bold, font: FONT })] });
  return new Header({ children: [
    new Table({
      alignment: AlignmentType.CENTER, layout: TableLayoutType.FIXED,
      width: { size: 10632, type: WidthType.DXA }, columnWidths: [2978, 5245, 2409], borders: hb,
      rows: [new TableRow({ height: { value: 1251, rule: HeightRule.ATLEAST }, children: [
        new TableCell({ width: { size: 2978, type: WidthType.DXA }, verticalAlign: VerticalAlign.CENTER, margins: { left: 70, right: 70 },
          children: [new Paragraph({ alignment: AlignmentType.CENTER, spacing: { after: 0 }, children: [new ImageRun({ type: 'png', data: logo, transformation: { width: 189, height: 74 } })] })] }),
        new TableCell({ width: { size: 5245, type: WidthType.DXA }, verticalAlign: VerticalAlign.CENTER, margins: { left: 70, right: 70 },
          children: [t(CFG.header.title, 28, true)] }),
        new TableCell({ width: { size: 2409, type: WidthType.DXA }, verticalAlign: VerticalAlign.CENTER, margins: { left: 70, right: 70 },
          children: [t(CFG.header.code, 24, true), t(CFG.header.version, 24, false)] }),
      ] })],
    }),
    new Paragraph({ children: [], spacing: { after: 0 } }),
  ] });
}
function footer() {
  return new Footer({ children: [new Paragraph({
    border: { top: { style: BorderStyle.SINGLE, size: 4, color: RULE, space: 6 } },
    children: [
      new TextRun({ text: CFG.footerText, size: 16, color: MUTED }),
      new TextRun({ children: [new PositionalTab({ alignment: PositionalTabAlignment.RIGHT, relativeTo: PositionalTabRelativeTo.MARGIN, leader: PositionalTabLeader.NONE }), 'Página ', PageNumber.CURRENT, ' de ', PageNumber.TOTAL_PAGES], size: 16, color: MUTED }),
    ],
  })] });
}

// ---------- documento ----------
const doc = new Document({
  creator: 'SI.SIOT', title: `${CFG.cover.title} — ${CFG.cover.subtitle} (Capacitación)`, description: 'Versión de capacitación del manual de usuario',
  features: { updateFields: true },
  styles: {
    default: { document: { run: { font: FONT, size: 21, color: INK }, paragraph: { spacing: { line: 276 } } } },
    paragraphStyles: [
      { id: 'Heading1', name: 'Heading 1', basedOn: 'Normal', next: 'Normal', quickFormat: true,
        run: { font: FONT, size: 32, bold: true, color: BRAND }, paragraph: { spacing: { before: 0, after: 240 }, keepNext: true, outlineLevel: 0,
          border: { bottom: { style: BorderStyle.SINGLE, size: 12, color: BRAND, space: 6 } } } },
      { id: 'Heading2', name: 'Heading 2', basedOn: 'Normal', next: 'Normal', quickFormat: true,
        run: { font: FONT, size: 26, bold: true, color: INK }, paragraph: { spacing: { before: 320, after: 120 }, keepNext: true, keepLines: true, outlineLevel: 1 } },
      { id: 'Heading3', name: 'Heading 3', basedOn: 'Normal', next: 'Normal', quickFormat: true,
        run: { font: FONT, size: 22, bold: true, color: BRAND }, paragraph: { spacing: { before: 220, after: 100 }, keepNext: true, keepLines: true, outlineLevel: 2 } },
      { id: 'TOC1', name: 'toc 1', basedOn: 'Normal', next: 'Normal', run: { size: 20, bold: true }, paragraph: { spacing: { before: 100, after: 20, line: 240 } } },
      { id: 'TOC2', name: 'toc 2', basedOn: 'Normal', next: 'Normal', run: { size: 19 }, paragraph: { spacing: { before: 0, after: 20, line: 240 }, indent: { left: 300 } } },
    ],
  },
  numbering: { config: [
    { reference: 'bullets', levels: [
      { level: 0, format: LevelFormat.BULLET, text: '•', alignment: AlignmentType.LEFT, style: { paragraph: { indent: { left: 360, hanging: 260 } } } },
      { level: 1, format: LevelFormat.BULLET, text: '–', alignment: AlignmentType.LEFT, style: { paragraph: { indent: { left: 720, hanging: 260 } } } },
    ] },
    { reference: 'ordered', levels: [
      { level: 0, format: LevelFormat.DECIMAL, text: '%1.', alignment: AlignmentType.LEFT, style: { paragraph: { indent: { left: 360, hanging: 300 } } } },
      { level: 1, format: LevelFormat.BULLET, text: '–', alignment: AlignmentType.LEFT, style: { paragraph: { indent: { left: 720, hanging: 260 } } } },
    ] },
  ] },
  sections: [{
    properties: { page: { size: { width: 12240, height: 15840 }, margin: { top: 1418, right: 1418, bottom: 1418, left: 1701, header: 567, footer: 720 } } },
    headers: { default: header() },
    footers: { default: footer() },
    children: [...cover(), ...contents(), ...howTo(), ...chapters.flatMap(renderChapter), ...glossary()],
  }],
});

fs.mkdirSync(path.dirname(OUT), { recursive: true });
Packer.toBuffer(doc).then((buf) => {
  fs.writeFileSync(OUT, buf);
  console.log('OK', OUT, buf.length, 'bytes;', chapters.map((c) => `${c.num}:${c.blocks.length}`).join(' '), 'ids:', Object.keys(ids).length);
});
