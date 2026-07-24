/* ============================================================
   Orchestration — relie l'UI aux endpoints et aux animations.
   ============================================================ */
(() => {
  const $ = (s) => document.querySelector(s);
  const monthFmt = new Intl.DateTimeFormat('fr-FR', { month: 'long' });
  let lastEtat = null;

  const range = $('#horizon'), out = $('#horizonOut');
  const runBtn = $('#runBtn');

  function setBusy(btn, busy, label) {
    if (!btn) return;
    const l = btn.querySelector('.btn__label');
    if (busy) { btn.classList.add('is-busy'); btn.dataset.prev = l ? l.textContent : ''; if (l && label) l.textContent = label; }
    else { btn.classList.remove('is-busy'); if (l && btn.dataset.prev) l.textContent = btn.dataset.prev; }
  }

  function paintRange() {
    const pct = ((range.value - range.min) / (range.max - range.min)) * 100;
    range.style.background = `linear-gradient(90deg,var(--accent) ${pct}%,rgba(255,255,255,.09) ${pct}%)`;
    out.textContent = range.value;
  }

  async function refreshEtat() {
    try {
      const e = await API.etat();
      lastEtat = e;
      Motion.count($('#kpiEcr'), e.nbEcritures);
      Motion.count($('#kpiPlafond'), Math.round(e.plafond), { suffix: ' MAD' });
      $('#kpiEcrFoot').textContent = e.dateMin
        ? `du ${new Date(e.dateMin).toLocaleDateString('fr-FR')} au ${new Date(e.dateMax).toLocaleDateString('fr-FR')}`
        : 'en attente de données';
    } catch (err) { /* silencieux au démarrage */ }
    return lastEtat;
  }

  function setConfidence(tr, bg) {
    const worst = Math.min(tr?.confiance ?? 1, bg?.confiance ?? 1);
    const kpi = $('#kpiConf'), foot = $('#kpiConfFoot');
    if (worst === 0) { kpi.textContent = 'Indicative'; foot.textContent = 'historique court (< 12 points)'; }
    else { kpi.textContent = 'Élevée'; foot.textContent = 'historique suffisant'; }
  }

  function setAlert(bg) {
    const card = $('#kpiAlertCard'), val = $('#kpiAlert'), foot = $('#kpiAlertFoot');
    const first = (bg?.points || []).find((p) => p.alerteDepassement);
    if (first) {
      const m = monthFmt.format(new Date(first.periode));
      val.textContent = m.charAt(0).toUpperCase() + m.slice(1);
      foot.textContent = 'plafond franchi — alerte anticipée';
      card.classList.add('is-hot');
      Motion.toast('Dépassement anticipé ⚠️', `Plafond budgétaire franchi dès ${m}.`, 'err');
    } else {
      val.textContent = 'Aucun';
      foot.textContent = 'sous le plafond voté';
      card.classList.remove('is-hot');
    }
  }

  async function runForecasts() {
    const horizon = +range.value;
    const budgetMonths = Math.max(3, Math.round(horizon / 30));
    setBusy(runBtn, true, 'Calcul en cours');
    try {
      await API.recalculer();
      const [tr, bg] = await Promise.all([
        API.tresorerie(horizon, 'Jour'),
        API.budgetaire(budgetMonths, lastEtat?.exercice || undefined),
      ]);
      Charts.render($('#chartTreso'), tr, { kind: 'treso' });
      Charts.render($('#chartBudget'), bg, { kind: 'budget', ceiling: lastEtat?.plafond });
      setConfidence(tr, bg);
      setAlert(bg);
      Motion.toast('Prévision générée', `Horizon ${horizon} j · trésorerie & budget.`, 'ok');
    } catch (err) {
      Motion.toast('Erreur de calcul', String(err.message || err), 'err');
    } finally { setBusy(runBtn, false); }
  }

  async function loadDemo(btn) {
    setBusy(btn, true, 'Chargement');
    try {
      await API.seedDemo();
      await refreshEtat();
      Motion.toast('Démo chargée', 'Jeu de données réaliste importé.', 'ok');
      document.getElementById('dashboard').scrollIntoView({ behavior: 'smooth' });
      await runForecasts();
    } catch (err) {
      Motion.toast('Échec du chargement', String(err.message || err), 'err');
    } finally { setBusy(btn, false); }
  }

  function wireDrop(id, kind) {
    const zone = $('#' + id), input = zone.querySelector('input');
    const done = () => { zone.classList.add('is-done'); zone.querySelector('.drop__state').textContent = 'importé'; };
    const upload = async (file) => {
      if (!file) return;
      try {
        await (kind === 'ecritures' ? API.importEcritures(file) : API.importBudget(file));
        done();
        Motion.toast('Fichier importé', `${file.name}`, 'ok');
        await refreshEtat();
      } catch (err) { Motion.toast('Import refusé', String(err.message || err), 'err'); }
    };
    input.addEventListener('change', () => upload(input.files[0]));
    ['dragenter', 'dragover'].forEach((ev) => zone.addEventListener(ev, (e) => { e.preventDefault(); zone.classList.add('is-over'); }));
    ['dragleave', 'drop'].forEach((ev) => zone.addEventListener(ev, (e) => { e.preventDefault(); zone.classList.remove('is-over'); }));
    zone.addEventListener('drop', (e) => upload(e.dataTransfer.files[0]));
  }

  function wireScroll() {
    document.querySelectorAll('[data-scrollto-target]').forEach((b) =>
      b.addEventListener('click', () => document.querySelector(b.dataset.scrolltoTarget)?.scrollIntoView({ behavior: 'smooth' })));
    document.querySelectorAll('[data-scrollto]').forEach((a) =>
      a.addEventListener('click', (e) => { e.preventDefault(); document.querySelector(a.getAttribute('href'))?.scrollIntoView({ behavior: 'smooth' }); }));
  }

  function init() {
    Motion.init();
    paintRange();
    wireScroll();
    wireDrop('dropEcritures', 'ecritures');
    wireDrop('dropBudget', 'budget');
    range.addEventListener('input', paintRange);
    range.addEventListener('change', () => { if (lastEtat?.nbEcritures) runForecasts(); });
    runBtn.addEventListener('click', runForecasts);
    $('#ctaDemo').addEventListener('click', (e) => loadDemo(e.currentTarget));
    $('#navDemo').addEventListener('click', (e) => loadDemo(e.currentTarget));
    refreshEtat().then((e) => { if (e && e.nbEcritures > 0) runForecasts(); });
  }

  if (document.readyState !== 'loading') init();
  else document.addEventListener('DOMContentLoaded', init);
})();
