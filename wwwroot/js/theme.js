// Theme toggle with View Transitions circular reveal. Emits "ks:themechange" ({detail:{theme}}) on document.
(function () {
  const root = document.documentElement;
  const reduce = matchMedia("(prefers-reduced-motion: reduce)").matches;
  const isDark = () => root.dataset.theme === "dark";

  function burst(x, y, toDark) {
    const cols = toDark ? ["#A9C1FF", "#FFFFFF", "#7DB2F2"] : ["#FFC54D", "#FFE08A", "#FF9A5A"], n = 18;
    const fx = (el, css) => { Object.assign(el.style, css); document.body.appendChild(el); return el; };
    for (let i = 0; i < n; i++) {
      const a = (i / n) * Math.PI * 2 + Math.random() * 0.3, dist = 70 + Math.random() * 110, sz = 3 + Math.random() * 4, c = cols[i % 3];
      const el = fx(document.createElement("i"), { position: "fixed", left: x + "px", top: y + "px", width: sz + "px", height: sz + "px", borderRadius: "50%", pointerEvents: "none", zIndex: "9999", background: c, boxShadow: "0 0 10px " + c });
      el.animate([{ transform: "translate(-50%,-50%) scale(1)", opacity: 1 }, { transform: "translate(calc(-50% + " + Math.cos(a) * dist + "px),calc(-50% + " + Math.sin(a) * dist + "px)) scale(0)", opacity: 0 }], { duration: 700 + Math.random() * 400, easing: "cubic-bezier(.2,.8,.2,1)" }).onfinish = () => el.remove();
    }
    const c = toDark ? "#A9C1FF" : "#FFB347";
    const ring = fx(document.createElement("i"), { position: "fixed", left: x + "px", top: y + "px", width: "36px", height: "36px", margin: "-18px 0 0 -18px", borderRadius: "50%", pointerEvents: "none", zIndex: "9999", border: "2px solid " + c, boxShadow: "0 0 24px " + c });
    ring.animate([{ transform: "scale(.6)", opacity: 0.9 }, { transform: "scale(6)", opacity: 0 }], { duration: 900, easing: "ease-out" }).onfinish = () => ring.remove();
  }

  function setTheme(next, x, y) {
    const apply = () => {
      root.dataset.theme = next;
      try { localStorage.setItem("ks-theme", next); } catch (e) { /* storage blocked */ }
      document.dispatchEvent(new CustomEvent("ks:themechange", { detail: { theme: next } }));
    };
    if (!document.startViewTransition || reduce) { apply(); return; }
    const r = Math.hypot(Math.max(x, innerWidth - x), Math.max(y, innerHeight - y)) + 160, ease = "cubic-bezier(.65,0,.25,1)";
    root.style.setProperty("--cx", x + "px"); root.style.setProperty("--cy", y + "px");
    burst(x, y, next === "dark");
    document.startViewTransition(apply).ready.then(() => {
      root.animate({ "--r": ["0px", r + "px"] }, { duration: 1100, easing: ease, pseudoElement: "::view-transition-new(root)" });
      root.animate([{ filter: "blur(0px)", transform: "scale(1)" }, { filter: "blur(6px)", transform: "scale(.985)" }], { duration: 1100, easing: ease, fill: "forwards", pseudoElement: "::view-transition-old(root)" });
    }).catch(() => {});
  }

  document.addEventListener("click", (e) => {
    const btn = e.target.closest("#theme, [data-theme-toggle]");
    if (!btn) return;
    const b = btn.getBoundingClientRect();
    setTheme(isDark() ? "light" : "dark", b.left + b.width / 2, b.top + b.height / 2);
  });
  window.KrishiTheme = { set: setTheme, isDark };
})();
