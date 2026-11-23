// History page: when every zone is drawn, give each its fixed series colour (--s1 Open Field, --s2 Polyhouse, --s3 Drip).
// Runs after charts.js, which draws the charts and restyles them on theme change.
(function () {
  const node = document.getElementById("chart-data");
  if (!node || !window.Chart) return;
  let D;
  try {
    D = JSON.parse(node.textContent);
  } catch (e) {
    return;
  }
  if (D.selected !== "all") return;
  const token = { open: "--s1", poly: "--s2", drip: "--s3" };
  const css = (n) =>
    getComputedStyle(document.documentElement).getPropertyValue(n).trim();
  function paint() {
    document.querySelectorAll("canvas[data-chart]").forEach((cv) => {
      const chart = Chart.getChart(cv);
      if (!chart) return;
      chart.data.datasets.forEach((ds) => {
        ds.borderColor = css(token[ds.key] || "--s1");
        ds.borderWidth = 2;
      });
      chart.update("none");
    });
  }
  paint();
  document.addEventListener("ks:themechange", paint);
})();
