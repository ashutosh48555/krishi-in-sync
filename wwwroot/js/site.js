// Every page: toast auto-dismiss, account menu closes on outside click or Escape.
(function () {
  document.querySelectorAll("[data-toast]").forEach(function (t) { setTimeout(function () { t.hidden = true; }, 5000); });
  var um = document.querySelector("details.um");
  if (!um) return;
  document.addEventListener("click", function (e) { if (!um.contains(e.target)) um.open = false; });
  um.addEventListener("keydown", function (e) { if (e.key === "Escape") { um.open = false; um.querySelector("summary").focus(); } });
})();
