import { defineConfig, devices } from '@playwright/test';
import * as dotenv from 'dotenv';
import * as path from 'path';

// Load environment variables from .env file (if present)
dotenv.config({ path: path.resolve(__dirname, '.env') });

const BASE_URL = process.env.BASE_URL || 'http://localhost:5063';
const API_BASE_URL = process.env.API_BASE_URL || 'http://localhost:5181';

export default defineConfig({
  // ── Global settings ──
  testDir: './tests',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? Number(process.env.RETRIES) || 2 : Number(process.env.RETRIES) || 1,
  workers: Number(process.env.WORKERS) || 2,
  timeout: Number(process.env.TEST_TIMEOUT) || 60_000,
  expect: {
    timeout: Number(process.env.EXPECT_TIMEOUT) || 15_000,
  },

  // ── Reporter configuration ──
  reporter: [
    ['html', { outputFolder: 'playwright-report', open: 'never' }],
    ['list', { printSteps: true }],
    ['json', { outputFile: 'test-results/results.json' }],
    ['junit', { outputFile: 'test-results/junit.xml' }],
  ],

  // ── Global setup / teardown ──
  globalSetup: undefined, // Add path to global setup if needed
  globalTeardown: undefined,

  // ── Shared settings ──
  use: {
    baseURL: BASE_URL,
    apiBaseURL: API_BASE_URL,

    // Screenshot on first retry failure
    screenshot: process.env.SCREENSHOT_ON_FAILURE !== 'false' ? 'only-on-failure' : 'off',
    video: 'retain-on-failure',
    trace: 'retain-on-failure',

    // Default navigation timeout
    navigationTimeout: Number(process.env.NAVIGATION_TIMEOUT) || 30_000,

    // Extra HTTP headers sent with every request
    extraHTTPHeaders: {
      'X-Test-Run': 'playwright-e2e',
    },

    // Ignore HTTPS errors in local development
    ignoreHTTPSErrors: true,
  },

  // ── Browser / device projects ──
  projects: [
    {
      name: 'chromium',
      use: {
        ...devices['Desktop Chrome'],
        viewport: { width: 1440, height: 900 },
        launchOptions: {
          args: ['--disable-dev-shm-usage'], // CI stability
        },
      },
    },

    {
      name: 'firefox',
      use: {
        ...devices['Desktop Firefox'],
        viewport: { width: 1440, height: 900 },
      },
    },

    {
      name: 'webkit',
      use: {
        ...devices['Desktop Safari'],
        viewport: { width: 1440, height: 900 },
      },
    },

    // ── Mobile viewport (responsive testing) ──
    {
      name: 'mobile-chrome',
      use: {
        ...devices['Pixel 7'],
      },
    },

    {
      name: 'mobile-safari',
      use: {
        ...devices['iPhone 14'],
      },
    },
  ],

  // ── Global timeout for hooks ──
  globalTimeout: 30 * 60 * 1000, // 30 minutes max total run
});
