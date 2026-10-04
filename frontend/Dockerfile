# ProCargo portal image: builds the React app and serves it with nginx, which also forwards /api to the API.
#   docker build -t procargo-portal frontend
FROM node:22-alpine AS build
WORKDIR /app
COPY package.json package-lock.json* ./
RUN npm install --no-audit --no-fund
COPY . .
# The portal calls the API on the same host; nginx forwards /api/ to the API container.
ENV VITE_API_BASE_URL=/api/v1
RUN npm run build

FROM nginx:1.29-alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 8080
