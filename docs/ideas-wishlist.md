# Ιδέες & Εκκρεμότητες — προσωπική λίστα

> **Σκοπός:** πρόχειρη, ζωντανή λίστα με ό,τι θέλω να φτιάξω/βελτιώσω **κάποια στιγμή**. Ρίχνω εδώ κάθε ιδέα μόλις μου έρθει, χωρίς να μπλέκομαι με το scope της πτυχιακής τώρα.
>
> Για δομημένη ανάλυση (τι αξίζει για την πτυχιακή vs προϊόν) → [2026-07-14-backlog-deferred-items.md](2026-07-14-backlog-deferred-items.md).
>
> **Status:** 🔲 προς υλοποίηση · 🚧 σε εξέλιξη · ✅ έγινε

---

## Λίστα

### 🔲 Global loading / splash overlay όταν περιμένουμε τον server
**Τι:** Όταν το frontend περιμένει απάντηση από τον server, να εμφανίζεται ένα splash/overlay που μπλοκάρει τις αλληλεπιδράσεις (δεν μπορεί ο χρήστης να πατήσει κουμπιά ή να κάνει πράγματα μέχρι να έρθει η απάντηση).

**Προτεινόμενο design (συμφωνήθηκε):**
- **`LoadingService`** (στο `ui` lib): signal-based counter — `begin()`/`done()` κάνουν increment/decrement· `isLoading = computed(() => count() > 0)`. Counter (όχι boolean) ώστε πολλαπλά ταυτόχρονα requests να μη σβήνουν πρόωρα το overlay.
- **`loadingInterceptor`** (στο `auth` lib): τρέχει **μόνο για mutations** (`POST/PUT/PATCH/DELETE`). `begin()` στην αρχή, `done()` σε `finalize()` (καλύπτει success + error/cancel, δεν κολλάει ποτέ). Escape hatch: `HttpContext` token `SKIP_LOADING` για εξαιρέσεις (π.χ. μελλοντικό polling).
- **`LoadingOverlay` component** (στο `ui` lib): full-screen `position: fixed`, dimmed backdrop, ψηλό `z-index`, πιάνει όλο το viewport ώστε να μην πατιέται τίποτα από κάτω. Material `mat-progress-spinner` (indeterminate) στο κέντρο. **Show-delay ~250ms** ώστε να μην αναβοσβήνει σε γρήγορα calls. Accessibility: `role="alert"`, `aria-busy="true"`.
- **Wiring:** `<lib-loading-overlay />` πάνω από το `<router-outlet>` στα δύο `app.html` (customer + staff)· ο interceptor στο `withInterceptors([...])` και στα δύο `app.config.ts`.
- **Tests:** `LoadingService` (counter logic) + interceptor (mutation → begin/done, GET → τίποτα, error → done).

**Σημείωση:** τα GET μένουν με τα υπάρχοντα `list-skeleton` — το overlay είναι μόνο για mutations (κράτηση, ακύρωση, αποθήκευση κ.λπ.).

---

_(πρόσθεσε νέες ιδέες από κάτω)_
