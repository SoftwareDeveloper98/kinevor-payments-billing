import { chromium } from 'playwright';
import { mkdir } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import { dirname, resolve } from 'node:path';

const here = dirname(fileURLToPath(import.meta.url));
const output = resolve(here, '..', '..', 'docs', 'screenshots');
const webUrl = process.env.KINEVOR_WEB_URL ?? 'http://localhost:4206';
const apiUrl = process.env.KINEVOR_API_URL ?? 'http://localhost:5087';
const channel = process.env.PLAYWRIGHT_CHANNEL ?? 'chrome';

const browser = await chromium.launch(channel ? { channel } : {});
const page = await browser.newPage({ viewport: { width: 1440, height: 1040 } });

try {
  await mkdir(output, { recursive: true });

  await page.goto(`${webUrl}/billing`, { waitUntil: 'domcontentloaded' });
  await page.waitForSelector('.hero-panel .hero-amount', { timeout: 30000 });
  await page.waitForSelector('.metric-card', { timeout: 30000 });
  await page.waitForTimeout(900);
  await page.screenshot({
    path: resolve(output, 'web-billing-dashboard.png'),
    fullPage: true,
  });
  console.log('saved web-billing-dashboard.png');

  await page.goto(`${webUrl}/billing/invoices`, { waitUntil: 'domcontentloaded' });
  await page.waitForSelector('.billing-table tbody tr', { timeout: 30000 });
  await page.waitForTimeout(700);
  await page.screenshot({
    path: resolve(output, 'web-invoice-list.png'),
    fullPage: true,
  });
  console.log('saved web-invoice-list.png');

  const openInvoice = page.locator('tbody tr:has(.status-open) .invoice-link').first();
  const fallbackInvoice = page.locator('.invoice-link').first();
  const invoiceLink = (await openInvoice.count()) > 0 ? openInvoice : fallbackInvoice;
  const href = await invoiceLink.getAttribute('href');

  if (!href) throw new Error('No invoice link found in the list.');

  await page.goto(`${webUrl}${href}`, { waitUntil: 'domcontentloaded' });
  await page.waitForSelector('.detail-head', { timeout: 30000 });
  await page.waitForSelector('.payment-card, .settled-card', { timeout: 30000 });
  await page.waitForTimeout(700);
  await page.screenshot({
    path: resolve(output, 'web-invoice-detail.png'),
    fullPage: true,
  });
  console.log('saved web-invoice-detail.png');

  await page.goto(`${apiUrl}/swagger/index.html`, { waitUntil: 'domcontentloaded' });
  await page.waitForSelector('.swagger-ui .info', { timeout: 30000 });
  await page.waitForTimeout(800);
  await page.screenshot({
    path: resolve(output, 'api-swagger.png'),
    fullPage: true,
  });
  console.log('saved api-swagger.png');
} finally {
  await browser.close();
}
