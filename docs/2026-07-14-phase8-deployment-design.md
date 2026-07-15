# Φάση 8 (Μέρος A) — Deployment για demo/tests — Design

> **Scope αυτού του sub-project:** να τρέχει η εφαρμογή σε ένα **σταθερό δημόσιο URL**, προσβάσιμο από κινητά, **δωρεάν**, μόνο για παρουσίαση/tests (όχι πραγματική χρήση/production traffic). Είναι το **πρώτο** από τα κομμάτια της Φάσης 8· τα υπόλοιπα (security pass, sidenav bug, πλήρες CI/CD gate) μένουν για επόμενα sub-projects.
>
> **Spec (single source of truth):** [2026-06-28-gym-booking-design.md](2026-06-28-gym-booking-design.md) · **Implementation plan:** [2026-06-28-implementation-plan-master.md](2026-06-28-implementation-plan-master.md)

## Στόχος & περιορισμοί

- **Θέλουμε:** live URL που ανοίγει από οποιοδήποτε κινητό, με έτοιμα demo δεδομένα, ώστε να κάνουμε tests με πραγματικούς χρήστες και να το δείχνουμε όπου χρειαστεί.
- **ΔΕΝ θέλουμε (τώρα):** production-grade ασφάλεια/κλίμακα, πληρωμένο hosting, μηδενικό downtime. Είναι demo — αποδεκτό το «κοιμάται & ξυπνάει σε ~30s».
- **Κόστος:** αυστηρά $0. Χωρίς πιστωτική κάρτα όπου γίνεται.

## Επιλεγμένη αρχιτεκτονική

| Component | Πλατφόρμα (free) | Σημειώσεις |
|---|---|---|
| `customer` app (Angular build) | Render **Static Site** #1 | Δωρεάν, χωρίς λήξη, HTTPS + CDN out-of-the-box |
| `staff` app (Angular build) | Render **Static Site** #2 | Ξεχωριστό site (ξεχωριστό app· διαφορετικό κοινό) |
| .NET API | Render **Web Service** (Free, Docker) | Auto-deploy από push σε `main`· sleeps μετά από ~15′ αδράνειας, cold start ~30s |
| PostgreSQL | **Neon** (free tier) | Serverless Postgres, **δεν λήγει** (σε αντίθεση με το free Postgres του Render που σβήνει σε ~30 μέρες) |

### Γιατί αυτή η στοίβα
- **Ένας πάροχος (Render) για app + API:** ένα dashboard, native GitHub auto-deploy, δωρεάν HTTPS. Το μόνο εξωτερικό είναι η Neon για μόνιμη βάση.
- **Neon αντί Render Postgres:** το project ζει μήνες· δεν θέλουμε να χαθούν δεδομένα σε 30 μέρες.

### Το κλειδί: same-origin μέσω rewrite-proxy (μηδέν CORS αλλαγές)
Τα Angular apps καλούν το API με **relative paths** (`/auth`, `/class-types`, `/bookings`, …) — δες [`frontend/apps/staff/proxy.conf.js`](../frontend/apps/staff/proxy.conf.js). Τοπικά αυτό το γεφυρώνει το `proxy.conf.js`.

Στο deployment, κάθε Render Static Site παίρνει **rewrite rules** που προωθούν αυτά ακριβώς τα path-prefixes στο API origin. Το browser βλέπει τα πάντα σαν **ίδιο origin** → **δεν χρειάζεται production CORS policy** και **δεν αλλάζει κώδικας στα data-access services**. Είναι το ίδιο μοντέλο με το dev proxy, μεταφερμένο στο hosting layer.

> Fallback αν οι rewrites του Render δεν καλύπτουν κάποιο path: γίνεται προσθήκη production CORS policy + absolute API base URL μέσω Angular `environment.ts`. Το προτιμώμενο μονοπάτι είναι ο proxy (λιγότερος κώδικας).

## Αλλαγές στην εφαρμογή

### A. Backend — deployment readiness (κρίσιμα gotchas)

