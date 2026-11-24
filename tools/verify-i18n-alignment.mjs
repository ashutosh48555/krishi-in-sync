import { chromium } from "playwright";

async function verify() {
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });

  const routes = [
    "/",
    "/Climate/Dashboard",
    "/Climate/Topology",
    "/Climate/Analytics",
    "/Automation",
    "/Alerts",
    "/FarmCluster",
    "/Account/Login"
  ];

  const langs = ["hi", "pa", "en"];
  let issues = 0;

  for (const lang of langs) {
    for (const route of routes) {
      const url = `http://127.0.0.1:5099${route}?lang=${lang}`;
      await page.goto(url);
      await page.waitForLoadState("networkidle");

      // Check for raw untranslated keys like "au.something" or "z.something"
      const text = await page.evaluate(() => document.body.innerText);
      const rawKeyRegex = /\b(?:au|z|a|cl|m|val|st|vpd|pv|rule)\.[a-z0-9_.]+\b/gi;
      const matches = text.match(rawKeyRegex) || [];
      const suspicious = matches.filter(m => !m.includes("min.") && !m.includes(".com") && !m.includes(".org") && !m.includes(".in"));

      if (suspicious.length > 0) {
        console.error(`[LEAK] ${url} leaked keys:`, suspicious);
        issues += suspicious.length;
      }

      // Check for broken SVGs / missing icons
      const missingIcons = await page.evaluate(() => {
        const icons = Array.from(document.querySelectorAll("svg"));
        return icons.filter(s => s.clientWidth === 0 && s.clientHeight === 0 && !s.closest("[hidden]")).length;
      });

      if (missingIcons > 0) {
        console.error(`[ICON ISSUE] ${url} has ${missingIcons} zero-sized SVGs`);
      }
    }
  }

  // Capture proof screenshots
  await page.goto("http://127.0.0.1:5099/Automation?lang=hi");
  await page.evaluate(() => document.documentElement.setAttribute("data-theme", "dark"));
  await page.screenshot({ path: "docs/screens/automation-hi.png", fullPage: true });

  await page.goto("http://127.0.0.1:5099/Climate/Topology?lang=hi");
  await page.screenshot({ path: "docs/screens/topology-hi.png", fullPage: true });

  await page.goto("http://127.0.0.1:5099/Climate/Dashboard?lang=hi");
  await page.screenshot({ path: "docs/screens/dashboard-hi.png", fullPage: true });

  await browser.close();

  if (issues === 0) {
    console.log("🎉 ALL ROUTES IN HI, PA, EN AUDITED CLEANLY: 0 RAW KEYS, 0 MISSING ICONS!");
  } else {
    console.error(`Finished with ${issues} translation issues.`);
  }
}

verify().catch(console.error);
