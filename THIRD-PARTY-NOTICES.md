# Third-party notices

Το GymBooking χρησιμοποιεί τις παρακάτω βιβλιοθήκες και εικόνες Docker τρίτων. Οι άδειες προέρχονται από τα στοιχεία των πακέτων (nuget.org, `package.json` κάθε πακέτου npm) και από τα αποθετήρια των έργων· το πλήρες κείμενο κάθε άδειας περιλαμβάνεται στο αντίστοιχο πακέτο.

Δεν διανέμονται μαζί με τον κώδικα: οι εξαρτήσεις εγκαθίστανται από τα `*.csproj` και το `frontend/package-lock.json`.

## Backend (NuGet)

| Πακέτο | Έκδοση | Άδεια |
|---|---|---|
| Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.9 | MIT |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 10.0.9 | MIT |
| Microsoft.AspNetCore.OpenApi | 10.0.9 | MIT |
| Microsoft.EntityFrameworkCore.Design | 10.0.2 | MIT |
| Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore | 10.0.2 | MIT |
| Microsoft.Extensions.Identity.Stores | 10.0.9 | MIT |
| Microsoft.OpenApi | 2.9.0 | MIT |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.2 | PostgreSQL License |
| Scalar.AspNetCore | 2.16.10 | MIT |
| Serilog.AspNetCore | 10.0.0 | Apache-2.0 |
| Serilog.Sinks.File | 7.0.0 | Apache-2.0 |

**Μόνο για δοκιμές:**

| Πακέτο | Έκδοση | Άδεια |
|---|---|---|
| Microsoft.AspNetCore.Mvc.Testing | 10.0.9 | MIT |
| Microsoft.EntityFrameworkCore.InMemory | 10.0.9 | MIT |
| Microsoft.NET.Test.Sdk | 17.14.1 | MIT |
| Testcontainers.PostgreSql | 4.13.0 | MIT |
| xunit | 2.9.3 | Apache-2.0 |
| xunit.runner.visualstudio | 3.1.4 | Apache-2.0 |
| coverlet.collector | 6.0.4 | MIT |

## Frontend (npm)

**Χρόνος εκτέλεσης (μέρος των εφαρμογών):**

| Πακέτο | Έκδοση | Άδεια |
|---|---|---|
| @angular/cdk | 21.2.14 | MIT |
| @angular/common | 21.2.17 | MIT |
| @angular/compiler | 21.2.17 | MIT |
| @angular/core | 21.2.17 | MIT |
| @angular/forms | 21.2.17 | MIT |
| @angular/material | 21.2.14 | MIT |
| @angular/platform-browser | 21.2.17 | MIT |
| @angular/router | 21.2.17 | MIT |
| rxjs | 7.8.2 | Apache-2.0 |

**Εργαλεία ανάπτυξης, build και δοκιμών:**

| Πακέτο | Έκδοση | Άδεια |
|---|---|---|
| @analogjs/vite-plugin-angular, @analogjs/vitest-angular | 2.1.3 | MIT |
| @angular-devkit/core, @angular-devkit/schematics, @angular/build, @angular/cli, @schematics/angular | 21.2.18 | MIT |
| @angular/compiler-cli, @angular/language-service | 21.2.17 | MIT |
| @nx/angular, @nx/eslint, @nx/eslint-plugin, @nx/js, @nx/vite, @nx/vitest, @nx/web, @nx/workspace, nx | 23.0.1 | MIT |
| @eslint/js, eslint | 9.39.4 | MIT |
| @typescript-eslint/utils, typescript-eslint | 8.62.1 | MIT |
| angular-eslint | 21.4.0 | MIT |
| eslint-config-prettier | 10.1.8 | MIT |
| prettier | 3.6.2 | MIT |
| @oxc-project/runtime | 0.115.0 | MIT |
| @swc/helpers | 0.5.23 | Apache-2.0 |
| @tailwindcss/postcss, tailwindcss | 4.3.2 | MIT |
| postcss | 8.5.16 | MIT |
| @types/node | 22.20.0 | MIT |
| @vitest/coverage-v8, @vitest/ui, vitest | 4.1.9 | MIT |
| vite | 8.1.3 | MIT |
| jsdom | 27.4.0 | MIT |
| tslib | 2.8.1 | 0BSD |
| typescript | 5.9.3 | Apache-2.0 |

## Εικόνες Docker

| Εικόνα | Χρήση | Άδεια λογισμικού |
|---|---|---|
| `postgres:16`, `postgres:16-alpine` | Βάση δεδομένων | PostgreSQL License |
| `changemakerstudiosus/papercut-smtp` | Τοπικός εξυπηρετητής email | Apache-2.0 |
| `mcr.microsoft.com/dotnet/sdk:10.0`, `mcr.microsoft.com/dotnet/aspnet:10.0` | Build και εκτέλεση του API | MIT (.NET) |
| `node:20-alpine` | Build των εφαρμογών | MIT (Node.js) |
| `nginx:alpine` | Σερβίρισμα των εφαρμογών | BSD-2-Clause (nginx) |

Οι εικόνες περιέχουν επιπλέον πακέτα του λειτουργικού συστήματος με τις δικές τους άδειες.
