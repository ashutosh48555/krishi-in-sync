// Zone charts (Chart.js 4, self-hosted) + zone selection + table toggle + VPD marker. Reads #chart-data. Exposes window.KrishiCharts.
// Selecting a zone (table row) restyles the charts and fires "ks:zoneselect" ({detail:{key}}); home.js listens.
(function () {
  const root = document.documentElement;
  const css = (n) => getComputedStyle(root).getPropertyValue(n).trim();
  const rows = () => document.querySelectorAll(".ztable tbody tr[data-zone]");
  const charts = [];
  let D = null, sel = "";

  // VPD marker: slides to its value on first load (no inline style, so the CSP stays strict).
  document.querySelectorAll(".pt[data-pos]").forEach((p) => {
    requestAnimationFrame(() => requestAnimationFrame(() => { p.style.left = p.dataset.pos + "%"; p.classList.add("ready"); }));
  });

  // "View data as table" under every chart.
  document.querySelectorAll("[data-table-toggle]").forEach((b) => {
    b.addEventListener("click", () => {
      const t = document.getElementById(b.dataset.tableToggle), open = t.hidden;
      t.hidden = !open; b.setAttribute("aria-expanded", String(open)); b.textContent = open ? b.dataset.hide : b.dataset.show;
    });
  });

  function markRows(key) {
    rows().forEach((r) => {
      const on = r.dataset.zone === key;
      r.classList.toggle("sel", on);
      const b = r.querySelector("button.zn"); if (b) b.setAttribute("aria-pressed", String(on));
    });
  }
  function select(key) {
    if (!key || key === sel) return;
    sel = key; markRows(key); restyle();
    document.dispatchEvent(new CustomEvent("ks:zoneselect", { detail: { key } }));
  }
  document.addEventListener("click", (e) => {
    const r = e.target.closest(".ztable tbody tr[data-zone]");
    if (r) select(r.dataset.zone);
  });

  const node = document.getElementById("chart-data");
  try { D = node && JSON.parse(node.textContent); } catch (e) { D = null; }
  if (D) sel = D.selected || (D.series[0] && D.series[0].key) || "";
  window.KrishiCharts = { select };
  if (!D || !window.Chart) return;

  const short = (name, w) => (w < 520 ? name.split(" ")[0] : name);
  const font = (size, wt) => (wt || 500) + " " + size + "px " + getComputedStyle(document.body).fontFamily;

  // Direct labels at the end of each line, nudged apart so they never overlap.
  const endLabels = {
    id: "endLabels",
    afterDatasetsDraw(chart) {
      const c = chart.ctx, w = chart.width, items = [];
      chart.data.datasets.forEach((ds, i) => {
        const pts = chart.getDatasetMeta(i).data;
        for (let k = pts.length - 1; k >= 0; k--) {
          if (ds.data[k] != null) { items.push({ x: pts[k].x, y: pts[k].y, t: short(ds.label, w), sel: ds.key === sel }); break; }
        }
      });
      items.sort((a, b) => a.y - b.y);
      for (let i = 1; i < items.length; i++) if (items[i].y - items[i - 1].y < 14) items[i].y = items[i - 1].y + 14;
      c.save(); c.textBaseline = "middle";
      items.forEach((it) => { c.font = font(12, it.sel ? 600 : 500); c.fillStyle = it.sel ? css("--accent") : css("--text-3"); c.fillText(it.t, it.x + 8, it.y); });
      c.restore();
    },
  };

  // Shaded optimal band on the VPD chart.
  const band = {
    id: "band",
    beforeDatasetsDraw(chart) {
      if (chart.canvas.dataset.chart !== "vpd") return;
      const { ctx: c, chartArea: a, scales: { y } } = chart, y0 = y.getPixelForValue(D.band[1]), y1 = y.getPixelForValue(D.band[0]);
      c.save(); c.fillStyle = css("--accent-soft"); c.fillRect(a.left, y0, a.right - a.left, y1 - y0);
      c.fillStyle = css("--text-2"); c.font = font(12, 500); c.textBaseline = "top"; c.fillText(D.text.band, a.left + 8, y0 + 5); c.restore();
    },
  };

  function dataset(s, kind) {
    const on = s.key === sel;
    return {
      key: s.key, label: s.name, data: kind === "vpd" ? s.vpd : s.temp, spanGaps: false, tension: 0.3,
      borderColor: on ? css("--accent") : css("--line-2"), borderWidth: on ? 2.5 : 1.75,
      backgroundColor: css("--accent-soft"), fill: on && kind === "temp" ? "origin" : false,
      pointRadius: 0, pointHoverRadius: 4, pointHitRadius: 14, order: on ? 0 : 1,
    };
  }
  function restyle() {
    charts.forEach(({ chart, kind }) => {
      chart.data.datasets = D.series.map((s) => dataset(s, kind));
      const o = chart.options;
      o.scales.x.ticks.color = o.scales.y.ticks.color = css("--text-3");
      o.scales.y.grid.color = css("--line"); o.scales.x.border.color = css("--line-2");
      Object.assign(o.plugins.tooltip, { backgroundColor: css("--surface"), titleColor: css("--text"), bodyColor: css("--text-2"), borderColor: css("--line-2") });
      chart.update("none");
    });
  }

  document.querySelectorAll("canvas[data-chart]").forEach((cv) => {
    const kind = cv.dataset.chart, vpd = kind === "vpd", fmt = (v) => (v == null ? "—" : v.toFixed(vpd ? 2 : 1));
    const chart = new Chart(cv, {
      type: "line",
      data: { labels: D.labels, datasets: D.series.map((s) => dataset(s, kind)) },
      plugins: [band, endLabels],
      options: {
        animation: false, responsive: true, maintainAspectRatio: false,
        layout: { padding: { right: innerWidth < 520 ? 74 : 128, top: 6 } },
        interaction: { mode: "index", intersect: false },
        plugins: {
          legend: { display: false },
          tooltip: { borderWidth: 1, padding: 10, boxPadding: 4, callbacks: { label: (c) => c.dataset.label + ": " + fmt(c.parsed.y) + (vpd ? " kPa" : " °C") } },
        },
        scales: {
          x: { grid: { display: false }, border: {}, ticks: { maxTicksLimit: 7, maxRotation: 0 } },
          y: vpd ? { min: 0, suggestedMax: 2, grid: {}, border: { display: false }, ticks: {} } : { grid: {}, border: { display: false }, ticks: { callback: (v) => v + "°" } },
        },
      },
    });
    charts.push({ chart, kind });
  });
  restyle();
  document.addEventListener("ks:themechange", restyle);
  markRows(sel);
})();
