# ---------- STAGE 1: BUILD ----------
FROM node:20-alpine AS build
WORKDIR /app

# Instalăm dependențele (cache eficient)
COPY package*.json ./
RUN npm ci

# Copiem codul și construim
COPY . .

# VITE_API_URL = adresa la care BROWSERUL vede API-ul (localhost:8080, portul expus)
# Se injectează la build ca argument
ARG VITE_API_URL=http://localhost:8080
ENV VITE_API_URL=$VITE_API_URL
RUN npm run build

# ---------- STAGE 2: SERVE (nginx) ----------
FROM nginx:alpine AS serve
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]