1. **Auto-apply migrations στο startup.** Σήμερα οι migrations τρέχουν **χειροκίνητα** (`dotnet ef database update`). Σε deployed container δεν υπάρχει αυτό το βήμα → η Neon DB θα μείνει χωρίς schema. Προσθήκη στο `Program.cs` (πριν το seeding, μόνο εκτός `Testing`): εφαρμογή `dbContext.Database.Migrate()` σε scope στο startup.

2. **HTTPS redirect πίσω από proxy.** Το Render τερματίζει το TLS στον edge proxy· ο container δέχεται HTTP εσωτερικά. Το `app.UseHttpsRedirection()` μπορεί να προκαλέσει redirect loops/λάθος scheme. Λύση: `UseForwardedHeaders` (ώστε το app να «βλέπει» το αρχικό `X-Forwarded-Proto`), και/ή περιορισμός του `UseHttpsRedirection` εκτός του hosted environment. (Το TLS το εγγυάται ο Render — δεν χάνουμε ασφάλεια.)

3. **Port binding.** Το Render περνάει το port μέσω env var `PORT`. Το API πρέπει να ακούει εκεί (`ASPNETCORE_URLS=http://0.0.0.0:$PORT` ή αντίστοιχο στο Dockerfile/env).

4. **Dockerfile για το API.** Δεν υπάρχει κανένα Dockerfile στο repo ακόμα. Νέο multi-stage `Dockerfile` (build με .NET SDK → runtime image) στο `backend/`.

### B. Backend — invitation link στο admin UI (αντί email)

Σε deployed demo το Papercut δεν είναι προσβάσιμο από κινητό. Αντί για email, ο admin παίρνει το raw invite link μέσα στο staff app και το στέλνει χειροκίνητα (WhatsApp/SMS).

- `InvitationService.CreateAsync` → επιστρέφει το `registerLink` (τώρα `void`).
- `InvitationsController.Create` → `200 OK { registerLink }` (τώρα `NoContent()`).
- **Production-safe `IEmailSender`:** το υπάρχον `SmtpEmailSender` θα προσπαθήσει σύνδεση σε ανύπαρκτο SMTP σε production και θα ρίξει exception → θα μπλόκαρε τη δημιουργία invitation. Νέος `LoggingEmailSender` (γράφει το email στο Serilog log αντί να στέλνει). Στο `Program.cs`, το registration γίνεται conditional: dev → `SmtpEmailSender` (Papercut), αλλού → `LoggingEmailSender`.
- **`RegisterUrlBase`** γίνεται per-environment config (env var στο Render → δείχνει στο deployed customer app URL), όχι hardcoded `http://localhost:4200/register`.
- **Staff frontend:** μετά την επιτυχή πρόσκληση, dialog/snackbar με το link + κουμπί «Αντιγραφή». (Το invite form ήδη υπάρχει· προσθέτουμε την εμφάνιση του link στο response.)

### C. Demo dataset (πλουσιότερο seeding)

Επέκταση του seeding ώστε ένας tester να βλέπει αμέσως ένα «ζωντανό» app. Πάνω από το υπάρχον (1 tenant + 1 admin), προσθήκη:
- 2–3 class types (π.χ. Yoga, Pilates, Spin).
- 1–2 instructors (με γνωστά credentials για το demo).
- Αρκετά **μελλοντικά** class sessions (ώστε να γεμίζει το εβδομαδιαίο πρόγραμμα).
- 2–3 members με active subscriptions.
- 1–2 bookings + 1 waitlist entry (ώστε να φαίνονται roster/waitlist).

**Approach:** επέκταση του `DbSeeder` ή νέος `DemoDataSeeder` που τρέχει **μόνο σε άδεια βάση** (ίδιο guard με το υπάρχον). Τα demo credentials τεκμηριώνονται (README/spec) για το demo. **Ανοιχτό στην υλοποίηση:** αν τα demo credentials πρέπει να είναι config-driven ή σταθερά για ευκολία — θα κριθεί στο plan.

### D. Δεύτερο tenant (για προσωπικά tests) — config-driven seed

