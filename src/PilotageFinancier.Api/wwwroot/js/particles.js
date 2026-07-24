/* Champ de particules orbitales sur canvas — profondeur ambiante, réactif au curseur.
   requestAnimationFrame (hors Anime.js) pour rester à 60 FPS sur le fond. */
(() => {
  const canvas = document.getElementById('field');
  if (!canvas) return;
  const ctx = canvas.getContext('2d');
  const reduce = matchMedia('(prefers-reduced-motion:reduce)').matches;
  let w, h, dpr, parts = [];
  const mouse = { x: -999, y: -999 };
  const COUNT = window.innerWidth < 760 ? 34 : 66;
  const palette = ['150,155,175', '150,155,175', '124,108,255']; /* neutre + rares points accent */

  function resize() {
    dpr = Math.min(devicePixelRatio || 1, 2);
    w = canvas.width = innerWidth * dpr;
    h = canvas.height = innerHeight * dpr;
    canvas.style.width = innerWidth + 'px';
    canvas.style.height = innerHeight + 'px';
  }

  function seed() {
    parts = Array.from({ length: COUNT }, () => ({
      x: Math.random() * w, y: Math.random() * h,
      z: Math.random() * 0.8 + 0.2,
      vx: (Math.random() - .5) * 0.14,
      vy: (Math.random() - .5) * 0.14,
      c: palette[(Math.random() * palette.length) | 0],
      r: Math.random() * 1.6 + 0.6,
    }));
  }

  function step() {
    ctx.clearRect(0, 0, w, h);
    const mx = mouse.x * dpr, my = mouse.y * dpr;
    for (let i = 0; i < parts.length; i++) {
      const p = parts[i];
      p.x += p.vx * p.z * dpr; p.y += p.vy * p.z * dpr;
      const dx = mx - p.x, dy = my - p.y, d2 = dx * dx + dy * dy;
      if (d2 < (170 * dpr) ** 2) { p.x += dx * 0.0009 * p.z; p.y += dy * 0.0009 * p.z; }
      if (p.x < 0) p.x = w; if (p.x > w) p.x = 0;
      if (p.y < 0) p.y = h; if (p.y > h) p.y = 0;

      ctx.beginPath();
      ctx.arc(p.x, p.y, p.r * p.z * dpr, 0, 6.283);
      ctx.fillStyle = `rgba(${p.c},${0.18 + p.z * 0.5})`;
      ctx.fill();

      for (let j = i + 1; j < parts.length; j++) {
        const q = parts[j], lx = p.x - q.x, ly = p.y - q.y, l2 = lx * lx + ly * ly;
        const max = 120 * dpr;
        if (l2 < max * max) {
          const a = (1 - Math.sqrt(l2) / max) * 0.16 * Math.min(p.z, q.z);
          ctx.strokeStyle = `rgba(${p.c},${a})`;
          ctx.lineWidth = dpr * 0.6;
          ctx.beginPath(); ctx.moveTo(p.x, p.y); ctx.lineTo(q.x, q.y); ctx.stroke();
        }
      }
    }
    raf = requestAnimationFrame(step);
  }

  let raf;
  addEventListener('resize', () => { resize(); seed(); }, { passive: true });
  addEventListener('pointermove', (e) => { mouse.x = e.clientX; mouse.y = e.clientY; }, { passive: true });
  resize(); seed();
  if (!reduce) step();
  else { ctx.clearRect(0, 0, w, h); }
})();
