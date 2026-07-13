const target = 'http://localhost:5076';

function bypassPageNavigation(req) {
  if (req.headers.accept && req.headers.accept.indexOf('html') !== -1) {
    return '/index.html';
  }
}

module.exports = {
  '/auth': { target, secure: false },
  '/invitations': { target, secure: false },
  '/class-types': { target, secure: false },
  '/class-sessions': { target, secure: false },
  '/bookings': { target, secure: false },
  // '/schedule' is both a customer Angular route and a backend API path —
  // bypass to the SPA shell for real page navigations (e.g. refresh/deep-link),
  // proxy everything else (the XHR calls from ScheduleApiService) to the backend.
  '/schedule': { target, secure: false, bypass: bypassPageNavigation },
  '/instructors': { target, secure: false },
  '/health': { target, secure: false },
};
