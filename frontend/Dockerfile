# Parametrized frontend image — χτίζει είτε το customer είτε το staff app (ARG APP) και το σερβίρει
# ως static site μέσω nginx. Χρησιμοποιείται από το docker-compose.full.yml (build context = ./frontend).

# --- Build stage ---
FROM node:20-alpine AS build
ARG APP=staff
WORKDIR /app

# Restore με layer caching: πρώτα μόνο τα manifests.
COPY package.json package-lock.json ./
RUN npm ci

# Το υπόλοιπο workspace (apps + libs + nx config) και build με το "docker" configuration
# (apiBaseUrl -> http://localhost:8080, βλ. environment.docker.ts).
COPY . .
RUN npx nx build ${APP} --configuration=docker

# Το @angular/build:application βγάζει τα browser assets στο dist/apps/<app>/browser.
# Τα μαζεύουμε σε σταθερό path για το runtime stage (fallback αν δεν υπάρχει /browser).
RUN cp -r dist/apps/${APP}/browser /site || cp -r dist/apps/${APP} /site

# --- Runtime stage ---
FROM nginx:alpine AS runtime
COPY --from=build /site /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
