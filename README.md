# GymBooking — Εφαρμογή Διαχείρισης Κρατήσεων και Προγραμμάτων Γυμναστηρίου

Πολυμισθωτική (multi-tenant) διαδικτυακή εφαρμογή για κρατήσεις μαθημάτων γυμναστηρίου, που αναπτύχθηκε ως πτυχιακή εργασία στο Τμήμα Πληροφορικής του Πανεπιστημίου Πειραιώς.

- **Μέλη:** εβδομαδιαίο πρόγραμμα με φίλτρα, κράτηση και ακύρωση μαθήματος, λίστα αναμονής με αυτόματη προαγωγή, συνδρομή και ιστορικό.
- **Προσωπικό:** τύποι μαθημάτων, ώρες μαθημάτων, παρουσιολόγιο, κράτηση και ακύρωση εκ μέρους πελάτη.
- **Διαχειριστές:** χρήστες και ρόλοι, προσκλήσεις εγγραφής, συνδρομητικά πλάνα και συνδρομές, ρυθμίσεις γυμναστηρίου.
- **Πολλά γυμναστήρια σε μία εγκατάσταση:** κοινή βάση με αυτόματο διαχωρισμό δεδομένων ανά γυμναστήριο.
- **Ατομικές κρατήσεις:** συναλλαγή με `SELECT … FOR UPDATE`, ώστε να μη γίνεται ποτέ υπέρβαση της χωρητικότητας.

## Τεχνολογίες

| Μέρος | Τεχνολογίες |
|---|---|
| Backend | .NET 10, ASP.NET Core Web API, Entity Framework Core 10 + Npgsql, ASP.NET Core Identity, JWT, Serilog, Scalar (OpenAPI) |
| Βάση δεδομένων | PostgreSQL 16 |
| Frontend | Angular 21, Nx 23 (monorepo), Angular Material, Tailwind CSS 4 |
| Δοκιμές | xUnit, Testcontainers (PostgreSQL), Vitest |
| Εκτέλεση | Docker, Docker Compose |

## Δομή

```
backend/           Λύση .NET (GymBooking.slnx)
  src/GymBooking.Api     Controllers, services, ρυθμίσεις, seeding
  src/GymBooking.Core    Οντότητες, contracts, multi-tenancy
  src/GymBooking.Data    AppDbContext, migrations
  tests/GymBooking.Tests Αυτοματοποιημένες δοκιμές και πειράματα αξιολόγησης (Evaluation/)
frontend/          Nx workspace
  apps/customer          Εφαρμογή μελών (mobile-first)
  apps/staff             Εφαρμογή προσωπικού (desktop-first)
  libs/                  models, data-access, auth, ui
docker/            docker-compose.infra.yml (μόνο PostgreSQL + Papercut, για ανάπτυξη)
                   docker-compose.full-stack.yml (ολόκληρο το σύστημα)
evaluation/        Script και αποτελέσματα της αξιολόγησης
```

## Γρήγορη εκκίνηση (Docker)

