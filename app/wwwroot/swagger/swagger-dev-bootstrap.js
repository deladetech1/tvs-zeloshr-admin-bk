/* Development only — prefills Trove headers and Bearer JWT from /swagger/dev-bootstrap.json */
(function () {
  function buildHeaders(data) {
    return {
      appId: data.appId,
      orgId: data.orgId,
      busId: data.busId,
      locId: data.locId,
      authorization: data.authorization,
      bearerToken: data.bearerToken,
    };
  }

  function authorizeBearer(token) {
    if (!token || !window.ui || !window.ui.authActions) return;
    try {
      window.ui.authActions.authorize({
        Bearer: {
          name: "Bearer",
          schema: { type: "http", scheme: "bearer", bearerFormat: "JWT" },
          value: token,
        },
      });
    } catch (_) {
      /* Swagger UI version differences */
    }
  }

  function fillParameterInputs(h) {
    if (!h) return;
    var map = {
      "app-id": h.appId,
      "bus-id": h.busId,
      "loc-id": h.locId,
      "org-id": h.orgId,
      authorization: h.authorization,
    };
    Object.keys(map).forEach(function (name) {
      var val = map[name];
      if (!val) return;
      document
        .querySelectorAll(
          'tr[data-param-name="' +
            name +
            '"] input, input[data-param-name="' +
            name +
            '"]'
        )
        .forEach(function (el) {
          el.value = val;
          el.dispatchEvent(new Event("input", { bubbles: true }));
          el.dispatchEvent(new Event("change", { bubbles: true }));
        });
    });
  }

  function whenUiReady(fn) {
    var attempts = 0;
    var timer = setInterval(function () {
      attempts += 1;
      if (window.ui) {
        clearInterval(timer);
        fn();
      } else if (attempts > 60) {
        clearInterval(timer);
      }
    }, 150);
  }

  function applyBootstrap(data) {
    var h = buildHeaders(data);
    window.__zeloshrSwaggerHeaders = h;
    whenUiReady(function () {
      authorizeBearer(h.bearerToken);
      fillParameterInputs(h);
    });
    document.addEventListener(
      "click",
      function (ev) {
        var t = ev.target;
        if (!t || !t.classList) return;
        if (
          t.classList.contains("try-out__btn") ||
          t.classList.contains("opblock-summary-control")
        ) {
          setTimeout(function () {
            fillParameterInputs(window.__zeloshrSwaggerHeaders);
          }, 150);
        }
      },
      true
    );
  }

  fetch("/swagger/dev-bootstrap.json", { credentials: "same-origin" })
    .then(function (r) {
      if (!r.ok) throw new Error("bootstrap unavailable");
      return r.json();
    })
    .then(applyBootstrap)
    .catch(function (err) {
      console.warn("[ZelosHR Swagger] Could not load dev bootstrap:", err);
    });
})();
