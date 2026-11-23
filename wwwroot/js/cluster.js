// Farm cluster coordinate presets
(function () {
  function applyPreset(btn) {
    var lat = btn.dataset.lat;
    var lon = btn.dataset.lon;
    var elLat = document.getElementById("f-lat");
    var elLon = document.getElementById("f-lon");
    if (elLat && lat) elLat.value = lat;
    if (elLon && lon) elLon.value = lon;
  }
  document.addEventListener("click", function (e) {
    var btn = e.target.closest(".btn-preset[data-lat]");
    if (btn) applyPreset(btn);
  });
})();
