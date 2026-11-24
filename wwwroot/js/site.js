// Krishi Sync core interactive logic: dialogs, toasts, form loading states, search, presets & print.
(function () {
  "use strict";

  // 1. Modals & Dialogs (Accessible HTML5 Dialogs)
  document.querySelectorAll("[data-open-dialog]").forEach(function (btn) {
    btn.addEventListener("click", function () {
      var d = document.getElementById(btn.dataset.openDialog);
      if (d && typeof d.showModal === "function") d.showModal();
    });
  });

  document.querySelectorAll("[data-close-dialog]").forEach(function (btn) {
    btn.addEventListener("click", function () {
      var d = btn.closest("dialog");
      if (d && typeof d.close === "function") d.close();
    });
  });

  document.querySelectorAll("dialog").forEach(function (d) {
    d.addEventListener("click", function (e) {
      if (e.target === d && typeof d.close === "function") d.close();
    });
  });

  // 2. Toast notifications with countdown & dismiss
  document.querySelectorAll("[data-toast]").forEach(function (toast) {
    var timer = setTimeout(function () { toast.hidden = true; }, 5000);
    toast.addEventListener("mouseenter", function () { clearTimeout(timer); });
    toast.addEventListener("mouseleave", function () {
      timer = setTimeout(function () { toast.hidden = true; }, 3000);
    });
    var closeBtn = toast.querySelector("[data-close-toast]");
    if (closeBtn) {
      closeBtn.addEventListener("click", function () {
        clearTimeout(timer);
        toast.hidden = true;
      });
    }
  });

  // 3. Form submit confirmation & loading spinners
  document.addEventListener("submit", function (e) {
    var f = e.target;
    var q = f && f.dataset && f.dataset.confirm;
    if (q && !window.confirm(q)) {
      e.preventDefault();
      return;
    }
    var btn = f.querySelector('button[type="submit"]:not(.is-loading)');
    if (btn) {
      var loadText = btn.dataset.loadingText;
      if (loadText) {
        btn.innerHTML = '<span class="btn-spinner" aria-hidden="true"></span> ' + loadText;
      } else {
        btn.classList.add("is-loading");
      }
    }
  });

  // 4. Alert list real-time instant search
  var searchInput = document.getElementById("alert-search");
  if (searchInput) {
    var countBadge = document.getElementById("search-count");
    var noMatch = document.getElementById("alert-no-match");
    var items = document.querySelectorAll(".listcard article.al");
    searchInput.addEventListener("input", function () {
      var query = searchInput.value.trim().toLowerCase();
      var matches = 0;
      items.forEach(function (it) {
        var text = (it.dataset.alertText || it.textContent).toLowerCase();
        var isMatch = !query || text.indexOf(query) !== -1;
        it.hidden = !isMatch;
        if (isMatch) matches++;
      });
      if (noMatch) noMatch.hidden = matches > 0 || !query;
      if (countBadge) countBadge.textContent = query ? matches + " matching" : "";
    });
  }

  // 5. Automation Rule Presets
  document.querySelectorAll("[data-preset-metric]").forEach(function (btn) {
    btn.addEventListener("click", function () {
      var metric = document.querySelector('select[name="NewRule.ConditionVariable"]');
      var op = document.querySelector('select[name="NewRule.Operator"]');
      var val = document.querySelector('input[name="NewRule.ThresholdValue"]');
      var note = document.querySelector('input[name="NewRule.Description"]');
      var onRadio = document.querySelector('input[name="NewRule.TurnOn"][value="' + btn.dataset.presetOn + '"]');
      if (metric) metric.value = btn.dataset.presetMetric;
      if (op) op.value = btn.dataset.presetOp;
      if (val) val.value = btn.dataset.presetVal;
      if (note) note.value = btn.dataset.presetNote;
      if (onRadio) onRadio.checked = true;

      var act = document.querySelector('select[name="NewRule.ActuatorId"]');
      if (act && btn.dataset.presetAct) {
        for (var i = 0; i < act.options.length; i++) {
          if (act.options[i].text.toLowerCase().indexOf(btn.dataset.presetAct.toLowerCase()) !== -1) {
            act.selectedIndex = i;
            break;
          }
        }
      }
    });
  });

  // 6. Print Report Action
  document.querySelectorAll('[data-action="print"]').forEach(function (b) {
    b.addEventListener("click", function () { window.print(); });
  });

  // 7. VPD Marker Slider Initialization across all views
  function initVpdSliders() {
    document.querySelectorAll(".pt[data-pos]").forEach(function (p) {
      var pos = p.getAttribute("data-pos");
      if (pos) {
        requestAnimationFrame(function () {
          p.style.left = pos + "%";
          p.classList.add("ready");
        });
      }
    });
  }
  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", initVpdSliders);
  } else {
    initVpdSliders();
  }

  // 8. Account menu dismiss
  var um = document.querySelector("details.um");
  if (um) {
    document.addEventListener("click", function (e) { if (!um.contains(e.target)) um.open = false; });
    um.addEventListener("keydown", function (e) {
      if (e.key === "Escape") { um.open = false; um.querySelector("summary").focus(); }
    });
  }

  // 9. Quick Demo Credentials Filler
  document.querySelectorAll("[data-fill-email]").forEach(function (btn) {
    btn.addEventListener("click", function () {
      var email = btn.getAttribute("data-fill-email");
      var pass = btn.getAttribute("data-fill-pass");
      var emailInput = document.getElementById("Email");
      var passInput = document.getElementById("Password");
      if (emailInput && email) emailInput.value = email;
      if (passInput && pass) passInput.value = pass;
    });
  });
})();
