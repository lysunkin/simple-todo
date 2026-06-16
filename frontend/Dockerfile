# --- Build stage ---
FROM node:20-alpine AS build
WORKDIR /app

# Install dependencies first (layer cache friendly)
COPY package.json package-lock.json* ./
RUN npm ci --silent

# Copy source and build
COPY . .
# VITE_API_BASE_URL is intentionally empty: nginx will proxy /api to the backend
RUN npm run build

# --- Runtime stage ---
FROM nginx:alpine AS runtime

# Remove default nginx config
RUN rm /etc/nginx/conf.d/default.conf

COPY nginx.conf /etc/nginx/conf.d/app.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80

# nginx runs as root by default in the official image; this is standard for
# port 80. In a hardened environment you'd use a non-root nginx setup.
CMD ["nginx", "-g", "daemon off;"]
