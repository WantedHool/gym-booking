const target = 'http://localhost:5076';

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
