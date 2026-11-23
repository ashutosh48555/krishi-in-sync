// Sky + time-of-day for the farm scene. window.KrishiSky(rig) -> { setHour, goTime, pos, hour }.
// rig: { stage, sun, hemi, fill, lamp, shadow, dirV, hourInput, hourLabel, discEl, tod: [night,dawn,day,dusk], reduce }
(function () {
  const SKY = [[0, "#050A1C", "#16233F"], [4.5, "#0B1530", "#26365C"], [6, "#4A68A8", "#F4A877"], [8, "#62A8EE", "#BFE0FB"], [12.5, "#3F94E6", "#B7DEFF"], [16.5, "#5B9FE3", "#F6DFB4"], [18.3, "#3E4F8F", "#F08A58"], [19.5, "#141F45", "#3B3F6E"], [21, "#070D24", "#1A2845"], [24, "#050A1C", "#16233F"]];
  const clamp = (x, lo, hi) => Math.min(hi, Math.max(lo, x));
  const ss = (a, b, x) => { const t = clamp((x - a) / (b - a), 0, 1); return t * t * (3 - 2 * t); };

  function skyAt(h) {
    let i = 0;
    while (i < SKY.length - 2 && h > SKY[i + 1][0]) i++;
    const a = SKY[i], b = SKY[i + 1], t = clamp((h - a[0]) / (b[0] - a[0]), 0, 1);
    return [new THREE.Color(a[1]).lerp(new THREE.Color(b[1]), t), new THREE.Color(a[2]).lerp(new THREE.Color(b[2]), t)];
  }

  window.KrishiSky = function (rig) {
    const st = rig.stage, set = (k, v) => st.style.setProperty(k, v);
    let hourV = 13, anim = 0, sunBase = 1, dayNow = true;

    function setHour(h) {
      h = ((h % 24) + 24) % 24; hourV = h;
      if (rig.hourInput) rig.hourInput.value = h;
      const nf = h >= 12 ? ss(17.5, 19.5, h) : 1 - ss(4.5, 6.5, h), isDay = h >= 5.5 && h <= 18.5;
      const a = isDay ? ((h - 6) / 12) * Math.PI : (((h < 12 ? h + 24 : h) - 18) / 12) * Math.PI, k = Math.max(0, Math.sin(a));
      rig.dirV.set(-Math.cos(a), Math.max(Math.sin(a), 0.05), -0.35).normalize();
      rig.sun.position.copy(rig.dirV).multiplyScalar(18);
      if (isDay) { rig.sun.color.set("#ff9a5a").lerp(new THREE.Color("#fff3dc"), Math.min(1, k * 2.5)); rig.sun.intensity = 0.15 + k; }
      else { rig.sun.color.set("#9db4ff"); rig.sun.intensity = 0.12 + 0.35 * k; }
      rig.hemi.intensity = 0.32 + 0.5 * (1 - nf);
      rig.hemi.color.set("#ffffff").lerp(new THREE.Color("#5d73b8"), nf);
      rig.hemi.groundColor.set("#c9c3ad").lerp(new THREE.Color("#10151f"), nf);
      rig.fill.intensity = 0.35 - 0.2 * nf; rig.lamp.intensity = 1.8 * nf; rig.shadow.material.opacity = 0.2 + 0.1 * nf;
      sunBase = k > 0 ? Math.min(1, k * 5) : 0; dayNow = isDay;
      const sk = skyAt(h); set("--sky-top", "#" + sk[0].getHexString()); set("--sky-bot", "#" + sk[1].getHexString());
      const warm = Math.max(Math.exp(-Math.pow((h - 6.3) / 1.1, 2)), Math.exp(-Math.pow((h - 18.2) / 1.1, 2)));
      set("--grade", "rgba(" + Math.round(255 - 235 * nf) + "," + Math.round(140 - 110 * nf) + "," + Math.round(60 + 20 * nf) + "," + (warm * 0.4 + nf * 0.3).toFixed(3) + ")");
      set("--star-o", nf.toFixed(3)); set("--cloud-o", (0.75 - 0.6 * nf).toFixed(3)); set("--cb", (1 - 0.65 * nf).toFixed(3));
      if (rig.discEl) rig.discEl.classList.toggle("moon", !isDay);
      const idx = h < 5 || h >= 20 ? 0 : h < 8 ? 1 : h < 17 ? 2 : 3, hh = Math.floor(h), mm = Math.round((h - hh) * 60);
      if (rig.hourLabel) rig.hourLabel.textContent = String(hh).padStart(2, "0") + ":" + String(mm).padStart(2, "0") + " · " + rig.tod[idx];
      st.dataset.hour = h.toFixed(2);
    }

    // Sun/moon disc follows the light direction relative to the camera angle.
    function pos(theta) {
      let d = Math.atan2(rig.dirV.x, rig.dirV.z) - (theta + Math.PI); d = Math.atan2(Math.sin(d), Math.cos(d));
      const e = Math.asin(rig.dirV.y / rig.dirV.length());
      set("--sx", clamp(50 + Math.sin(d) * 58, 12, 88) + "%"); set("--sy", Math.max(14, 62 - (e / (Math.PI / 2)) * 54) + "%");
      const vis = sunBase * (1 - ss(1.1, 1.5, Math.abs(d)));
      set("--sun-o", vis.toFixed(3)); set("--ray-o", (dayNow ? vis * 0.5 : 0).toFixed(3));
    }

    function goTime(target) {
      cancelAnimationFrame(anim);
      const from = hourV, delta = (((target - from) % 24) + 24) % 24;
      if (rig.reduce || delta < 0.01) { setHour(target); return; }
      const dur = Math.min(2600, Math.max(900, delta * 240)), t1 = performance.now();
      (function stp(n) {
        const p = Math.min(1, (n - t1) / dur), e = p < 0.5 ? 2 * p * p : 1 - Math.pow(-2 * p + 2, 2) / 2;
        setHour(from + delta * e); if (p < 1) anim = requestAnimationFrame(stp);
      })(t1);
    }
    return { setHour, goTime, pos, stop: () => cancelAnimationFrame(anim), hour: () => hourV };
  };
})();
