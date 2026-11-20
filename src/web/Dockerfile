# Build context: src/web
FROM node:24-alpine AS build
WORKDIR /web
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
ARG VITE_AUTH_MODE=dev
ARG VITE_API_BASE_URL=
ARG VITE_ENTRA_TENANT_ID=
ARG VITE_ENTRA_CLIENT_ID=
ARG VITE_ENTRA_API_SCOPE=
RUN npm run build

FROM nginx:1.29-alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /web/dist /usr/share/nginx/html
EXPOSE 80
