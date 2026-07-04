# Gym Booking — Technical Spec

**Stack:** Angular (Material + Tailwind + Signals) · .NET Web API (EF Core, Serilog, Swagger, ASP.NET Core Identity + JWT) · PostgreSQL · Docker · Git/GitHub
**Φύση:** multi-tenant SaaS = ένα app σερβίρει πολλά γυμναστήρια με χωρισμένα δεδομένα. Στην πτυχιακή το τρέχουμε με 1 γυμναστήριο. Φιλοσοφία: απλό baseline → προσθέτουμε επάνω του (όχι ξαναγράψιμο).

## Αρχιτεκτονική
```
[customer app (mobile-first)] ┐
                              ├ --REST--> [.NET Web API] --EF Core--> [PostgreSQL]
[staff app (desktop-first)]   ┘
   Angular monorepo + shared libs
```
REST = το frontend μιλάει στο backend με HTTP κλήσεις (GET/POST κ.λπ.). EF Core = το εργαλείο που μεταφράζει C# αντικείμενα ↔ πίνακες της βάσης (ORM), για να μη γράφεις SQL στο χέρι.
- **FE (frontend):** **Angular monorepo (ένα workspace) με 2 apps**: `customer` (μέλη, mobile-first) + `staff` (instructors+admins, desktop-first), που μοιράζονται **shared libraries** (`models`, `data-access`/API services, `auth`, `ui`) → μηδέν διπλός κώδικας. Κοινά: standalone components (κομμάτια UI χωρίς NgModule boilerplate) · Angular Signals (reactive state — μεταβλητές που ειδοποιούν το UI όταν αλλάζουν) · lazy features (φορτώνει κάθε σελίδα όταν χρειαστεί) · role guards · JWT interceptor (βάζει αυτόματα το token σε κάθε κλήση).
- **Σημ.:** ο αρχικός στόχος «ένα URL desktop+mobile χωρίς 2 clients» διατηρείται για το **customer** app — είναι ΕΝΑ responsive app για μέλη. Το `staff` είναι χωριστό κοινό (προσωπικό), όχι δεύτερος client για τον ίδιο χρήστη.
- **BE (backend):** layered = χωρισμένο σε επίπεδα Controllers (δέχονται το request) → Services (η λογική) → EF Core (η βάση). Serilog = βιβλιοθήκη logging. Swagger = αυτόματη τεκμηρίωση/δοκιμή του API σε σελίδα.
- **DB:** PostgreSQL · code-first migrations = γράφεις τα C# entities και το EF Core φτιάχνει/ενημερώνει το schema της βάσης αυτόματα.

## Multi-tenancy (πώς χωρίζονται τα δεδομένα ανά γυμναστήριο)
- Μία κοινή βάση + στήλη `TenantId` (= ποιανού γυμναστηρίου είναι) σε κάθε πίνακα.
- **EF Core Global Query Filters:** ορίζεις μία φορά «φέρε μου μόνο όσα έχουν το TenantId μου» και εφαρμόζεται **αυτόματα σε κάθε query**. Έτσι δεν ξεχνάς ποτέ το φίλτρο → δεν διαρρέουν δεδομένα άλλου γυμναστηρίου.
- Το `TenantId` το παίρνουμε **από το token** του χρήστη, ποτέ από αυτά που στέλνει ο ίδιος (αλλιώς θα μπορούσε να ζητήσει ξένα δεδομένα).
- **Tenant resolver:** μικρό κομμάτι που αποφασίζει «σε ποιο γυμναστήριο ανήκει αυτό το request». Τώρα το βρίσκει από το token· αργότερα θα μπορεί από το subdomain (π.χ. `ironfit.app.com`) χωρίς αλλαγή της υπόλοιπης λογικής.
- Tests που αποδεικνύουν ότι το ένα γυμναστήριο δεν βλέπει τα δεδομένα του άλλου.

## Auth & Security (σε 2 φάσεις)
JWT = ένα υπογεγραμμένο «εισιτήριο» που αποδεικνύει ποιος είσαι σε κάθε κλήση. Claims = τα στοιχεία μέσα στο token (ποιος, ποιο γυμναστήριο, ρόλος). Το auth μπαίνει πίσω από abstraction ώστε η Φάση 2 να είναι προσθήκη, όχι ξαναγράψιμο.

