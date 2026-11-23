// Farm cluster coordinate presets
window.setLoc = function(lat, lon) {
  var elLat = document.getElementById('f-lat');
  var elLon = document.getElementById('f-lon');
  if (elLat) elLat.value = lat;
  if (elLon) elLon.value = lon;
};
