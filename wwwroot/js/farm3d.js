// Krishi farm diorama (three.js r128). Reads #farm-data JSON; exposes window.KrishiFarm.
// JSON: { lang, selected?, labels?:{tod:[4]}, zones:[{id,name:{en,hi,pa},temp,humidity,vpd,target,status,offTarget}] }
// Illustration only: zone readings come from the server, fan and drops are simulated.
(function () {
  const X = { poly: -4.7, open: 0, drip: 4.7 }; // scene geometry: plot centres
  const reduce = matchMedia("(prefers-reduced-motion: reduce)").matches;
  const el = (tag, cls, text) => { const e = document.createElement(tag); if (cls) e.className = cls; if (text) e.textContent = text; return e; };
  let inited = false, data, stage, renderer, scene, camera, sky, fan, polyOff = false, sel = "";
  let theta = 0.62, drag = null, moved = 0, active = true, raf = 0, t0 = 0;
  const plots = {}, hi = {}, pins = {}, drops = [], cbs = [];

  const mk = (geo, color, o) => {
    const m = new THREE.Mesh(geo, new THREE.MeshStandardMaterial(Object.assign({ color: color, roughness: 0.85, metalness: 0 }, o || {})));
    m.castShadow = true; m.receiveShadow = true; return m;
  };
  function crops(g, xs, span, n, y, h, cols) {
    const im = new THREE.InstancedMesh(new THREE.ConeGeometry(0.085, h, 5), new THREE.MeshStandardMaterial({ roughness: 0.8 }), xs.length * n);
    im.castShadow = true; im.receiveShadow = true;
    const m = new THREE.Matrix4(), c = new THREE.Color(); let k = 0;
    xs.forEach((x, ri) => {
      for (let j = 0; j < n; j++) {
        const sc = 0.8 + Math.random() * 0.5;
        m.compose(new THREE.Vector3(x + (Math.random() - 0.5) * 0.05, y + (h * sc) / 2, -span / 2 + (span * (j + 0.5)) / n + (Math.random() - 0.5) * 0.06), new THREE.Quaternion(), new THREE.Vector3(1, sc, 1));
        im.setMatrixAt(k, m); c.setHex(cols[(ri + j) % cols.length]); im.setColorAt(k, c); k++;
      }
    });
    g.add(im);
  }
  function tree(x, z, s) {
    const t = new THREE.Group(), tr = mk(new THREE.CylinderGeometry(0.07, 0.1, 0.5, 6), 0x6b4e33); tr.position.y = 0.25;
    const c1 = mk(new THREE.ConeGeometry(0.42, 0.8, 7), 0x3f8f57); c1.position.y = 0.85;
    const c2 = mk(new THREE.ConeGeometry(0.3, 0.6, 7), 0x4fa066); c2.position.y = 1.3;
    t.add(tr, c1, c2); t.position.set(x, 0, z); t.scale.setScalar(s); scene.add(t);
  }
  const ridge = (g, x, w) => { const r = mk(new THREE.BoxGeometry(w, 0.12, 5.9), 0x4a3322); r.position.set(x, 0.2, 0); g.add(r); };

  function plot(id) {
    const g = new THREE.Group(); g.position.set(X[id], 0, 0); g.userData.zone = id; plots[id] = g; scene.add(g);
    const soil = mk(new THREE.BoxGeometry(4.3, 0.14, 6.5), 0x654932); soil.position.y = 0.07; g.add(soil);
    const edge = new THREE.LineSegments(new THREE.EdgesGeometry(new THREE.BoxGeometry(4.5, 0.16, 6.7)), new THREE.LineBasicMaterial({ color: 0xe3a400 }));
    edge.position.y = 0.08; edge.visible = false; g.add(edge); hi[id] = edge;
    if (id === "open") {
      const xs = [...Array(10)].map((_, i) => -1.7 + i * 0.378);
      xs.forEach((x) => ridge(g, x, 0.2));
      crops(g, xs, 5.7, 20, 0.26, 0.46, [0x4f9a60, 0x5ba66b, 0x6bb076]);
    } else if (id === "drip") {
      const xs = [...Array(8)].map((_, i) => -1.6 + i * 0.46);
      xs.forEach((x) => {
        ridge(g, x, 0.22);
        const l = mk(new THREE.CylinderGeometry(0.022, 0.022, 5.9, 8), 0x2f86d6); l.rotation.x = Math.PI / 2; l.position.set(x + 0.15, 0.2, 0); g.add(l);
        for (let k = 0; k < 3; k++) {
          const d = mk(new THREE.SphereGeometry(0.06, 8, 8), 0x7ec0ff, { emissive: 0x2f86d6, emissiveIntensity: 0.5 });
          d.position.set(x + 0.15, 0.2, -3 + k * 2); d.userData.o = k * 2; g.add(d); drops.push(d);
        }
      });
      crops(g, xs, 5.7, 18, 0.26, 0.36, [0x7db36a, 0x6fa85c, 0x8cc07a]);
      const pipe = mk(new THREE.CylinderGeometry(0.05, 0.05, 3.7, 10), 0x2f86d6); pipe.rotation.z = Math.PI / 2; pipe.position.set(-0.05, 0.22, -3.3); g.add(pipe);
      const v = mk(new THREE.BoxGeometry(0.5, 0.4, 0.5), 0x245ba8); v.position.set(-2.05, 0.3, -3.3); g.add(v);
    } else {
      const skin = new THREE.Mesh(new THREE.CylinderGeometry(1.95, 1.95, 5.8, 32, 1, true, Math.PI / 2, Math.PI), new THREE.MeshPhysicalMaterial({ color: 0xffffff, transparent: true, opacity: 0.24, roughness: 0.1, side: THREE.DoubleSide, depthWrite: false }));
      skin.rotation.x = Math.PI / 2; skin.position.y = 0.14; g.add(skin);
      for (let i = 0; i < 7; i++) { const rb = mk(new THREE.TorusGeometry(1.95, 0.04, 8, 32, Math.PI), 0xf4f6f8, { metalness: 0.3 }); rb.position.set(0, 0.14, -2.7 + i * 0.9); g.add(rb); }
      for (let r = 0; r < 3; r++) for (let i = 0; i < 8; i++) { const p = mk(new THREE.ConeGeometry(0.17, 0.42, 7), 0x3f9a58); p.position.set(-1.2 + r * 1.2, 0.4, -2.6 + i * 0.74); g.add(p); }
      fan = new THREE.Group(); fan.position.set(0, 1.05, 3.0); g.add(fan);
      for (let b = 0; b < 3; b++) { const bl = mk(new THREE.BoxGeometry(0.12, 0.62, 0.03), 0x1b2559); bl.position.y = 0.31; const pv = new THREE.Group(); pv.rotation.z = (b * Math.PI * 2) / 3; pv.add(bl); fan.add(pv); }
      const hub = mk(new THREE.CylinderGeometry(0.1, 0.1, 0.08, 16), 0xe3a400); hub.rotation.x = Math.PI / 2; fan.add(hub);
    }
  }

  function build() {
    renderer = new THREE.WebGLRenderer({ canvas: stage.querySelector("canvas"), antialias: true, alpha: true });
    renderer.setPixelRatio(Math.min(devicePixelRatio, 2));
    renderer.shadowMap.enabled = true; renderer.shadowMap.type = THREE.PCFSoftShadowMap;
    scene = new THREE.Scene(); camera = new THREE.PerspectiveCamera(35, 1, 0.1, 100);
    const hemi = new THREE.HemisphereLight(0xffffff, 0xc9c3ad, 0.8); scene.add(hemi);
    const sun = new THREE.DirectionalLight(0xfff1d6, 1), map = matchMedia("(pointer:coarse)").matches ? 1024 : 2048;
    sun.castShadow = true; sun.shadow.mapSize.set(map, map);
    Object.assign(sun.shadow.camera, { left: -12, right: 12, top: 9, bottom: -9, near: 1, far: 45 });
    sun.shadow.bias = -0.0006; sun.shadow.normalBias = 0.02; scene.add(sun);
    const fill = new THREE.DirectionalLight(0xffffff, 0.35); fill.position.set(2, 7, 14); scene.add(fill);
    const lamp = new THREE.PointLight(0xffd29a, 0, 9); lamp.position.set(X.poly, 1.3, 0); scene.add(lamp);
    const slab1 = mk(new THREE.BoxGeometry(15.6, 0.3, 7.8), 0xe8e3d3); slab1.position.y = -0.15; scene.add(slab1);
    const slab2 = mk(new THREE.BoxGeometry(15, 0.35, 7.2), 0xbdb5a0); slab2.position.y = -0.475; scene.add(slab2);
    const shadow = new THREE.Mesh(new THREE.PlaneGeometry(40, 30), new THREE.ShadowMaterial({ opacity: 0.2 }));
    shadow.rotation.x = -Math.PI / 2; shadow.position.y = -0.7; shadow.receiveShadow = true; scene.add(shadow);
    [[-7.35, -3], [-7.35, 3], [7.35, -3], [7.35, 3]].forEach((p, i) => tree(p[0], p[1], 0.7 + (i % 2) * 0.15));
    data.zones.forEach((z) => { if (z.id in X) plot(z.id); });
    sky = window.KrishiSky({ stage, sun, hemi, fill, lamp, shadow, dirV: new THREE.Vector3(0, 1, -0.35), hourInput: stage.querySelector("#hour"), hourLabel: stage.querySelector("#hl"), discEl: stage.querySelector("#sd"), tod: (data.labels && data.labels.tod) || ["Night", "Dawn", "Day", "Dusk"], reduce });
  }

  function buildUi() {
    const lang = data.lang || "en", name = (z) => (z.name && (z.name[lang] || z.name.en)) || z.id;
    const stars = stage.querySelector("#stars");
    if (stars) for (let i = 0; i < 70; i++) {
      const s = el("i", "star"); s.style.left = Math.random() * 100 + "%"; s.style.top = Math.random() * 55 + "%";
      s.style.animationDelay = "-" + Math.random() * 3 + "s"; s.style.opacity = (0.4 + Math.random() * 0.6).toFixed(2); stars.appendChild(s);
    }
    const zbs = stage.querySelector("#zbs");
    data.zones.forEach((z) => {
      const pin = el("div", "pin"); pin.appendChild(el("i")); pin.appendChild(document.createTextNode(name(z))); stage.appendChild(pin); pins[z.id] = pin;
      if (zbs) { const b = el("button", "zb", name(z)); b.type = "button"; b.dataset.z = z.id; b.addEventListener("click", () => select(z.id)); zbs.appendChild(b); }
    });
  }

  function resize() {
    const w = stage.clientWidth, h = stage.clientHeight; if (!w || !h) return;
    renderer.setSize(w, h, false); camera.aspect = w / h; camera.updateProjectionMatrix();
  }
  function pick(e) {
    const r = stage.getBoundingClientRect(), m = new THREE.Vector2(((e.clientX - r.left) / r.width) * 2 - 1, -((e.clientY - r.top) / r.height) * 2 + 1);
    const rc = new THREE.Raycaster(); rc.setFromCamera(m, camera);
    const hit = rc.intersectObjects(Object.values(plots), true)[0];
    if (hit) { let o = hit.object; while (o && !o.userData.zone) o = o.parent; if (o) select(o.userData.zone); }
  }
  function updatePins() {
    data.zones.forEach((z) => {
      const pin = pins[z.id]; if (!pin || !(z.id in X)) return;
      const p = new THREE.Vector3(X[z.id], z.id === "poly" ? 2.5 : 1.2, 0).project(camera);
      pin.style.left = ((p.x + 1) / 2) * stage.clientWidth + "px"; pin.style.top = ((1 - p.y) / 2) * stage.clientHeight + "px";
      pin.classList.toggle("sel", z.id === sel);
    });
  }
  function select(id) {
    if (!data || !data.zones.some((z) => z.id === id)) return;
    sel = id;
    Object.keys(hi).forEach((k) => { hi[k].visible = k === id; });
    stage.querySelectorAll(".zb").forEach((b) => b.setAttribute("aria-pressed", String(b.dataset.z === id)));
    cbs.forEach((f) => f(id));
  }

  function frame(t) {
    if (!active) { raf = 0; return; }
    raf = requestAnimationFrame(frame);
    const s = (t - t0) / 1000;
    if (!drag && !reduce) theta += Math.sin(s * 0.25) * 0.0012;
    const r = Math.max(18.5, 17 / (0.63 * camera.aspect)), e = 0.5;
    camera.position.set(Math.sin(theta) * Math.cos(e) * r, Math.sin(e) * r, Math.cos(theta) * Math.cos(e) * r);
    camera.lookAt(0, 0.9, 0);
    Object.keys(plots).forEach((k) => { const g = plots[k]; g.position.y += ((k === sel ? 0.22 : 0) - g.position.y) * 0.12; });
    if (!reduce) { if (fan && polyOff) fan.rotation.z -= 0.18; drops.forEach((d) => { d.position.z = -3 + ((s * 0.9 + d.userData.o) % 6); }); }
    sky.pos(theta); renderer.render(scene, camera); updatePins();
  }
  function wake() { if (active && !raf && renderer) raf = requestAnimationFrame(frame); }

  function init() {
    if (inited) return; inited = true;
    const node = document.getElementById("farm-data"); stage = document.getElementById("stage");
    if (!node || !stage) return;
    try { data = JSON.parse(node.textContent); } catch (e) { stage.classList.add("nogl"); return; }
    const z0 = data.zones.find((z) => z.id === "poly"); polyOff = !!(z0 && z0.offTarget);
    buildUi();
    try { build(); } catch (e) { stage.classList.add("nogl"); renderer = null; return; }
    new ResizeObserver(resize).observe(stage); resize();
    stage.addEventListener("pointerdown", (e) => { drag = { x: e.clientX, t: theta }; moved = 0; stage.setPointerCapture(e.pointerId); });
    stage.addEventListener("pointermove", (e) => { if (!drag) return; const dx = e.clientX - drag.x; moved = Math.max(moved, Math.abs(dx)); theta = drag.t - dx * 0.006; });
    stage.addEventListener("pointerup", (e) => { if (drag && moved < 5) pick(e); drag = null; });
    const bar = stage.querySelector(".bar"); if (bar) bar.addEventListener("pointerdown", (e) => e.stopPropagation());
    const hour = stage.querySelector("#hour"); if (hour) hour.addEventListener("input", (e) => { sky.stop(); sky.setHour(+e.target.value); });
    document.addEventListener("ks:themechange", (e) => sky.goTime(e.detail.theme === "dark" ? 21.5 : 13));
    new IntersectionObserver((en) => { active = en[0].isIntersecting && !document.hidden; wake(); }).observe(stage);
    document.addEventListener("visibilitychange", () => { active = !document.hidden; wake(); });
    sky.setHour(document.documentElement.dataset.theme === "dark" ? 21.5 : 13);
    select(data.selected || data.zones[0].id);
    t0 = performance.now(); wake();
  }

  window.KrishiFarm = {
    init, select,
    setHour: (h) => sky && sky.setHour(h),
    goTime: (h) => sky && sky.goTime(h),
    onSelect: (f) => { cbs.push(f); },
  };
  if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", init); else init();
})();
