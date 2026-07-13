const target = 'http://localhost:5076';

/** Browser refresh / deep-link: serve the SPA shell instead of proxying to the API. */
function bypassPageNavigation(req) {
  if (req.headers.accept?.includes('html')) {
    return '/index.html';
  }
}

function apiRoute(spaBypass = false) {
  return {
    target,
    secure: false,
    ...(spaBypass ? { bypass: bypassPageNavigation } : {}),
  };
}

module.exports = { target, bypassPageNavigation, apiRoute };
