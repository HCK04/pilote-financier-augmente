/* Client API — enveloppe fine autour des endpoints REST du module. */
const API = (() => {
  const base = '/api';

  async function json(path, opts = {}) {
    const res = await fetch(base + path, opts);
    if (!res.ok) {
      let detail = res.statusText;
      try { detail = (await res.text()) || detail; } catch {}
      throw new Error(`${res.status} — ${detail}`);
    }
    return res.status === 204 ? null : res.json();
  }

  const upload = (kind, file) => {
    const fd = new FormData();
    fd.append('fichier', file);
    return json(`/import/${kind}`, { method: 'POST', body: fd });
  };

  return {
    etat:        () => json('/etat'),
    seedDemo:    () => json('/demo/seed', { method: 'POST' }),
    recalculer:  () => json('/recalculer', { method: 'POST' }),
    importEcritures: (f) => upload('ecritures', f),
    importBudget:    (f) => upload('budget', f),
    mapping:     (rows) => json('/mapping', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(rows),
    }),
    tresorerie:  (horizon, gran = 'Jour') =>
      json(`/previsions/tresorerie?horizon=${horizon}&granularite=${gran}`),
    budgetaire:  (horizon, exercice) =>
      json(`/previsions/budgetaire?horizon=${horizon}` + (exercice ? `&exercice=${exercice}` : '')),
  };
})();
