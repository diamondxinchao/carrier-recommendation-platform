import { useEffect, useState, type FormEvent } from 'react';
import { cities, discover, equipmentLabels, initialShipment, loadShortlist, readSaved, todayUtc,
  type Carrier, type Equipment, type Page, type Shipment, type Sort } from './api';
import Icon from './Icon';

function CarrierResult({ carrier: c, saved, toggle, ranked }: { carrier: Carrier; saved: boolean; toggle: () => void; ranked: boolean }) {
  return <article className="carrier-card">
    <div className="card-main">
      <div className="carrier-avatar" aria-hidden="true">{c.name.split(' ').slice(0, 2).map(s => s[0]).join('')}</div>
      <div className="carrier-identity"><h3>{c.name}</h3><p>Demo DOT {c.dotNumber} <span>·</span> {equipmentLabels[c.equipment]}</p></div>
      {ranked && <div className="match-score"><Icon name="spark" size={15} /><strong>{c.score.total.toFixed(1)}</strong><span>match score</span></div>}
      <button className={`save-button ${saved ? 'saved' : ''}`} aria-label={`${saved ? 'Remove' : 'Save'} ${c.name}${saved ? ' from' : ' to'} shortlist`} aria-pressed={saved} onClick={toggle}><Icon name="bookmark" /></button>
    </div>
    <div className="card-metrics">
      <div><span><Icon name="clock" size={14} /> On-time delivery</span><strong>{(c.onTimeRate * 100).toFixed(0)}<small>%</small></strong></div>
      <div><span><Icon name="shield" size={14} /> Safety score</span><strong>{c.safetyScore}<small> / 100</small></strong></div>
      <div><span><Icon name="truck" size={14} /> Completed loads</span><strong>{c.completedLoads.toLocaleString()}</strong></div>
      <div className="rate"><span>Illustrative rate</span><strong>${c.ratePerMile.toFixed(2)}<small> / mi</small></strong></div>
    </div>
    <div className="card-footer"><span className="capacity"><span className="status-dot" />{c.capacityLb.toLocaleString()} lb capacity</span>
      <details><summary>Why this score <span>+</span></summary><div className="explanation">
        <p>The quality score combines three historical signals. Eligibility is checked separately against your shipment.</p>
        <dl><div><dt>Reliability</dt><dd>{c.score.reliability.toFixed(1)} / 50</dd></div><div><dt>Safety</dt><dd>{c.score.safety.toFixed(1)} / 30</dd></div><div><dt>Experience</dt><dd>{c.score.experience.toFixed(1)} / 20</dd></div></dl>
        <p>Experience reaches its maximum at 500 completed loads. Rates are estimates; availability must be reconfirmed before booking.</p>
      </div></details>
    </div>
  </article>;
}

