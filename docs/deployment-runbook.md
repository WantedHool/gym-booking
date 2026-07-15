# Deployment Runbook — Φάση 8 (Μέρος A)

> Πρακτικός οδηγός για το demo/test deployment. Δεν είναι production-grade infra (free tiers, demo credentials) — μόνο για παρουσίαση/tests.

## URLs

| Service | URL |
|---|---|
| Customer app | https://gym-booking-2.onrender.com |
| Staff app | https://gym-booking-1.onrender.com |
| API | https://gym-booking-0e32.onrender.com |
| API health check | https://gym-booking-0e32.onrender.com/health |
| API docs (Scalar) | *(απενεργοποιημένο σε Production — μόνο Development)* |
| Neon Postgres | project στο neon.tech, region eu-central-1 (Frankfurt) |

## Demo credentials

Όλοι οι demo χρήστες ανήκουν στο tenant **Demo Gym** (slug `demo-gym`), seeded αυτόματα στο startup του API (`DbSeeder` + `DemoDataSeeder`).

| Ρόλος | Email | Password |
|---|---|---|
| Admin | admin@demo.gym | `Admin123!` |
| Instructor | instructor@demo.gym | `Demo1234!` |
| Instructor | instructor2@demo.gym | `Demo1234!` |
| Member | member@demo.gym | `Demo1234!` |
| Member | member2@demo.gym | `Demo1234!` |
| Member | member3@demo.gym | `Demo1234!` |

Demo δεδομένα (από `DemoDataSeeder`): 2 class types (Yoga, CrossFit), 4 μελλοντικά sessions, 2 membership plans (Απεριόριστο Μηνιαίο, Πακέτο 10 Συνεδριών), 1 booking (member → CrossFit session, capacity 1 → γεμάτο), 1 waitlist entry (member2 στο ίδιο session). Ο `member3` δεν έχει καμία κράτηση — ελεύθερος για δοκιμές.

## Αρχιτεκτονική (τελική — διαφέρει από το αρχικό πλάνο)

Το αρχικό πλάνο προέβλεπε same-origin rewrite-proxy (τα static sites κάνουν rewrite τα API paths προς το API, μηδέν CORS). Στην πράξη, το Render Static Site "Redirects/Rewrites" feature αποδείχθηκε αναξιόπιστο για proxying προς dynamic/authenticated API (δες παρακάτω "Γνωστά Render gotchas"). Η τελική λύση:

- Το API επιτρέπει **CORS** και σε Production (όχι μόνο Development), με allowed origins τα 2 static site domains (config-driven, `Cors:AllowedOrigins`).
- Τα Angular apps καλούν το API **απευθείας** (cross-origin) μέσω ενός `apiBaseUrlInterceptor` (`libs/auth`) που κάνει prepend το πραγματικό API URL σε κάθε relative request, βασισμένο σε Angular `environment.production.ts` (build-time, μέσω `fileReplacements`).
- Τα static sites χρειάζονται πλέον **μόνο** το SPA fallback rewrite rule: `/* → /index.html`. Όλα τα άλλα API rewrite rules **πρέπει να διαγραφούν** — δεν είναι απλά νεκρός κώδικας: όσο μένουν, "κλέβουν" κάθε reload σε Angular route που συμπίπτει σε path με API prefix (π.χ. `/schedule`, `/class-types`, `/users`, `/tenant`, `/sessions`) πριν προλάβει να σερβιριστεί το `index.html`, δίνοντας 401/404 αντί για την εφαρμογή. Αυτό ήταν πραγματικό bug που εμφανίστηκε (reload → "no webpage found") και διορθώθηκε σβήνοντας όλα τα rules εκτός του `/*`.

## Env vars στο Render (API Web Service)

| Env var | Τιμή |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__Default` | Npgsql connection string από Neon (`Host=...;Database=...;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true`) |
| `Jwt__SigningKey` | Τυχαίο string ≥32 χαρακτήρων (π.χ. `openssl rand -base64 48`) |
| `Seed__AdminPassword` | `Admin123!` |
| `Invitations__RegisterUrlBase` | `https://gym-booking-2.onrender.com/register` |
| `Cors__AllowedOrigins__0` / `__1` | Ήδη default στο `appsettings.Production.json` (τα 2 static site URLs) — env var override μόνο αν αλλάξουν τα domains |

## Πώς προσθέτεις δεύτερο tenant (προσωπικά tests)