**Φάση 1 (πτυχιακή — απλό & αρκετό):**
- **ASP.NET Core Identity** = έτοιμη βιβλιοθήκη που αποθηκεύει κωδικούς **hashed** (PBKDF2+salt = ο κωδικός δεν αποθηκεύεται ποτέ καθαρός). Δεν γράφουμε δικό μας κρυπτογράφημα.
- **Μόνο access token** (~1–2h ισχύς) με claims `userId/tenantId/roles`. Ελέγχουμε υπογραφή & λήξη. Το κλειδί υπογραφής σε secrets (όχι μέσα στον κώδικα/git).
- Έλεγχοι πρόσβασης: ανά **ρόλο** (`[Authorize(Roles=...)]`) + **tenant isolation** + **resource ownership** (ο χρήστης ακυρώνει **μόνο τη δική του** κράτηση).
- HTTPS (κρυπτογραφημένη σύνδεση), parameterized queries (προστασία από SQL injection), το Angular «καθαρίζει» HTML αυτόματα (προστασία από XSS).
- Invitation tokens (τα links πρόσκλησης): τυχαία, hashed, με λήξη, μιας χρήσης.
- Token στο frontend: προσωρινά στο `localStorage` (απλό).

**Φάση 2 (προϊόν — σκλήρυνση):**
- **Refresh token rotation:** δίπλα στο short-lived access token, ένα refresh token που ανανεώνεται κάθε φορά· αν χρησιμοποιηθεί παλιό = πιθανή κλοπή → ακύρωση όλων.
- Token storage: access στη μνήμη + refresh σε **httpOnly cookie** (cookie που η JavaScript δεν μπορεί να διαβάσει → προστασία από XSS) + προστασία CSRF.
- **Rate limiting** (όριο προσπαθειών) + **account lockout** (κλείδωμα μετά από Χ αποτυχίες) στο login → κατά brute-force.
- OWASP ZAP scan (αυτόματος έλεγχος ευπαθειών) + security analyzers.

## Data Model (οι πίνακες)
Όλοι οι πίνακες γυμναστηρίου έχουν `TenantId`.
- **Tenant** (το γυμναστήριο): name, slug, settings (π.χ. ώρες πριν για ακύρωση)
- **User**: email, passwordHash, TenantId, status (Pending = περιμένει έγκριση / Active)
- **Role / UserRole**: User / Instructor / Admin
- **Invitation**: email, role, TenantId, token, expiry, usedAt
- **MembershipPlan** (τύπος συνδρομής): name, **Type {SessionPack = πακέτο Χ προπονήσεων | Unlimited = απεριόριστο για διάστημα}**, sessionsCount, durationDays, price
- **Subscription** (η συνδρομή ενός χρήστη): UserId, PlanId, remainingSessions (υπόλοιπο), validFrom/validTo, status
- **ClassType** (είδος μαθήματος, π.χ. Yoga): name, description, defaultDuration, defaultCapacity
- **ClassSession** (συγκεκριμένη ώρα μαθήματος): ClassTypeId, InstructorId, startsAt, duration, capacity, **bookedCount** (πόσοι έκλεισαν)
- **Booking** (κράτηση): UserId, ClassSessionId, status {Confirmed | Cancelled}, createdAt
- **WaitlistEntry** (λίστα αναμονής): UserId, ClassSessionId, position, createdAt

## Ρόλοι
- **User:** εγγραφή μέσω invite, login, βλέπει πρόγραμμα, κάνει/ακυρώνει κράτηση, βλέπει υπόλοιπο συνδρομής & ιστορικό.
- **Instructor:** δημιουργεί/διαχειρίζεται μαθήματα & ώρες (ημερομηνία/διάρκεια/χωρητικότητα), βλέπει ποιοι δήλωσαν.
- **Admin:** χρήστες & ρόλους, προσκλήσεις, τύπους συνδρομών, εποπτεία κρατήσεων, ρυθμίσεις γυμναστηρίου.

## Scope (τι μπαίνει και πότε)
**🟢 MUST (ο πυρήνας):** auth + 3 ρόλοι · multi-tenant · εγγραφή μέσω invite link · CRUD μαθημάτων + **one-off sessions** (ο instructor φτιάχνει κάθε ώρα ξεχωριστά) + εβδομαδιαίο πρόγραμμα · **atomic booking** (κράτηση χωρίς overbooking) · ακύρωση + αποτροπή διπλοκράτησης · ιστορικό/επερχόμενα · dashboard ανά ρόλο · responsive (ένα app, desktop+mobile).

**🟡 SHOULD — Προϊόν (αν προλάβουμε):** συνδρομές/πακέτα + αφαίρεση προπόνησης σε κάθε κράτηση · waitlist (όταν γεμίσει, αυτόματη προώθηση του 1ου σε ακύρωση) · πολιτική ακύρωσης (όχι <Χ ώρες πριν) · φίλτρα μαθημάτων · **recurring template** (μοτίβο «κάθε Δευτ/Τετ 18:00» που γεννά sessions για Χ εβδομάδες) · **i18n-ready** (κείμενα σε resource files· UI στα Ελληνικά, εύκολη προσθήκη αγγλικών).

