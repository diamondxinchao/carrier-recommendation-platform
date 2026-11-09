import { defineConfig, devices } from '@playwright/test';
export default defineConfig({
  testDir: './e2e', fullyParallel: true, retries: process.env.CI ? 1 : 0,
  use: { baseURL: 'http://127.0.0.1:5173', trace: 'retain-on-failure' },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }, { name: 'mobile', use: { ...devices['iPhone 13'], defaultBrowserType: 'chromium' } }],
  webServer: [
    { command: 'dotnet run --project ../src/Carrier.Api --no-launch-profile --urls http://localhost:5080', url: 'http://localhost:5080/health/ready', env: { ASPNETCORE_ENVIRONMENT: 'Development' }, reuseExistingServer: !process.env.CI, timeout: 120000 },
    { command: 'pnpm dev', url: 'http://127.0.0.1:5173', reuseExistingServer: !process.env.CI },
  ],
});