Δεν θέλουμε self-service ή δημόσιο API endpoint (το spec αφήνει το tenant provisioning για το μέλλον). **Κλειδωμένη απόφαση:** ο admin του νέου tenant δημιουργείται με τον **ίδιο μηχανισμό όπως ο πρώτος** — μέσω `UserManager.CreateAsync` (όπως στον `DbSeeder`), ώστε να παράγεται **έγκυρο Identity password hash**. Δεν χρησιμοποιούμε raw SQL insert για τον χρήστη (δεν παράγει έγκυρο hash).

- **Μηχανισμός:** ένα **config-driven, on-demand seed** που δημιουργεί έναν επιπλέον `Tenant` + roles + τον πρώτο admin του, επαναχρησιμοποιώντας το pattern του `DbSeeder`. Τρέχει μόνο όταν το ζητήσω ρητά (π.χ. προαιρετικό `Seed:AdditionalTenants` section / flag), **idempotent** (δεν ξαναδημιουργεί tenant που υπάρχει ήδη — guard σε slug).
- Ο χρήστης δίνει τα στοιχεία (όνομα/slug γυμναστηρίου, admin email/password) και ο μηχανισμός τον δημιουργεί — «όπως έγινε ο πρώτος admin».
- **Ανοιχτό στην υλοποίηση (μικρό):** αν το «on-demand» υλοποιηθεί ως ξεχωριστό config section που ελέγχεται στο startup, ή ως ξεχωριστό one-off command. Θα κριθεί στο plan· ο πυρήνας (UserManager, idempotent, config-driven) είναι κλειδωμένος.

### E. Secrets & config baseline (μόνο τα απαραίτητα)

- **Environment variables στο Render** (ποτέ στο git): `ConnectionStrings__Default` (Neon), `Jwt__SigningKey`, `Seed__AdminPassword`, `Invitations__RegisterUrlBase`, demo instructor/member passwords.
- **`appsettings.Production.json`** σκελετός: δομή χωρίς μυστικές τιμές (οι τιμές έρχονται από env vars με το `__` convention).
- **CORS:** μένει dev-only (ο same-origin proxy το κάνει περιττό σε production).
- **`.gitignore`:** επιβεβαίωση ότι δεν διαρρέει κανένα secret/appsettings με τιμές.

## Ρητά ΕΚΤΟΣ αυτού του sub-project

Μένουν για επόμενα Φάση-8 chats:
- **Security pass:** rate limiting, account lockout, refresh token rotation (httpOnly cookie), OWASP ZAP scan, security analyzers.
- **Sidenav drawer bug** (staff, στενές οθόνες) — καταγεγραμμένο ξεχωριστό task.
- **Πλήρες CI/CD gate:** deploy μόνο αν περάσει το `dotnet test` (τώρα: native Render auto-deploy on push, χωρίς gate).
- **Πραγματικό email** (SMTP provider) — αντικαταστάθηκε από invite-link-in-UI για το demo.

## Ροή deployment (high-level)

1. Push `Dockerfile` + config αλλαγές σε `main`.
2. Neon: δημιουργία δωρεάν project → connection string.
3. Render: Web Service (Docker) → env vars → auto-deploy. Migrations + seed τρέχουν στο startup.
4. Render: 2 Static Sites (customer, staff) με build command (`nx build …`) + rewrite rules προς το API.
5. `Invitations__RegisterUrlBase` → δείχνει στο customer Static Site URL.
6. Smoke test από κινητό: login admin → invite → register → book.

## Ρίσκα / ανοιχτά

- **Cold start ~30s** στο πρώτο request μετά από αδράνεια (Render free) — αποδεκτό για demo· να ενημερώνονται οι testers.
- **Δεύτερο-tenant admin hash** — ✅ λύθηκε: δημιουργία μέσω `UserManager` (config-driven seed), όχι raw SQL (βλ. D).
- **Render Static Site rewrites** — να επιβεβαιωθεί ότι καλύπτουν όλα τα API path-prefixes· αλλιώς fallback σε CORS + absolute URL.
- **Demo credentials** στο repo/README — αποδεκτό γιατί είναι throwaway demo instance, όχι production.
