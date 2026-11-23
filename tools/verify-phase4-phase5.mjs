import { chromium } from "playwright";
import axe from "axe-core";

const BASE = process.env.BASE ?? "http://127.0.0.1:5099";

async function main() {
  console.log("=== STARTING PHASE 4 & PHASE 5 VERIFICATION ===");
  const browser = await chromium.launch();
  let totalViolations = 0;
  const consoleErrors = [];
  const networkRequests = [];

  const context = await browser.newContext({
    viewport: { width: 1440, height: 900 }
  });

  const page = await context.newPage();
  page.on("dialog", (d) => d.accept());

  page.on("console", (m) => {
    if (m.type() === "error") {
      consoleErrors.push(`[Console Error] ${m.text()}`);
    }
  });

  page.on("pageerror", (e) => {
    consoleErrors.push(`[Page Error] ${e.message}`);
  });

  page.on("request", (req) => {
    const url = req.url();
    networkRequests.push(url);
    const parsed = new URL(url);
    if (
      !parsed.hostname.includes("127.0.0.1") &&
      !parsed.hostname.includes("localhost") &&
      !parsed.hostname.includes("clerk") &&
      !parsed.hostname.includes("neon.tech")
    ) {
      console.error(`[LEAK DETECTED] Request to external origin: ${url}`);
    }
  });

  // 1. HOME & 3D FARM VERIFICATION
  console.log("\n--- Testing 3D Farm on / ---");
  await page.goto(`${BASE}/`, { waitUntil: "networkidle" });

  const canvas = page.locator("#stage canvas");
  console.log(`Canvas visible: ${await canvas.isVisible()}`);
  const ariaLabel = await canvas.getAttribute("aria-label");
  console.log(`Canvas aria-label: "${ariaLabel}"`);

  // Test zone selection via buttons
  const polyBtn = page.locator('#zbs button[data-z="poly"]');
  if (await polyBtn.isVisible()) {
    await polyBtn.click();
    console.log(`Poly button aria-pressed: ${await polyBtn.getAttribute("aria-pressed")}`);
  }

  // Test slider
  const slider = page.locator("#hour");
  await slider.fill("20");
  await slider.dispatchEvent("input");
  const timeLabel = await page.locator("#hl").textContent();
  console.log(`Time label after slider moved to 20: "${timeLabel.trim()}"`);

  // Test WebGL fallback display
  const fallbackTested = await page.evaluate(() => {
    const stage = document.getElementById("stage");
    stage.classList.add("nogl");
    const fb = document.getElementById("fb");
    const fbStyle = window.getComputedStyle(fb);
    const cv = stage.querySelector("canvas");
    const cvStyle = window.getComputedStyle(cv);
    const pass = fbStyle.display === "grid" && cvStyle.display === "none";
    stage.classList.remove("nogl");
    return pass;
  });
  console.log(`WebGL fallback styles (.nogl hides canvas, shows #fb): ${fallbackTested}`);

  // Test Theme switch on Home
  console.log("\n--- Testing Theme Switch ---");
  const themeBtn = page.locator("#theme");
  const themeBefore = await page.evaluate(() => document.documentElement.dataset.theme);
  console.log(`Theme before toggle: ${themeBefore}`);
  await themeBtn.click();
  await page.waitForTimeout(600);
  const themeAfter = await page.evaluate(() => document.documentElement.dataset.theme);
  console.log(`Theme after toggle: ${themeAfter}`);
  // Toggle back
  await themeBtn.click();
  await page.waitForTimeout(600);

  // 2. DASHBOARD & CHARTS
  console.log("\n--- Testing Dashboard & Charts ---");
  await page.goto(`${BASE}/Climate/Dashboard`, { waitUntil: "networkidle" });

  const tempChart = page.locator('canvas[data-chart="temp"]');
  const vpdChart = page.locator('canvas[data-chart="vpd"]');
  console.log(`Dashboard Temp Chart visible: ${await tempChart.isVisible()}`);
  console.log(`Dashboard Temp Chart aria-label: "${await tempChart.getAttribute("aria-label")}"`);
  console.log(`Dashboard VPD Chart visible: ${await vpdChart.isVisible()}`);
  console.log(`Dashboard VPD Chart aria-label: "${await vpdChart.getAttribute("aria-label")}"`);

  // Test "View data as table" toggle
  const toggleTempTable = page.locator('[data-table-toggle="tbl-temp"]');
  console.log(`Table toggle initial aria-expanded: ${await toggleTempTable.getAttribute("aria-expanded")}`);
  await toggleTempTable.click();
  console.log(`Table toggle after click aria-expanded: ${await toggleTempTable.getAttribute("aria-expanded")}`);
  const tblTemp = page.locator("#tbl-temp");
  console.log(`Table container hidden after click: ${await tblTemp.getAttribute("hidden")}`);
  const thCount = await page.locator("#tbl-temp th[scope='col']").count();
  console.log(`Table column headers count: ${thCount}`);

  // 3. HISTORY PAGE CHARTS
  console.log("\n--- Testing History Charts ---");
  await page.goto(`${BASE}/Climate/Analytics`, { waitUntil: "networkidle" });
  const hTempChart = page.locator('canvas[data-chart="temp"]');
  const hVpdChart = page.locator('canvas[data-chart="vpd"]');
  console.log(`History Temp Chart visible: ${await hTempChart.isVisible()}`);
  console.log(`History Temp Chart aria-label: "${await hTempChart.getAttribute("aria-label")}"`);

  // 4. AXE-CORE ACCESSIBILITY AUDIT ACROSS ALL PAGES
  console.log("\n--- Running axe-core Accessibility Audit ---");
  const pagesToAudit = [
    "/",
    "/Climate/Dashboard",
    "/Climate/Analytics",
    "/Climate/Topology",
    "/Alerts",
    "/Automation",
    "/FarmCluster",
    "/FarmCluster/Create",
    "/Account/Login",
    "/Home/Privacy",
    "/Account/AccessDenied",
    "/Account/NotFound",
  ];

  for (const theme of ["light", "dark"]) {
    const auditContext = await browser.newContext({
      viewport: { width: 1440, height: 900 }
    });
    await auditContext.addInitScript((th) => {
      localStorage.setItem("ks-theme", th);
      document.documentElement.dataset.theme = th;
    }, theme);
    const auditPage = await auditContext.newPage();

    for (const p of pagesToAudit) {
      await auditPage.goto(`${BASE}${p}`, { waitUntil: "networkidle" });
      await auditPage.waitForTimeout(100);

      const results = await auditPage.evaluate(async (axeSource) => {
        eval(axeSource);
        return await axe.run({
          runOnly: {
            type: "tag",
            values: ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"]
          }
        });
      }, axe.source);

      const seriousOrCritical = results.violations.filter(
        (v) => v.impact === "serious" || v.impact === "critical"
      );

      if (seriousOrCritical.length > 0) {
        console.error(`[A11y VIOLATION] ${p} (${theme}): ${seriousOrCritical.length} serious/critical issues`);
        for (const v of seriousOrCritical) {
          console.error(`  - [${v.impact}] ${v.id}: ${v.description} (${v.nodes.length} nodes)`);
          for (const node of v.nodes.slice(0, 2)) {
            console.error(`      target: ${node.target.join(", ")}; snippet: ${node.html}`);
          }
        }
        totalViolations += seriousOrCritical.length;
      } else {
        console.log(`[A11y PASS] ${p} (${theme}): 0 serious/critical violations`);
      }
    }
    await auditContext.close();
  }

  // 5. MULTI-LANGUAGE VERIFICATION
  console.log("\n--- Testing Multi-language Support (en, hi, pa) ---");
  for (const lang of ["en", "hi", "pa"]) {
    await page.goto(`${BASE}/?lang=${lang}`, { waitUntil: "networkidle" });
    const htmlLang = await page.evaluate(() => document.documentElement.lang);
    const h1Text = await page.locator(".copy h1").textContent();
    console.log(`Lang: ${lang} -> <html lang="${htmlLang}">, H1: "${h1Text.trim()}"`);
    if (htmlLang !== lang) {
      console.error(`[LANG MISMATCH] expected ${lang}, got ${htmlLang}`);
    }
  }

  // 6. KEYBOARD NAVIGATION WALK (60-second evaluator path)
  console.log("\n--- Testing Keyboard Walk ---");
  await page.goto(`${BASE}/`, { waitUntil: "networkidle" });
  await page.keyboard.press("Tab"); // Skip link
  const skipActive = await page.evaluate(() => document.activeElement?.className);
  console.log(`First tab active element class: "${skipActive}"`);

  // Tab through nav links
  for (let i = 0; i < 7; i++) {
    await page.keyboard.press("Tab");
  }
  const navActive = await page.evaluate(() => document.activeElement?.textContent?.trim());
  console.log(`Active element after tabs: "${navActive}"`);

  // 7. DEMO LOGIN & EVALUATOR PATH
  console.log("\n--- Testing Demo Login & Evaluator Path ---");
  await page.goto(`${BASE}/Account/Login?demo=true`, { waitUntil: "networkidle" });
  console.log(`Login page loaded`);

  const emailVal = await page.locator('#Email').inputValue();
  console.log(`Demo email prefilled: "${emailVal}"`);

  // Submit login
  await page.click('form button[type="submit"]');
  await page.waitForURL("**/Climate/Dashboard");
  console.log(`Login redirected to: ${page.url()}`);

  // Go to /Automation and add a rule
  await page.goto(`${BASE}/Automation`, { waitUntil: "networkidle" });
  const rulesBefore = await page.locator(".rule-item").count();
  console.log(`Rules count before add: ${rulesBefore}`);

  // Fill add rule form
  await page.locator('select[name="NewRule.ConditionVariable"]').selectOption("Temperature");
  await page.locator('select[name="NewRule.Operator"]').selectOption(">");
  await page.locator('input[name="NewRule.ThresholdValue"]').fill("32.5");
  // Select first actuator option if any
  const actSelect = page.locator('select[name="NewRule.ActuatorId"]');
  if (await actSelect.count() > 0) {
    const opts = await actSelect.locator("option").all();
    if (opts.length > 1) {
      const val = await opts[1].getAttribute("value");
      if (val) await actSelect.selectOption(val);
    }
  }
  await page.locator('input[name="NewRule.Description"]').fill("Evaluator test rule");
  await page.click('form.form-grid button[type="submit"]');

  await page.waitForLoadState("networkidle");
  const rulesAfter = await page.locator(".rule-item").count();
  console.log(`Rules count after add: ${rulesAfter}`);

  // Find the created rule and delete it
  const deleteBtn = page.locator('form[action*="DeleteRule"] button.btn-del').first();
  if (await deleteBtn.isVisible()) {
    await deleteBtn.click();
    await page.waitForLoadState("networkidle");
    const rulesFinal = await page.locator(".rule-item").count();
    console.log(`Rules count after delete: ${rulesFinal}`);
  }

  // 8. FINAL AUDIT RESULTS
  console.log("\n=== VERIFICATION SUMMARY ===");
  console.log(`Total A11y Serious/Critical Violations: ${totalViolations}`);
  console.log(`Console Errors: ${consoleErrors.length}`);
  if (consoleErrors.length > 0) {
    consoleErrors.forEach((e) => console.error(e));
  }
  console.log(`Total Network Requests: ${networkRequests.length}`);

  // Filter external non-allowed requests
  const externalLeaks = networkRequests.filter((url) => {
    const p = new URL(url);
    return (
      !p.hostname.includes("127.0.0.1") &&
      !p.hostname.includes("localhost") &&
      !p.hostname.includes("clerk") &&
      !p.hostname.includes("neon.tech")
    );
  });
  console.log(`External Third-Party Leaks: ${externalLeaks.length}`);
  if (externalLeaks.length > 0) {
    externalLeaks.forEach((l) => console.error(`Leak: ${l}`));
  }

  await browser.close();

  if (totalViolations === 0 && consoleErrors.length === 0 && externalLeaks.length === 0) {
    console.log("\n🎉 ALL CHECKS PASSED PERFECTLY!");
    process.exit(0);
  } else {
    console.error("\n❌ CHECKS HAD FAILURES.");
    process.exit(1);
  }
}

main().catch((err) => {
  console.error("FATAL ERROR:", err);
  process.exit(1);
});