Πρόσθεσε στο Render API service env vars (idempotent σε slug, δεν πειράζει το demo tenant):

```
Seed__AdditionalTenants__0__TenantName=Test Gym 2
Seed__AdditionalTenants__0__TenantSlug=test-gym-2
Seed__AdditionalTenants__0__CancellationHours=24
Seed__AdditionalTenants__0__AdminEmail=admin2@test.gym
Seed__AdditionalTenants__0__AdminPassword=<δικό σου password>
```

Restart το service (Manual Deploy ή απλό restart) — ο `DbSeeder.SeedAdditionalTenantAsync` τρέχει στο startup και δημιουργεί το νέο tenant + admin.

## Cold start

Render free tier: τα services "κοιμούνται" μετά από ~15 λεπτά αδράνειας. Το πρώτο request μετά ξύπνημα καθυστερεί **~30-50 δευτερόλεπτα** (API container boot + migrations + seed check). Τα επόμενα requests είναι γρήγορα μέχρι το επόμενο idle period. Αν κάνεις demo/παρουσίαση, "ζέστανε" τα services λίγα λεπτά πριν (άνοιξε το `/health` του API + τα 2 static sites).

## Γνωστά Render gotchas (βρέθηκαν κατά το deployment)

1. **Docker Build Context**: το πεδίο "Docker Build Context Directory" στο Render πρέπει να είναι `backend` (όχι το default root του repo) — αλλιώς build error `"/src": not found`.
2. **`:splat` δεν είναι έγκυρο Render syntax** για rewrite rules — το Render χρησιμοποιεί bare `*` και στο Destination (π.χ. `/auth/* → https://api.../auth/*`), όχι `:splat` (Netlify/Vercel convention). Με `:splat` το destination γίνεται literal string, 404 σε όλα τα wildcard-matched requests.
3. **Rule order**: το `/* → /index.html` (SPA fallback) πρέπει να είναι η **τελευταία** γραμμή στη λίστα rewrites — αν μπει πριν από τα άλλα rules (π.χ. επειδή προστέθηκε αργότερα με "+ Add Rule", που append-άρει στο τέλος πάνω από ένα ήδη υπάρχον `/*`), σκεπάζει τα πάντα.
4. **Exact-match vs wildcard**: ένα rewrite rule `/foo/*` ταιριάζει **μόνο** με paths που έχουν literal `/` μετά το `foo` (π.χ. `/foo/123`) — **όχι** με το bare `/foo` (χωρίς trailing slash). Χρειάζεται ξεχωριστό exact-match rule (`/foo → https://api.../foo`) για τα collection/list endpoints που καλούνται bare.
5. **Edge caching σε authenticated proxied responses**: ακόμα κι αφού διορθώθηκαν τα #2-#4, το Render/Cloudflare edge cache-άρει τις proxied απαντήσεις **αγνοώντας το `Authorization` header** (η ίδια cached response σερβίρεται ανεξαρτήτως ποιανού token χρησιμοποιήθηκε) — ακατάλληλο για dynamic/authenticated REST API. **Αυτό οδήγησε στην εγκατάλειψη του rewrite-proxy approach υπέρ του CORS** (δες "Αρχιτεκτονική" παραπάνω).
6. **Docker runtime `libgssapi_krb5.so.2` warning**: το minimal ASP.NET runtime image δεν έχει Kerberos support για Npgsql — απλό warning στα logs, δεν εμποδίζει τη σύνδεση (η Neon χρησιμοποιεί password/SSL auth, όχι Kerberos).

## Mobile smoke test (checklist)

- [ ] Άνοιξε το staff URL σε κινητό → login ως `admin@demo.gym`.
- [ ] Δημιούργησε invitation (role = User) → αντίγραψε το register link.
- [ ] Άνοιξε το link (customer register) σε άλλο κινητό/tab → κάνε register.
- [ ] Login ως ο νέος user στο customer app → δες το πρόγραμμα (demo sessions).
- [ ] Κάνε μια κράτηση (σημείωση: ο νέος user δεν έχει συνδρομή by default — χρειάζεται ο admin να του δώσει μία μέσω staff UI πρώτα, ή δοκίμασε με `member3@demo.gym` που έχει ήδη ενεργή συνδρομή).
- [ ] Επιβεβαίωσε ότι όλα δουλεύουν σωστά από κινητό (responsive, χωρίς overflow, σωστά requests μέσω CORS).
