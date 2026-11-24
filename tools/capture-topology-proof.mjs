import { chromium } from "playwright";

async function run() {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({
    viewport: { width: 1440, height: 900 }
  });
  const page = await context.newPage();

  // Test dark mode
  await page.goto("http://127.0.0.1:5099/Climate/Topology");
  await page.evaluate(() => document.documentElement.setAttribute("data-theme", "dark"));
  await page.waitForTimeout(1000);

  const cards = await page.$$eval(".zc", els => els.map(el => {
    const rect = el.getBoundingClientRect();
    const ah = el.querySelector(".ah")?.getBoundingClientRect();
    const pt = el.querySelector(".pt")?.getBoundingClientRect();
    return {
      id: el.id,
      height: rect.height,
      bottom: rect.bottom,
      ahTop: ah ? ah.top : null,
      ptLeft: pt ? pt.left : null
    };
  }));

  console.log("Card metrics (dark):", JSON.stringify(cards, null, 2));

  await page.screenshot({ path: "docs/screens/topology-aligned-dark.png", fullPage: true });

  // Test light mode
  await page.evaluate(() => document.documentElement.setAttribute("data-theme", "light"));
  await page.waitForTimeout(500);
  await page.screenshot({ path: "docs/screens/topology-aligned-light.png", fullPage: true });

  await browser.close();
}

run().catch(console.error);
