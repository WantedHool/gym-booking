
FROM node:20-alpine AS build
ARG APP=staff
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npx nx build ${APP} --configuration=docker

RUN cp -r dist/apps/${APP}/browser /site || cp -r dist/apps/${APP} /site

FROM nginx:alpine AS runtime
COPY --from=build /site /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
