import { afterEach, describe, expect, it, vi } from 'vitest';
import { discover, initialShipment, loadShortlist, readSaved } from './api';

afterEach(() => { vi.unstubAllGlobals(); localStorage.clear(); });
describe('API boundary', () => {
  it('surfaces server validation details', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({ errors: { Origin: ['Origin is required.'] } }), { status: 400 })));
    await expect(discover(initialShipment(), new AbortController().signal)).rejects.toThrow('Origin is required.');
  });
  it('retains valid saved IDs and rejects damaged browser storage', () => {
    const id = '00000000-0000-0000-0000-000000000001';
    localStorage.setItem('wayfinder.shortlist.v1', JSON.stringify([id, id, 10, 'unsafe/path']));
    expect(readSaved()).toEqual([id]);
    localStorage.setItem('wayfinder.shortlist.v1', 'not json'); expect(readSaved()).toEqual([]);
  });
  it('omits deleted carriers from the shortlist', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(null, { status: 404 })));
    const result = await loadShortlist(['00000000-0000-0000-0000-000000000001'], new AbortController().signal);
    expect(result.items).toEqual([]); expect(result.totalCount).toBe(0);
  });
});
