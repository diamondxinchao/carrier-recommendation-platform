const paths = {
  route: 'M4 5h4m8 14h4M8 5c8 0-4 14 8 14M6 3a2 2 0 1 0 0 4 2 2 0 0 0 0-4m12 14a2 2 0 1 0 0 4 2 2 0 0 0 0-4',
  search: 'm21 21-5-5M18 10a8 8 0 1 1-16 0 8 8 0 0 1 16 0',
  bookmark: 'M6 3h12v18l-6-4-6 4V3Z',
  arrow: 'M4 12h16m-6-6 6 6-6 6',
  truck: 'M2 5h12v12H2V5Zm12 5h4l4 4v3h-8M8 18a2 2 0 1 1-4 0 2 2 0 0 1 4 0m12 0a2 2 0 1 1-4 0 2 2 0 0 1 4 0',
  check: 'm5 12 4 4L19 6',
  spark: 'm12 3 2.5 6.5L21 12l-6.5 2.5L12 21l-2.5-6.5L3 12l6.5-2.5L12 3Z',
  shield: 'm12 2 9 4v6c0 5-9 10-9 10S3 17 3 12V6l9-4Zm-5 10 3 3 7-7',
  clock: 'M12 7v5l3 2m7-2a10 10 0 1 1-20 0 10 10 0 0 1 20 0',
  info: 'M12 11v6m0-10v.1m10 4.9a10 10 0 1 1-20 0 10 10 0 0 1 20 0',
};
export default function Icon({ name, size = 20 }: { name: keyof typeof paths; size?: number }) {
  return <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true"><path d={paths[name]} /></svg>;
}