**Προαπαιτούμενο:** [Docker Desktop](https://www.docker.com/products/docker-desktop/) σε λειτουργία. Δεν χρειάζεται εγκατάσταση .NET, Node.js ή PostgreSQL. Οι εντολές εκτελούνται από τον ριζικό φάκελο του αποθετηρίου.

```bash
docker compose -f docker/docker-compose.full-stack.yml up --build
```

Την πρώτη φορά το build διαρκεί μερικά λεπτά. Κατά την εκκίνηση, το API εφαρμόζει αυτόματα τα migrations και γεμίζει τη βάση με δοκιμαστικά δεδομένα.

| Υπηρεσία | Διεύθυνση |
|---|---|
| Εφαρμογή μελών | http://localhost:4200 |
| Εφαρμογή προσωπικού | http://localhost:4201 |
| API και τεκμηρίωση (Scalar) | http://localhost:8080/scalar/v1 |
| Papercut (τα email των προσκλήσεων) | http://localhost:8025 |

**Δοκιμαστικοί λογαριασμοί** (υπάρχουν μόνο στην τοπική βάση):

| Ρόλος | Email | Κωδικός |
|---|---|---|
| Διαχειριστής | `admin@demo.gym` | `Admin123!` |
| Εκπαιδευτής | `instructor@demo.gym` | `Demo1234!` |
| Μέλος με ενεργή συνδρομή | `member3@demo.gym` | `Demo1234!` |
| Μέλος | `member@demo.gym`, `member2@demo.gym` | `Demo1234!` |

Νέοι χρήστες εγγράφονται μόνο με πρόσκληση: ο διαχειριστής τη δημιουργεί από την οθόνη «Προσκλήσεις» και ο σύνδεσμος εγγραφής εμφανίζεται στην ίδια οθόνη και στο Papercut. Ένας νέος χρήστης μπορεί να κάνει κράτηση μόνο αφού του ανατεθεί συνδρομή από την οθόνη «Συνδρομές».

**Τερματισμός:**

```bash
docker compose -f docker/docker-compose.full-stack.yml down      # σταματά τις υπηρεσίες
docker compose -f docker/docker-compose.full-stack.yml down -v   # διαγράφει και τη βάση (καθαρή εκκίνηση)
```

## Εκτέλεση για ανάπτυξη (χωρίς Docker)

Απαιτούνται .NET SDK 10, Node.js 20 με npm, και Docker Desktop για τη βάση και το Papercut.

```bash
# 1. PostgreSQL 16 και Papercut
docker compose -f docker/docker-compose.infra.yml up -d

# 2. API — http://localhost:5076 (Scalar: /scalar/v1)
cd backend
dotnet run --project src/GymBooking.Api

# 3. Εφαρμογές — http://localhost:4200 και http://localhost:4201
cd frontend
npm ci
npx nx serve customer
npx nx serve staff --port 4201
```

## Δοκιμές

```bash
cd backend && dotnet test                         # 78 δοκιμές (χρειάζεται Docker για την PostgreSQL)
cd frontend && npx nx run-many -t lint test build # 26 δοκιμές, lint και build
```

Τα πειράματα της αξιολόγησης (υπερκρατήσεις με/χωρίς κλείδωμα, χρόνοι απόκρισης) εκτελούνται ξεχωριστά με `evaluation/run-evaluation.ps1`· περισσότερα στο [evaluation/README.md](evaluation/README.md).

## Μεταβλητές περιβάλλοντος

Οι ρυθμίσεις του API βρίσκονται στα `appsettings.*.json` και αντικαθίστανται από μεταβλητές περιβάλλοντος. Στο `docker/docker-compose.full-stack.yml` υπάρχουν ήδη δοκιμαστικές τιμές· σε εγκατάσταση παραγωγής πρέπει να οριστούν δικές σας τιμές, και τα μυστικά δεν αποθηκεύονται στο αποθετήριο.

| Μεταβλητή | Σκοπός |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Development` ή `Production` |
| `ConnectionStrings__Default` | Σύνδεση με την PostgreSQL |
| `Jwt__SigningKey` | Κλειδί υπογραφής των JWT (τουλάχιστον 32 χαρακτήρες) |
| `Jwt__Issuer`, `Jwt__Audience`, `Jwt__AccessTokenExpiryMinutes` | Στοιχεία και διάρκεια του token |
| `Cors__AllowedOrigins__0`, `__1` | Επιτρεπόμενες διευθύνσεις των εφαρμογών |
| `Email__SmtpHost`, `Email__SmtpPort`, `Email__FromAddress` | Εξυπηρετητής email |
| `Invitations__RegisterUrlBase` | Βάση του συνδέσμου εγγραφής |
| `Seed__AdminEmail`, `Seed__AdminPassword`, `Seed__CancellationHours` | Αρχικός διαχειριστής και ρυθμίσεις (μόνο σε άδεια βάση) |

## Συχνά προβλήματα

- **«port is already allocated»:** κάποια από τις θύρες 4200, 4201, 8080, 8025, 5432 ή 2525 χρησιμοποιείται ήδη, π.χ. από το `docker/docker-compose.infra.yml`. Σταματήστε την άλλη υπηρεσία και ξανατρέξτε.
- **«Cannot connect to the Docker daemon»:** το Docker Desktop δεν λειτουργεί.
- **Η σύνδεση αποτυγχάνει αμέσως μετά την εκκίνηση:** το API ίσως δεν έχει ολοκληρώσει ακόμη τα migrations. Κατάσταση: http://localhost:8080/health · logs: `docker compose -f docker/docker-compose.full-stack.yml logs api`.
- **«Ο λογαριασμός κλειδώθηκε προσωρινά»:** μετά από 5 λανθασμένες προσπάθειες ο λογαριασμός κλειδώνει για 15 λεπτά· μετά από 10 προσπάθειες σύνδεσης μέσα σε ένα λεπτό απαιτείται αναμονή ενός λεπτού.

## Άδεια και παραπομπή

Ο κώδικας διατίθεται με την άδεια [MIT](LICENSE). Οι άδειες των βιβλιοθηκών που χρησιμοποιούνται βρίσκονται στο [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md), οι αλλαγές ανά έκδοση στο [CHANGELOG.md](CHANGELOG.md), και τα στοιχεία παραπομπής στο [CITATION.cff](CITATION.cff).