**🟡 SHOULD — Tech (αξία για CV):**
- **SignalR** = real-time επικοινωνία (live ενημέρωση θέσεων χωρίς refresh).
- **Background jobs** (Hangfire/Quartz) = εργασίες που τρέχουν μόνες τους στο παρασκήνιο (λήξη συνδρομών, reminders).
- **GitHub Actions (CI/CD)** = κάθε φορά που ανεβάζεις κώδικα, τρέχει αυτόματα build + tests.
- **PWA** = το customer app εγκαθίσταται στο κινητό σαν εφαρμογή. *(χαμηλότερη προτεραιότητα — πρώτο που κόβεται αν πιέσει ο χρόνος, λόγω του monorepo setup overhead.)*
> Σειρά: πρώτα τα Προϊόν SHOULD, μετά τα Tech όσα προλάβουμε. Serilog μπαίνει σίγουρα. SignalR+Signals: θα μπουν στο προϊόν ακόμα κι αν κοπούν από την πτυχιακή.

**🔴 WON'T (εκτός, για μέλλον):** online πληρωμές, email/SMS notifications, native app, analytics, ξεχωριστό domain ανά γυμναστήριο.

## Conventions / Αποφάσεις
- **Tenant provisioning:** για πτυχιακή, **seed** στη βάση (1 γυμναστήριο + 1 Admin έτοιμα). Platform/super-admin που ανοίγει νέα γυμναστήρια = future.
- **Subscription assignment:** ο **Admin αναθέτει χειροκίνητα** πακέτο/συνδρομή σε χρήστη (σαν πληρωμή στο ταμείο). Online πληρωμή = future.
- **Dates/timezone:** αποθήκευση σε **UTC** στη βάση, εμφάνιση σε τοπική (Europe/Athens). Per-tenant timezone = future.
- **Soft delete:** flag `isActive`/`isDeleted` αντί για πραγματική διαγραφή σε μαθήματα/χρήστες → διατήρηση ιστορικού, χωρίς σπασμένα foreign keys.

## Κρίσιμη Λογική
- **Atomic booking** (το «καρδιά» — να μη γίνει overbooking σε ταυτόχρονες κρατήσεις): μέσα σε μία transaction κλειδώνουμε τη σειρά του μαθήματος (`SELECT … FOR UPDATE` = «κανείς άλλος μην το πειράξει μέχρι να τελειώσω»), ελέγχουμε `bookedCount < capacity`, αφαιρούμε προπόνηση αν είναι πακέτο, και ή γίνονται όλα ή τίποτα. Το αποδεικνύουμε με test ταυτόχρονων κρατήσεων.
- **Ακύρωση:** επιστροφή προπόνησης στο πακέτο αν είσαι εντός ορίου· μπλοκ αν είναι <Χ ώρες πριν.
- **Waitlist:** σε ακύρωση, μπαίνει αυτόματα ο 1ος της αναμονής.
- **Email:** πίσω από interface `IEmailSender`. Τοπικά → Papercut (ψεύτικο inbox)/log. Production → αλλάζεις μόνο config για πραγματικό SMTP.

### Real-time (σε φάσεις)
Σημαντικό: το real-time είναι **εμπειρία χρήστη, όχι ορθότητα** — το atomic booking ήδη εγγυάται ότι δεν γίνεται overbooking. Άρα μπαίνει αργότερα χωρίς ρίσκο.
- **Φ1 (MUST):** καθόλου real-time· το UI ανανεώνεται με refresh/ξαναφόρτωμα.
- **Φ2 (προαιρετικό, εύκολο):** polling = η σελίδα ρωτάει «άλλαξε κάτι;» κάθε ~10–15 δευτ.
- **Φ3 (SHOULD):** SignalR στέλνει `SpotsUpdated(sessionId, bookedCount)` → ο client κάνει `spots.set(...)` (Signal) → το UI ενημερώνεται μόνο του. Ξεχωριστά κανάλια (groups) ανά γυμναστήριο.
- **Φ4 (προϊόν):** ειδοποίηση «άνοιξε θέση» από waitlist, ζωντανή λίστα συμμετεχόντων.
- **Κλειδί:** βάζουμε **ένα μόνο σημείο** στον κώδικα όπου αλλάζει το `bookedCount`. Στη Φ3 προσθέτουμε εκεί **μία γραμμή** που στέλνει το real-time μήνυμα — χωρίς να αλλάξει η λογική κράτησης.

