import { defineConfig, devices } from '@playwright/test'

// End-to-end tests: a real browser against the real backend and the Vite dev server.
// The backend runs in demo mode with its own throwaway database (asp-backend/e2e.db),
// which is deleted before every run - your justone.db and demo.db are never touched.
// Port 5250 must be free, because the frontend always talks to http://localhost:5250.
const FRONTEND_PORT = 5175

export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  workers: 1,
  retries: 0,
  timeout: 30_000,
  reporter: 'list',
  use: {
    baseURL: `http://localhost:${FRONTEND_PORT}`,
    trace: 'retain-on-failure',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: [
    {
      cwd: '../asp-backend',
      command:
        'node -e "for (const f of [\'e2e.db\', \'e2e.db-shm\', \'e2e.db-wal\']) require(\'fs\').rmSync(f, { force: true })" && ' +
        'dotnet run --launch-profile http -- --DemoData:Enabled=true "--ConnectionStrings:DefaultConnection=Data Source=e2e.db"',
      url: 'http://localhost:5250/swagger/index.html',
      // Never reuse a running backend: the tests write to its database.
      reuseExistingServer: false,
      timeout: 180_000,
      stdout: 'ignore',
      stderr: 'pipe',
    },
    {
      command: `npm run dev -- --port ${FRONTEND_PORT} --strictPort`,
      url: `http://localhost:${FRONTEND_PORT}`,
      reuseExistingServer: true,
      timeout: 60_000,
    },
  ],
})
