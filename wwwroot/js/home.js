// Home: keeps the "Selected zone" card in step with the 3D scene, the zone buttons and the zones table.
// Data comes from #farm-data (server-rendered, real readings). DOM APIs only: textContent and hidden, never innerHTML.
(function () {
  const node = document.getElementById("farm-data"), card = document.getElementById("selcard");
  if (!node || !card) return;
  let D; try { D = JSON.parse(node.textContent); } catch (e) { return; }
  const byId = {}; D.zones.forEach((z) => { byId[z.id] = z; });
  const f = (n) => card.querySelector('[data-f="' + n + '"]');

  function show(key) {
    const z = byId[key]; if (!z) return;
    const d = z.d;
    f("title").textContent = d.title;
    f("temp").textContent = d.has ? d.temp : "—";
    f("hum").textContent = d.has ? d.hum : "—";
    f("vpd").textContent = d.has ? d.vpd : "—";
    f("act").textContent = d.act;
    f("actName").textContent = d.actName;
    f("sim").hidden = !d.sim;
    card.querySelectorAll("[data-u]").forEach((u) => { u.hidden = !d.has; });
    const note = f("note"); note.hidden = d.has; if (!d.has) note.textContent = D.labels.noReading;
  }

  document.addEventListener("ks:zoneselect", (e) => {
    show(e.detail.key);
    if (window.KrishiFarm) window.KrishiFarm.select(e.detail.key); // idempotent: keeps 3D and bar buttons in step
  });
  // 3D click or bar button -> table, charts and (through the event above) this card.
  if (window.KrishiFarm) window.KrishiFarm.onSelect((key) => { if (window.KrishiCharts) window.KrishiCharts.select(key); });
})();
