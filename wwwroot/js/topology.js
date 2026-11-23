// Zones page: the topology list links to a zone card; move focus there so keyboard and screen-reader users land on it.
(function () {
  function focusCard() {
    const el =
      location.hash.length > 1 &&
      document.getElementById(location.hash.slice(1));
    if (el && el.classList.contains("zc")) el.focus({ preventScroll: false });
  }
  window.addEventListener("hashchange", focusCard);
  focusCard();
})();
