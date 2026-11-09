export type Equipment = 'dryVan' | 'refrigerated' | 'flatbed';
export type Sort = 'recommended' | 'rate' | 'name';
export type Carrier = {
  id: string; name: string; dotNumber: string; equipment: Equipment; capacityLb: number;
  ratePerMile: number; onTimeRate: number; safetyScore: number; completedLoads: number;
  availableFrom: string; availableThrough: string;
  score: { reliability: number; safety: number; experience: number; total: number };
};
export type Shipment = {
  origin: string; destination: string; equipment: Equipment; pickupDate: string;
  weightLb: number; maxRatePerMile: number | null; query: string; sort: Sort; page: number; pageSize: number;
};
export type Page = { items: Carrier[]; totalCount: number; totalPages: number; page: number; pageSize: number; rankingVersion: string };
export const equipmentLabels: Record<Equipment, string> = { dryVan: 'Dry van', refrigerated: 'Refrigerated', flatbed: 'Flatbed' };
export const cities = ['Chicago, IL', 'Dallas, TX', 'Atlanta, GA', 'Los Angeles, CA', 'Seattle, WA', 'Denver, CO'];
export const todayUtc = () => new Date().toISOString().slice(0, 10);
export const initialShipment = (): Shipment => ({
  origin: cities[0], destination: cities[1], equipment: 'dryVan', pickupDate: todayUtc(),
  weightLb: 20000, maxRatePerMile: null, query: '', sort: 'recommended', page: 1, pageSize: 6,
});

async function read<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { title?: string; errors?: Record<string, string[]> } | null;
    const details = problem?.errors ? Object.values(problem.errors).flat().join(' ') : problem?.title;
    throw new Error(details || `Request failed (${response.status}). Please try again.`);
  }
  return response.json() as Promise<T>;
}

export async function discover(shipment: Shipment, signal: AbortSignal): Promise<Page> {
  return read<Page>(await fetch('/api/recommendations', {
    method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(shipment), signal,
  }));
}
export async function loadShortlist(ids: string[], signal: AbortSignal): Promise<Page> {
  const items = await Promise.all(ids.map(async id => {
    const response = await fetch(`/api/carriers/${encodeURIComponent(id)}`, { signal });
    return response.status === 404 ? null : read<Carrier>(response);
  }));
  const available = items.filter((c): c is Carrier => c !== null);
  return { items: available, totalCount: available.length, totalPages: 1, page: 1, pageSize: available.length, rankingVersion: 'shortlist' };
}

export function readSaved(): string[] {
  try {
    const value: unknown = JSON.parse(localStorage.getItem('wayfinder.shortlist.v1') || '[]');
    return Array.isArray(value) ? [...new Set(value.filter((id): id is string =>
      typeof id === 'string' && /^[a-f0-9-]{36}$/i.test(id)))].slice(0, 100) : [];
  } catch { return []; }
}
