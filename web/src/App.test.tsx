import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, expect, it, vi } from 'vitest';
import App from './App';

afterEach(() => { vi.unstubAllGlobals(); localStorage.clear(); });
it('explains empty results and lets the shipper reset', async () => {
  const fetch = vi.fn().mockImplementation(() => Promise.resolve(new Response(JSON.stringify({ items: [], totalCount: 0, totalPages: 0, page: 1, pageSize: 6, rankingVersion: 'quality-v1' }))));
  vi.stubGlobal('fetch', fetch); render(<App />);
  expect(await screen.findByText('No carriers fit these criteria')).toBeInTheDocument();
  await userEvent.click(screen.getByRole('button', { name: 'Reset shipment' }));
  expect(fetch.mock.calls.length).toBeGreaterThanOrEqual(2);
});
it('shows a useful failure and retries the request', async () => {
  const fetch = vi.fn().mockRejectedValue(new Error('Service temporarily unavailable.'));
  vi.stubGlobal('fetch', fetch); render(<App />);
  expect(await screen.findByRole('alert')).toHaveTextContent('Service temporarily unavailable.');
  await userEvent.click(screen.getByRole('button', { name: 'Try again' }));
  expect(fetch.mock.calls.length).toBeGreaterThanOrEqual(2);
});