## Frontend δομή (monorepo)
**Workspace:**
- **`customer` app** (mobile-first): `auth` (login + εγγραφή μέσω invite) · `schedule` (εβδομαδιαίο + φίλτρα) · `bookings` (δικές μου/ιστορικό) · `subscription` (υπόλοιπο/ιστορικό).
- **`staff` app** (desktop-first): `auth` · `instructor` (μαθήματα/sessions + συμμετέχοντες) · `admin` (χρήστες/ρόλοι/invitations/συνδρομές/εποπτεία/ρυθμίσεις). Ξεκινά desktop-only με βασικές λειτουργίες, επεκτείνεται για προϊόν.
- **shared libs:** `models` (DTOs/types) · `data-access` (API services) · `auth` (login, JWT interceptor, guards) · `ui` (κοινά components).

Κοινά παντού: lazy loading, role guards, JWT interceptor (από τη `auth` lib).

## Testing
- **xUnit** (unit tests C#): ταυτόχρονες κρατήσεις (no overbooking), tenant isolation, λογική συνδρομών.
- **Integration με Testcontainers:** σηκώνει αληθινή PostgreSQL σε container μόνο για τα tests → ρεαλιστικά αποτελέσματα.
- **FE:** λίγα — guards + βασικές ροές.

## Docker (πρώτα τοπικά)
Docker = πακετάρει app + περιβάλλον μαζί, ώστε να τρέχει το ίδιο παντού (αντί να στήνεις τον server με το χέρι).
- **Τώρα:** ένα `docker-compose` σηκώνει PostgreSQL + Papercut με μία εντολή (`docker compose up`).
- **Deployment (φάση 8):** ένα `Dockerfile` για το API, ένα (nginx) για το Angular, μαζί με compose. Αντικαθιστά το παλιό copy-paste σε server.

## CI/CD (GitHub Actions)
CI = σε κάθε push, ένας καθαρός runner στο cloud χτίζει & τρέχει τα tests → ✅/❌. CD = αν περάσουν, φτιάχνει & κάνει deploy το πακέτο.
- **CI (`.github/workflows/ci.yml`):** trigger σε push + PR · jobs: backend (`dotnet build` + `dotnet test`) και frontend (`npm ci` + build + test). Testcontainers δουλεύουν (οι runners έχουν Docker).
- **Branch protection:** δεν γίνεται merge αν τα tests είναι κόκκινα → σπασμένος κώδικας δεν φτάνει στο `main`.
- **CD (φάση 8):** μόνο σε `main` & μόνο αν περάσει το CI → build Docker images → push σε registry (ghcr.io, tag = git SHA για rollback) → `deploy` job με SSH στον server (`docker compose pull && up -d`). `environment: production` με **χειροκίνητη έγκριση** (Continuous Delivery).
- **Secrets** (SSH key, host) στα GitHub repo secrets, όχι στον κώδικα.
- **Server:** φθηνό VPS (Hetzner/DO) με SSH deploy, ή PaaS (Render/Railway/Fly) με built-in GitHub integration. Live deploy = προαιρετικό για πτυχιακή, δυνατό demo/CV.
- **Extra (προαιρετικά):** linting/format gate, code coverage, status badge, CodeQL/Dependabot (security scan).

## Timeline (~100h, ~9 βδ., −1 άδεια)
| Φάση | Περιεχόμενο | ~h |
|---|---|---|
| Setup | repo, **Angular monorepo (customer+staff+shared libs)**, compose+Postgres, EF, base entities, tenant infra | 14 |
| 1 | auth+JWT+ρόλοι+invitation/email+εγγραφή | 12 |
| 2 | CRUD μαθημάτων/sessions + instructor | 12 |
| 3 | 🔥 booking core (atomic, ακύρωση, anti-double) + tests | 14 |
| 4 | εβδομαδιαίο πρόγραμμα + φίλτρα + user dashboard | 12 |
| 5 | συνδρομές/πακέτα + αφαίρεση + admin διαχείριση | 12 |
| 6 | waitlist + πολιτική ακύρωσης | 10 |
| 7 | admin dashboard, διαχείριση χρηστών, responsive QA | 12 |
| 8 | σκλήρυνση, security pass, demo data, deployment | 12 |
| Σεπτ (2βδ) | συγγραφή, διαγράμματα, demo, buffer | — |

## Future
online πληρωμές, notifications, domain ανά γυμναστήριο, analytics, PWA/native, hybrid tenancy (ξεχωριστή βάση για μεγάλους πελάτες).
