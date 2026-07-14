const { apiRoute } = require('../../proxy.shared');

module.exports = {
  '/auth': apiRoute(),
  '/invitations': apiRoute(true),
  '/class-types': apiRoute(true),
  '/class-sessions': apiRoute(),
  '/bookings': apiRoute(),
  '/schedule': apiRoute(true),
  '/instructors': apiRoute(),
  '/membership-plans': apiRoute(),
  '/subscriptions': apiRoute(),
  '/users': apiRoute(true),
  '/tenant': apiRoute(true),
  '/sessions': apiRoute(true),
  '/health': apiRoute(),
};
