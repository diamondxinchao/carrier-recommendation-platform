import { expect, test } from '@playwright/test';

test('shipper can discover, explain, and save a carrier', async ({ page }) => {
  await page.goto('/');
  await expect(page.getByRole('heading', { name: 'Good freight. Great partners.' })).toBeVisible();
  await expect(page.locator('.carrier-card').first()).toBeVisible();
  const first = page.locator('.carrier-card').first();
  const name = await first.getByRole('heading').innerText();
  await first.getByText('Why this score').click();
  await expect(first.getByText('Reliability', { exact: true })).toBeVisible();
  await first.getByRole('button', { name: /Save .* to shortlist/ }).click();
  await page.getByRole('button', { name: /My shortlist/ }).click();
  await expect(page.getByRole('heading', { name })).toBeVisible();
  await page.reload();
  await page.getByRole('button', { name: /My shortlist/ }).click();
  await expect(page.getByRole('heading', { name })).toBeVisible();
  await page.getByRole('button', { name: /Remove .* from shortlist/ }).click();
  await expect(page.getByText('Your shortlist starts here')).toBeVisible();
});

test('a shipment with an impossible rate shows an actionable empty state', async ({ page }) => {
  await page.goto('/');
  await page.getByLabel(/Maximum rate/).fill('0.01');
  await page.getByRole('button', { name: 'Find my carriers' }).click();
  await expect(page.getByRole('heading', { name: 'No carriers fit these criteria' })).toBeVisible();
  await page.getByRole('button', { name: 'Reset shipment' }).click();
  await expect(page.locator('.carrier-card').first()).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBeTruthy();
});
