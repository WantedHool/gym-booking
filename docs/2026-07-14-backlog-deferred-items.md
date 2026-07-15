# Backlog — Εκκρεμότητες & Μελλοντικά (deferred items)

> **Σκοπός:** ένα σημείο αναφοράς για όλα όσα **δεν** υλοποιούμε τώρα. Χωρισμένο σε (1) υποψήφια για την πτυχιακή και (2) για μετά, όταν το project γίνει προϊόν. Ενημέρωσέ το όποτε αναβάλλεται κάτι.
>
> **Πηγές:** [gym-booking-design.md](2026-06-28-gym-booking-design.md) (Auth Φ2, Real-time φάσεις, WON'T list, Future) · [phase8-deployment-design.md](2026-07-14-phase8-deployment-design.md) (ρητά εκτός scope) · CLAUDE.md progress notes.

Σύσταση (στήλη «Πτυχιακή;»): 🟢 = αξίζει, μικρό κόστος/μεγάλη αξία για CV/βαθμό · 🟡 = προαιρετικό, μόνο αν περισσεύει χρόνος · 🔴 = εκτός πτυχιακής, καθαρά προϊόν.

---

## A. Ασφάλεια — Auth Φάση 2 (hardening)

| Item | Τι είναι | Πτυχιακή; | Σημειώσεις |
|---|---|---|---|
| Rate limiting στο `/auth/login` | Όριο προσπαθειών ανά IP/χρόνο (κατά brute-force) | 🟢 | Μικρό (ASP.NET rate limiting middleware, built-in .NET 10). Καλό «security» σημείο στην αναφορά. |
| Account lockout | Κλείδωμα λογαριασμού μετά από Χ αποτυχίες | 🟢 | Το ASP.NET Core Identity το υποστηρίζει σχεδόν έτοιμο (`Lockout` options). Χαμηλό κόστος. |
| Refresh token rotation | Short-lived access + rotating refresh token | 🟡 | Ουσιαστικό για προϊόν· σημαντικό effort. Στην αναφορά μπορεί να μείνει ως «σχεδιασμένο, Φάση 2». |
| Token storage: httpOnly cookie + CSRF | Access σε μνήμη, refresh σε httpOnly cookie | 🔴 | Δένεται με το refresh rotation. Προϊόν. |
| OWASP ZAP scan | Αυτόματος έλεγχος ευπαθειών | 🟡 | Ένα scan + screenshot στην αναφορά είναι εντυπωσιακό & φθηνό. |
| Security analyzers | Roslyn security analyzers στο build | 🟡 | Εύκολο add· λίγη αξία στην παρουσίαση. |

**Σύσταση πτυχιακής:** rate limiting + account lockout (🟢, μικρά & δείχνουν security awareness). Προαιρετικά ένα ZAP scan για την αναφορά.

---

## B. Γνωστά bugs

| Item | Τι είναι | Πτυχιακή; | Σημειώσεις |
|---|---|---|---|
| Staff sidenav drawer σε στενές οθόνες | Το drawer μένει εκτός οθόνης όταν ανοίγει, stuck CSS transition | 🟢 | Προϋπάρχον bug (όχι από Φ7). Ορατό σε demo από κινητό → αξίζει fix πριν την παρουσίαση. Καταγεγραμμένο ως ξεχωριστό task. |

**Σύσταση πτυχιακής:** διόρθωσέ το (🟢) — είναι ορατό ελάττωμα σε mobile demo.

---

## C. CI/CD

| Item | Τι είναι | Πτυχιακή; | Σημειώσεις |
|---|---|---|---|
| CI (build + test σε push/PR) | GitHub Actions `ci.yml` | 🟢 | Ήδη σχεδιασμένο· ενεργοποιείται όταν ανέβει το repo στο GitHub. |
| Branch protection στο `main` | Δεν γίνεται merge αν κοκκινίσει το CI | 🟢 | Μικρό setup, καλό σημείο process maturity. |
| CD gate (deploy μόνο αν περάσει CI) | Το deploy μπλοκάρεται σε αποτυχία test | 🟡 | Τώρα: native Render auto-deploy χωρίς gate. Ωραίο follow-up. |
| Full CD pipeline (images→registry→SSH deploy) | ghcr.io + SSH σε VPS με manual approval | 🔴 | Το τρέχον demo deploy (Render) το καλύπτει πιο απλά. Αυτό είναι προϊόν/VPS σενάριο. |
| Extra: coverage, status badge, CodeQL, Dependabot | Security/quality gates | 🟡 | Φθηνά «bonus» σημεία για την αναφορά. |

**Σύσταση πτυχιακής:** ενεργοποίηση CI + branch protection (🟢) μόλις ανέβει στο GitHub.

---

## D. Real-time & background jobs

| Item | Τι είναι | Πτυχιακή; | Σημειώσεις |
|---|---|---|---|
| Polling (θέσεις κάθε ~10-15s) | Απλό «άλλαξε κάτι;» refresh | 🟡 | Φθηνό ενδιάμεσο βήμα πριν το SignalR. |
| SignalR real-time (`SpotsUpdated`) | Live ενημέρωση θέσεων χωρίς refresh | 🟡 | Δυνατό σημείο για CV. Το atomic booking ήδη εγγυάται ορθότητα — αυτό είναι μόνο UX. Το «single point» για `bookedCount` υπάρχει ήδη → εύκολη προσθήκη 1 γραμμής. |
| SignalR waitlist notifications | «Άνοιξε θέση» ζωντανά | 🔴 | Προϊόν. |
| Background jobs (Hangfire/Quartz) | Λήξη συνδρομών, reminders | 🔴 | Προϊόν· ωραίο για CV αλλά εκτός βασικού scope. |

**Σύσταση πτυχιακής:** αν περισσεύει χρόνος, SignalR για live θέσεις (🟡) — καλό «tech» σημείο. Αλλιώς άφησέ το.

---

## E. Features (SHOULD/Future)

| Item | Τι είναι | Πτυχιακή; | Σημειώσεις |
|---|---|---|---|
| Recurring session templates | «Κάθε Δευτ/Τετ 18:00» → γεννά sessions | 🟡 | SHOULD στο spec. Χρήσιμο αλλά μεσαίο effort. |
| i18n-ready (κείμενα σε resource files) | UI Ελληνικά + εύκολη προσθήκη Αγγλικών | 🟡 | Το UI είναι ήδη Ελληνικά· η υποδομή i18n είναι το επιπλέον. |
| PWA (customer εγκαθίσταται σαν app) | Offline/installable | 🔴 | Χαμηλότερη προτεραιότητα ήδη στο spec· πρώτο που κόβεται. |
| Branding (logo/όνομα per-tenant) | Cosmetic πέρασμα | 🟡 | Εύκολο· βελτιώνει την εικόνα του demo. |
| Πραγματικό email (SMTP provider) | Αντί για invite-link-in-UI | 🔴 | Το demo το λύνει με link στο admin UI. Προϊόν. |

**Σύσταση πτυχιακής:** ίσως ένα μικρό branding πέρασμα (🟡) για ωραιότερο demo. Τα υπόλοιπα εκτός.

---

## F. Multi-tenancy maturity

| Item | Τι είναι | Πτυχιακή; | Σημειώσεις |
|---|---|---|---|
| Self-service tenant provisioning | Super-admin που ανοίγει νέα γυμναστήρια από UI | 🔴 | Τώρα: config-driven seed (Task 7 Φ8). Προϊόν. |
| Tenant resolver από subdomain | `ironfit.app.com` → tenant | 🔴 | Η υποδομή είναι έτοιμη (resolver abstraction)· η ενεργοποίηση είναι προϊόν. |
| Per-tenant timezone | Αντί για σταθερό Europe/Athens | 🔴 | Future στο spec. |
| Hybrid tenancy (ξεχωριστή βάση για μεγάλους) | Isolated DB per big customer | 🔴 | Καθαρά προϊόν/scale. |

**Σύσταση πτυχιακής:** κανένα — η πτυχιακή τρέχει με 1 (+1 test) tenant. Η αρχιτεκτονική αποδεικνύει ήδη το multi-tenancy.

---

## G. Tech debt / maintenance

| Item | Τι είναι | Πτυχιακή; | Σημειώσεις |
|---|---|---|---|
| Testcontainers integration tests | Αληθινή Postgres σε container στα tests (αντί InMemory) | 🟡 | Το spec το προέβλεπε· τα tests τρέχουν τώρα σε InMemory. Πιο ρεαλιστικά αποτελέσματα, καλό σημείο στην αναφορά. |
| Nx eslint executor migration | `@nx/eslint:lint` καταργείται στο Nx v24 (`convert-to-inferred`) | 🟡 | Καταγεγραμμένο. Κάν' το σε hardening pass. |

**Σύσταση πτυχιακής:** Testcontainers (🟡) αν θέλεις να ενισχύσεις το testing κεφάλαιο της αναφοράς.

---

## H. Future προϊόντος (ρητά WON'T για πτυχιακή)

Από το spec (WON'T list) — όλα 🔴 για την πτυχιακή:
- Online πληρωμές
- Email/SMS notifications
- Native app
- Analytics
- Ξεχωριστό domain ανά γυμναστήριο

---

## Σύνοψη: τι προτείνω για την πτυχιακή (κρατώντας το scope σφιχτό)

**Ναι (🟢, μικρά & αξίζουν):**
1. Sidenav drawer bug fix (ορατό σε mobile demo).
2. Rate limiting + account lockout στο login (security awareness, μικρό κόστος).
3. CI + branch protection (μόλις ανέβει στο GitHub).

**Ίσως, αν περισσεύει χρόνος (🟡):**
- SignalR live θέσεις (δυνατό tech σημείο).
- Testcontainers integration tests (ενισχύει το testing κεφάλαιο).
- OWASP ZAP scan + μικρό branding πέρασμα (ωραιότερη αναφορά/demo).

**Όλα τα υπόλοιπα:** μετά την πτυχιακή, όταν γίνει προϊόν.
