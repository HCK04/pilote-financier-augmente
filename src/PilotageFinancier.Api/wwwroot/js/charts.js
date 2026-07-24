/* ============================================================
   Graphiques SVG sur mesure — tracé animé, bande de confiance,
   ligne de plafond et marqueurs de dépassement pulsés.
   ============================================================ */
const Charts = (() => {
  const NS = 'http://www.w3.org/2000/svg';
  const compact = new Intl.NumberFormat('fr-FR', { notation: 'compact', maximumFractionDigits: 1 });
  const full = new Intl.NumberFormat('fr-FR');
  const el = (n, a = {}) => { const e = document.createElementNS(NS, n); for (const k in a) e.setAttribute(k, a[k]); return e; };
  const fmtDate = (s) => { const d = new Date(s); return `${String(d.getDate()).padStart(2,'0')}/${String(d.getMonth()+1).padStart(2,'0')}`; };

  function render(container, data, opts = {}) {
    const kind = opts.kind || 'treso';
    const pts = data.points || [];
    container.innerHTML = '';
    if (!pts.length) return;

    const W = container.clientWidth || 520, H = container.clientHeight || 300;
    const P = { l: 46, r: 16, t: 18, b: 30 };
    const iw = W - P.l - P.r, ih = H - P.t - P.b;
    const accent = kind === 'budget' ? '232,128,140' : '87,171,156';

    let lo = Infinity, hi = -Infinity;
    pts.forEach((p) => { lo = Math.min(lo, p.borneInf, p.valeur); hi = Math.max(hi, p.borneSup, p.valeur); });
    if (opts.ceiling != null) hi = Math.max(hi, opts.ceiling);
    if (kind === 'treso') { lo = Math.min(lo, 0); }
    const pad = (hi - lo) * 0.12 || 1; lo -= pad; hi += pad;

    const X = (i) => P.l + (pts.length === 1 ? iw / 2 : (i / (pts.length - 1)) * iw);
    const Y = (v) => P.t + ih - ((v - lo) / (hi - lo)) * ih;

    const svg = el('svg', { viewBox: `0 0 ${W} ${H}` });

    // Palette plate (aucun dégradé) — un seul ton par série.

    const ticks = 4;
    for (let t = 0; t <= ticks; t++) {
      const v = lo + (t / ticks) * (hi - lo), y = Y(v);
      svg.appendChild(el('line', { x1: P.l, y1: y, x2: W - P.r, y2: y, stroke: 'rgba(255,255,255,.06)', 'stroke-width': 1 }));
      const lbl = el('text', { x: P.l - 8, y: y + 4, fill: 'rgba(154,160,182,.8)', 'font-size': 10, 'text-anchor': 'end' });
      lbl.textContent = compact.format(v);
      svg.appendChild(lbl);
    }
    const stepX = Math.max(1, Math.floor(pts.length / 5));
    for (let i = 0; i < pts.length; i += stepX) {
      const lbl = el('text', { x: X(i), y: H - 8, fill: 'rgba(154,160,182,.7)', 'font-size': 10, 'text-anchor': 'middle' });
      lbl.textContent = fmtDate(pts[i].periode);
      svg.appendChild(lbl);
    }

    const hasBand = pts.some((p) => p.borneSup !== p.valeur || p.borneInf !== p.valeur);
    let band;
    if (hasBand) {
      let up = '', dn = '';
      pts.forEach((p, i) => { up += `${i ? 'L' : 'M'}${X(i)} ${Y(p.borneSup)} `; });
      for (let i = pts.length - 1; i >= 0; i--) dn += `L${X(i)} ${Y(pts[i].borneInf)} `;
      band = el('path', { d: up + dn + 'Z', fill: `rgba(${accent},.12)`, stroke: 'none', opacity: 0 });
      svg.appendChild(band);
    }

    let area;
    if (kind === 'treso') {
      let d = `M${X(0)} ${Y(0)} `;
      pts.forEach((p, i) => { d += `L${X(i)} ${Y(p.valeur)} `; });
      d += `L${X(pts.length - 1)} ${Y(0)} Z`;
      area = el('path', { d, fill: `rgba(${accent},.10)`, opacity: 0 });
      svg.appendChild(area);
    }

    if (opts.ceiling != null) {
      const yc = Y(opts.ceiling);
      const cap = el('line', { x1: P.l, y1: yc, x2: W - P.r, y2: yc, stroke: 'rgb(232,128,140)', 'stroke-width': 1.5, 'stroke-dasharray': '6 6', opacity: 0 });
      svg.appendChild(cap);
      const ct = el('text', { x: W - P.r, y: yc - 7, fill: 'rgb(232,128,140)', 'font-size': 10, 'text-anchor': 'end' });
      ct.textContent = 'Plafond ' + compact.format(opts.ceiling);
      svg.appendChild(ct);
      anime({ targets: cap, opacity: [0, 1], easing: 'easeOutQuad', duration: 600, delay: 900 });
      anime({ targets: ct, opacity: [0, .9], duration: 600, delay: 1000 });
    }

    let ld = '';
    pts.forEach((p, i) => { ld += `${i ? 'L' : 'M'}${X(i)} ${Y(p.valeur)} `; });
    const line = el('path', { d: ld, fill: 'none', stroke: `rgb(${accent})`, 'stroke-width': 2.4, 'stroke-linecap': 'round', 'stroke-linejoin': 'round' });
    svg.appendChild(line);

    const dots = [];
    pts.forEach((p, i) => {
      const alert = !!p.alerteDepassement;
      const c = el('circle', { cx: X(i), cy: Y(p.valeur), r: alert ? 5 : 3,
        fill: alert ? 'rgb(232,128,140)' : '#fff', stroke: `rgb(${accent})`, 'stroke-width': alert ? 2 : 1, opacity: 0 });
      if (alert) { c.style.filter = 'drop-shadow(0 0 8px rgba(232,128,140,.9))'; c.classList.add('alert-dot'); }
      c.dataset.i = i;
      svg.appendChild(c); dots.push(c);
    });

    container.appendChild(svg);

    const len = line.getTotalLength();
    line.setAttribute('stroke-dasharray', len);
    line.setAttribute('stroke-dashoffset', len);
    anime({ targets: line, strokeDashoffset: [len, 0], easing: 'easeInOutSine', duration: 1500, delay: 250 });
    if (area) anime({ targets: area, opacity: [0, 1], easing: 'easeOutQuad', duration: 900, delay: 900 });
    if (band) anime({ targets: band, opacity: [0, 1], easing: 'easeOutQuad', duration: 900, delay: 700 });
    anime({ targets: dots, opacity: [0, 1], scale: [0, 1], transformOrigin: '50% 50%',
      easing: 'spring(1,80,10,0)', delay: anime.stagger(28, { start: 1100 }) });
    const alertDots = dots.filter((d) => d.classList.contains('alert-dot'));
    if (alertDots.length) anime({ targets: alertDots, r: [5, 8], opacity: [1, .5], direction: 'alternate',
      loop: true, easing: 'easeInOutSine', duration: 900, delay: 1600 });

    const tip = document.createElement('div'); tip.className = 'tip'; container.appendChild(tip);
    svg.addEventListener('pointermove', (e) => {
      const rect = svg.getBoundingClientRect();
      const mx = (e.clientX - rect.left) / rect.width * W;
      let best = 0, bd = Infinity;
      pts.forEach((p, i) => { const d = Math.abs(X(i) - mx); if (d < bd) { bd = d; best = i; } });
      const p = pts[best];
      tip.style.left = (X(best) / W * rect.width) + 'px';
      tip.style.top = (Y(p.valeur) / H * rect.height) + 'px';
      tip.innerHTML = `${fmtDate(p.periode)} — <b>${full.format(Math.round(p.valeur))}</b>` +
        (p.alerteDepassement ? ' ⚠️' : '');
      tip.style.opacity = 1;
      dots.forEach((d, i) => d.setAttribute('r', i === best ? 6 : (d.classList.contains('alert-dot') ? 5 : 3)));
    });
    svg.addEventListener('pointerleave', () => { tip.style.opacity = 0; });
  }

  return { render };
})();
