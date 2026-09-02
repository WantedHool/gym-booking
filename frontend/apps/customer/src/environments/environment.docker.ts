// Build configuration για το τοπικό full-stack demo (docker-compose.full.yml).
// Το API τρέχει σε container δημοσιευμένο στο host port 8080 — ο browser του χρήστη το
// βρίσκει στο http://localhost:8080 (CORS). Ξεχωριστό από το production (Render URL).
export const environment = {
  production: true,
  apiBaseUrl: 'http://localhost:8080',
};
