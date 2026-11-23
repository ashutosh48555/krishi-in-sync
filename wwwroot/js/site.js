// Every page: toast auto-dismiss, account menu closes on outside click or Escape, data-confirm forms.
(function () {
  document.querySelectorAll("[data-toast]").forEach(function (t) { setTimeout(function () { t.hidden = true; }, 5000); });
  document.addEventListener("submit", function (e) {
    var q = e.target && e.target.dataset && e.target.dataset.confirm;
    if (q && !window.confirm(q)) e.preventDefault();
  });
  var um = document.querySelector("details.um");
  if (!um) return;
  document.addEventListener("click", function (e) { if (!um.contains(e.target)) um.open = false; });
  um.addEventListener("keydown", function (e) { if (e.key === "Escape") { um.open = false; um.querySelector("summary").focus(); } });
})();