export default function App() {
  const [draft, setDraft] = useState<Shipment>(initialShipment);
  const [applied, setApplied] = useState<Shipment>(initialShipment);
  const [view, setView] = useState<'discover' | 'shortlist'>('discover');
  const [saved, setSaved] = useState<string[]>(readSaved);
  const [page, setPage] = useState<Page | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [storageNotice, setStorageNotice] = useState('');
  const [retry, setRetry] = useState(0);
  const [help, setHelp] = useState(false);
  // Saved IDs affect the request only in shortlist view.
  const shortlistKey = view === 'shortlist' ? saved.join(',') : '';
  useEffect(() => {
    const controller = new AbortController();
    setLoading(true); setError(''); setPage(null);
    const request = view === 'discover' ? discover(applied, controller.signal)
      : loadShortlist(shortlistKey ? shortlistKey.split(',') : [], controller.signal);
    request.then(result => { if (!controller.signal.aborted) setPage(result); })
      .catch((e: unknown) => { if (!controller.signal.aborted) setError(e instanceof Error ? e.message : 'Unable to load carriers.'); })
      .finally(() => { if (!controller.signal.aborted) setLoading(false); });
    return () => controller.abort();
  }, [applied, view, shortlistKey, retry]);

  function toggle(id: string) {
    const next = saved.includes(id) ? saved.filter(x => x !== id) : [...saved, id];
    if (next.length > 100) { setStorageNotice('Your shortlist can hold up to 100 carriers.'); return; }
    setSaved(next);
    try { localStorage.setItem('wayfinder.shortlist.v1', JSON.stringify(next)); setStorageNotice(''); }
    catch { setStorageNotice('Your shortlist is available for this visit. Browser storage is unavailable.'); }
  }
  function submit(event: FormEvent) { event.preventDefault(); setApplied({ ...draft, page: 1 }); }
  function change<K extends keyof Shipment>(key: K, value: Shipment[K]) { setDraft(d => ({ ...d, [key]: value })); }
  const isShortlist = view === 'shortlist';

  return <div className="app-shell">
    <a className="skip-link" href="#main">Skip to content</a>
    <aside className="sidebar">
      <a className="brand" href="#" onClick={e => { e.preventDefault(); setView('discover'); }}><span className="brand-mark"><Icon name="route" size={25} /></span>wayfinder<span className="brand-period">.</span></a>
      <div className="workspace-label">SHIPPER WORKSPACE</div>
      <nav aria-label="Main navigation">
        <button className={!isShortlist ? 'active' : ''} onClick={() => setView('discover')} aria-current={!isShortlist ? 'page' : undefined}><Icon name="search" />Carrier discovery<Icon name="arrow" size={16} /></button>
        <button className={isShortlist ? 'active' : ''} onClick={() => setView('shortlist')} aria-current={isShortlist ? 'page' : undefined}><Icon name="bookmark" />My shortlist<span className="nav-count">{saved.length}</span></button>
      </nav>
      <div className="sidebar-bottom"><div className="sidebar-note"><Icon name="route" size={27} /><h3>A better way forward.</h3><p>The right carrier starts with the right information.</p><button onClick={() => setHelp(!help)}>How matching works <Icon name="arrow" size={15} /></button></div><div className="account"><div className="account-avatar">D</div><div><strong>Demo workspace</strong><span>Shipper experience</span></div><span className="status-dot" /></div></div>
    </aside>
    <div className="main-shell"><header className="topbar"><span>Workspace <span className="breadcrumb-slash">/</span> <strong>{isShortlist ? 'My shortlist' : 'Carrier discovery'}</strong></span><span className="demo-pill"><span className="status-dot" />Demo environment</span></header>
      <main id="main"><div className="page-heading"><div><div className="eyebrow">MOVE WITH CONFIDENCE</div><h1>{isShortlist ? 'Your next move, saved.' : 'Good freight. Great partners.'}</h1><p>{isShortlist ? 'Keep promising carriers close. Recheck availability when your shipment is ready.' : 'Find reliable carriers that fit your shipment, backed by clear performance signals.'}</p></div><span className="heading-route"><Icon name="route" size={52} /></span></div>
        {help && <section className="help-panel" aria-label="How matching works"><h2>Clear rules. Useful matches.</h2><p>First, we check the exact route, equipment, capacity, pickup date, and your optional rate limit. Then we rank every eligible carrier by reliability (50%), safety (30%), and experience (20%).</p><p>This demo uses fictional records and illustrative scores. It does not contact carriers or make bookings.</p><button onClick={() => setHelp(false)}>Close explanation</button></section>}
        {!isShortlist && <section className="shipment-panel" aria-labelledby="shipment-heading"><div className="section-label"><span className="section-icon"><Icon name="truck" size={18} /></span><h2 id="shipment-heading">Where are we heading?</h2><span>Tell us about your shipment</span></div>
          <form onSubmit={submit}><div className="form-grid">
            <label>Origin<select value={draft.origin} onChange={e => change('origin', e.target.value)}>{cities.map(city => <option key={city}>{city}</option>)}</select></label>
            <label>Destination<select value={draft.destination} onChange={e => change('destination', e.target.value)}>{cities.map(city => <option key={city}>{city}</option>)}</select></label>
            <label>Pickup date<input type="date" required min={todayUtc()} value={draft.pickupDate} onChange={e => change('pickupDate', e.target.value)} /></label>
            <label>Equipment<select value={draft.equipment} onChange={e => change('equipment', e.target.value as Equipment)}>{Object.entries(equipmentLabels).map(([value, text]) => <option key={value} value={value}>{text}</option>)}</select></label>
            <label>Shipment weight <span className="label-hint">lb</span><input type="number" required min="1" max="100000" step="1" value={draft.weightLb} onChange={e => change('weightLb', Number(e.target.value))} /></label>
            <label>Maximum rate <span className="label-hint">USD / mi · optional</span><input type="number" min="0.01" max="100" step="0.01" placeholder="No limit" value={draft.maxRatePerMile ?? ''} onChange={e => change('maxRatePerMile', e.target.value === '' ? null : Number(e.target.value))} /></label>
          </div><div className="form-bottom"><span><Icon name="shield" size={15} />Your shipment sets the criteria. Performance sets the order.</span><button className="primary" type="submit">Find my carriers <Icon name="arrow" size={17} /></button></div></form>
        </section>}
        {storageNotice && <p className="notice" role="status">{storageNotice}</p>}
        <section aria-labelledby="results-heading" className="results-section"><div className="results-toolbar"><div><h2 id="results-heading">{isShortlist ? 'Saved carriers' : 'Your carrier matches'}{page && <span className="result-count">{page.totalCount}</span>}</h2><p>{isShortlist ? 'Saved on this browser. These carriers may not match your current shipment.' : `${applied.origin} → ${applied.destination} · ${equipmentLabels[applied.equipment]} · ${applied.weightLb.toLocaleString()} lb`}</p></div>
          {!isShortlist && <label className="sort-label">Sort by<select value={applied.sort} onChange={e => { const sort = e.target.value as Sort; setApplied(a => ({ ...a, sort, page: 1 })); change('sort', sort); }}><option value="recommended">Best match</option><option value="rate">Lowest rate</option><option value="name">Carrier name</option></select></label>}</div>
          {!isShortlist && <form className="name-search" onSubmit={e => { e.preventDefault(); setApplied(a => ({ ...a, query: draft.query, page: 1 })); }}><Icon name="search" size={17} /><input aria-label="Search carrier name or DOT" placeholder="Search by carrier name or demo DOT number" value={draft.query} onChange={e => change('query', e.target.value)} maxLength={100} /><button type="submit">Search</button></form>}
          {loading && <div className="loading-state" role="status"><span className="spinner" />Finding the right carriers…</div>}
          {error && <div className="empty-state" role="alert"><Icon name="info" size={30} /><h3>We couldn’t load your carriers</h3><p>{error}</p><button className="secondary" onClick={() => setRetry(x => x + 1)}>Try again</button></div>}
          {page && page.items.length === 0 && <div className="empty-state"><Icon name={isShortlist ? 'bookmark' : 'search'} size={34} /><h3>{isShortlist ? 'Your shortlist starts here' : 'No carriers fit these criteria'}</h3><p>{isShortlist ? 'Save a carrier using the bookmark button to compare it later.' : 'Try a different route, pickup date, equipment type, or rate limit.'}</p><button className="secondary" onClick={() => { if (isShortlist) setView('discover'); else { const initial = initialShipment(); setDraft(initial); setApplied(initial); } }}>{isShortlist ? 'Discover carriers' : 'Reset shipment'}</button></div>}
          <div className="carrier-list">{page?.items.map(carrier => <CarrierResult key={carrier.id} carrier={carrier} saved={saved.includes(carrier.id)} toggle={() => toggle(carrier.id)} ranked={page.rankingVersion === 'quality-v1'} />)}</div>
          {page && !isShortlist && page.totalPages > 1 && <nav className="pagination" aria-label="Results pages"><span>Page {page.page} of {page.totalPages}</span><div><button disabled={page.page <= 1} onClick={() => setApplied(a => ({ ...a, page: a.page - 1 }))}>Previous</button><button disabled={page.page >= page.totalPages} onClick={() => setApplied(a => ({ ...a, page: a.page + 1 }))}>Next <Icon name="arrow" size={14} /></button></div></nav>}
        </section><footer className="page-footer"><Icon name="info" size={14} /><span>Fictional carriers and illustrative performance data. Confirm rates and availability before booking.</span><button onClick={() => setHelp(!help)}>About this demo</button></footer>
      </main>
    </div>
  </div>;
}
