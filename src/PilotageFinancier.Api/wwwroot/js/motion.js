/* ============================================================
   Moteur d'interactions — Anime.js
   Boot cinématique, typographie kinétique, aimantation, tilt 3D,
   curseur, révélations, compteurs, toasts.
   ============================================================ */
const Motion = (() => {
  const reduce = matchMedia('(prefers-reduced-motion:reduce)').matches;
  const nfmt = new Intl.NumberFormat('fr-FR');

  /* ---------- Curseur : projecteur + point ---------- */
  function cursor() {
    const spot = document.getElementById('spotlight');
    const dot = document.getElementById('cursorDot');
    if (!dot) return;
    let tx = innerWidth / 2, ty = innerHeight / 2, x = tx, y = ty;
    addEventListener('pointermove', (e) => {
      tx = e.clientX; ty = e.clientY;
      spot.style.setProperty('--mx', (tx / innerWidth * 100) + '%');
      spot.style.setProperty('--my', (ty / innerHeight * 100) + '%');
    }, { passive: true });
    (function loop() {
      x += (tx - x) * 0.22; y += (ty - y) * 0.22;
      dot.style.transform = `translate(${x}px,${y}px) translate(-50%,-50%)`;
      requestAnimationFrame(loop);
    })();
    document.querySelectorAll('a,button,[data-tilt],[data-magnetic]').forEach((el) => {
      el.addEventListener('pointerenter', () => dot.classList.add('is-lg'));
      el.addEventListener('pointerleave', () => dot.classList.remove('is-lg'));
    });
  }

  /* ---------- Aimantation ---------- */
  function magnetic() {
    document.querySelectorAll('[data-magnetic]').forEach((el) => {
      const strength = 0.4;
      el.addEventListener('pointermove', (e) => {
        const r = el.getBoundingClientRect();
        const dx = e.clientX - (r.left + r.width / 2);
        const dy = e.clientY - (r.top + r.height / 2);
        anime.remove(el);
        el.style.transform = `translate(${dx * strength}px,${dy * strength}px)`;
      });
      el.addEventListener('pointerleave', () => {
        anime({ targets: el, translateX: 0, translateY: 0, easing: 'easeOutElastic(1,.5)', duration: 900 });
      });
    });
  }

  /* ---------- Tilt 3D + reflet lumineux (hors cartes du deck) ---------- */
  function tilt() {
    document.querySelectorAll('[data-tilt]:not(.deck__card)').forEach((el) => {
      const depth = +(el.dataset.depth || 12);
      el.style.transformStyle = 'preserve-3d';
      const sheen = document.createElement('div');
      Object.assign(sheen.style, {
        position: 'absolute', inset: '0', borderRadius: 'inherit', pointerEvents: 'none',
        opacity: '0', transition: 'opacity .3s', mixBlendMode: 'soft-light',
      });
      el.appendChild(sheen);
      el.addEventListener('pointermove', (e) => {
        const r = el.getBoundingClientRect();
        const px = (e.clientX - r.left) / r.width - 0.5;
        const py = (e.clientY - r.top) / r.height - 0.5;
        anime.remove(el);
        el.style.transform =
          `perspective(900px) rotateY(${px * 9}deg) rotateX(${-py * 9}deg) translateZ(${depth}px)`;
        sheen.style.opacity = '1';
        sheen.style.background =
          `radial-gradient(340px circle at ${(px + .5) * 100}% ${(py + .5) * 100}%,rgba(255,255,255,.16),transparent 60%)`;
      });
      el.addEventListener('pointerleave', () => {
        sheen.style.opacity = '0';
        anime({
          targets: el, rotateX: 0, rotateY: 0, translateZ: 0,
          easing: 'easeOutElastic(1,.55)', duration: 1100,
          update: (a) => {
            const t = a.animatables[0].target;
            el.style.transform = `perspective(900px) rotateY(${t.rotateY || 0}deg) rotateX(${t.rotateX || 0}deg)`;
          },
        });
      });
    });
  }

  /* ---------- Cartes 3D flottantes du hero ---------- */
  function deck() {
    const wrap = document.getElementById('deck');
    if (!wrap) return;
    const cards = [...wrap.querySelectorAll('.deck__card')];
    cards.forEach((c, i) => {
      const depth = +(c.dataset.depth || 20);
      c.style.transform = `translateZ(${depth}px)`;
      if (reduce) return;
      anime({
        targets: c, translateY: [ -10, 12 ], translateZ: depth, rotateZ: [ -1.4, 1.4 ],
        direction: 'alternate', loop: true, easing: 'easeInOutSine',
        duration: 4200 + i * 900, delay: i * 400,
      });
    });
    const hero = document.getElementById('hero');
    hero && hero.addEventListener('pointermove', (e) => {
      const r = hero.getBoundingClientRect();
      const px = (e.clientX - r.left) / r.width - 0.5;
      const py = (e.clientY - r.top) / r.height - 0.5;
      wrap.style.transform = `rotateY(${px * 12}deg) rotateX(${-py * 12}deg)`;
    });
    hero && hero.addEventListener('pointerleave', () => {
      anime({ targets: wrap, rotateX: 0, rotateY: 0, easing: 'easeOutElastic(1,.5)', duration: 1200,
        update: (a) => { const t = a.animatables[0].target; wrap.style.transform = `rotateY(${t.rotateY || 0}deg) rotateX(${t.rotateX || 0}deg)`; } });
    });
  }

  /* ---------- Typographie kinétique ---------- */
  function kinetic() {
    const el = document.getElementById('heroTitle');
    if (!el) return;
    const words = el.textContent.trim().split(' ');
    el.innerHTML = words.map((w) => {
      const accent = /avenir/i.test(w) ? ' accent' : '';
      return `<span class="kw${accent}">${w}</span>`;
    }).join(' ');
    if (reduce) return;
    anime.set('.hero__title .kw', { opacity: 0, translateY: 46, rotateX: -80, translateZ: -60 });
    return anime({
      targets: '.hero__title .kw',
      opacity: [0, 1], translateY: [46, 0], rotateX: [-80, 0], translateZ: [-60, 0],
      easing: 'spring(1,80,11,0)', duration: 1400, delay: anime.stagger(85, { start: 250 }),
    });
  }

  /* ---------- Boot cinématique ---------- */
  function boot() {
    const el = document.getElementById('boot');
    if (!el || reduce) { el && el.remove(); return Promise.resolve(); }
    const tl = anime.timeline({ easing: 'easeOutExpo' });
    tl.add({ targets: '.boot__mark span', scaleY: [0.3, 1], opacity: [0.5, 1],
             delay: anime.stagger(90), duration: 500 })
      .add({ targets: '.boot__mark span', scaleY: [1, 0.3], delay: anime.stagger(60), duration: 380 }, '+=120')
      .add({ targets: '.boot__label', opacity: [1, 0], duration: 300 }, '-=300')
      .add({ targets: el, opacity: [1, 0], duration: 500, complete: () => el.remove() });
    return tl.finished;
  }

  /* ---------- Révélations au défilement ---------- */
  function reveals() {
    const items = document.querySelectorAll('[data-reveal]');
    if (reduce) { items.forEach((i) => (i.style.opacity = 1)); return; }
    const io = new IntersectionObserver((entries) => {
      entries.forEach((en) => {
        if (!en.isIntersecting) return;
        anime({
          targets: en.target, opacity: [0, 1], translateY: [34, 0], rotateX: [12, 0],
          easing: 'cubicBezier(.16,1,.3,1)', duration: 1000,
        });
        io.unobserve(en.target);
      });
    }, { threshold: 0.16 });
    items.forEach((i) => io.observe(i));
  }

  /* ---------- Nav collée ---------- */
  function navStick() {
    const nav = document.getElementById('nav');
    addEventListener('scroll', () => nav.classList.toggle('is-stuck', scrollY > 40), { passive: true });
  }

  /* ---------- Compteurs animés ---------- */
  function count(el, to, { suffix = '' } = {}) {
    if (!el) return;
    const sfx = suffix || el.dataset.suffix || '';
    const obj = { v: parseFloat((el.textContent || '0').replace(/[^\d.-]/g, '')) || 0 };
    anime({
      targets: obj, v: to, round: 1, easing: 'easeOutExpo', duration: 1600,
      update: () => { el.textContent = nfmt.format(Math.round(obj.v)) + sfx; },
    });
  }

  /* ---------- Sheen des boutons ---------- */
  function sheens() {
    document.querySelectorAll('.btn__sheen').forEach((s) => {
      const btn = s.closest('.btn');
      btn.addEventListener('pointerenter', () => {
        anime.remove(s);
        anime({ targets: s, left: ['-60%', '160%'], easing: 'easeInOutQuad', duration: 700 });
      });
    });
  }

  /* ---------- Toasts ---------- */
  function toast(title, sub = '', type = '') {
    const host = document.getElementById('toasts');
    const t = document.createElement('div');
    t.className = 'toast ' + type;
    t.innerHTML = `<div><div>${title}</div>${sub ? `<small>${sub}</small>` : ''}</div>`;
    host.appendChild(t);
    anime({ targets: t, translateX: [60, 0], scale: [0.9, 1], opacity: [0, 1], easing: 'spring(1,90,12,0)', duration: 800 });
    setTimeout(() => {
      anime({ targets: t, translateX: [0, 80], opacity: [1, 0], easing: 'easeInBack', duration: 500,
        complete: () => t.remove() });
    }, 3600);
  }

  function init() {
    cursor(); magnetic(); tilt(); deck(); navStick(); reveals(); sheens();
    boot().then(() => {
      anime({ targets: '.nav', opacity: [0, 1], translateY: [-16, 0], easing: 'easeOutExpo', duration: 800 });
      kinetic();
      anime({ targets: '.hero__inner [data-reveal]', opacity: [0, 1], translateY: [26, 0],
        easing: 'easeOutExpo', duration: 900, delay: anime.stagger(120, { start: 500 }) });
      anime({ targets: '.deck__card', opacity: [0, 1], scale: [0.85, 1], translateY: [40, 0],
        easing: 'spring(1,70,12,0)', delay: anime.stagger(140, { start: 650 }) });
      document.querySelectorAll('.deck [data-count]').forEach((el) => count(el, +el.dataset.count));
    });
  }

  return { init, count, toast };
})();
