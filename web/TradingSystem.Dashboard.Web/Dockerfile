FROM node:24-alpine AS build
WORKDIR /app
COPY web/TradingSystem.Dashboard.Web/package*.json ./
RUN npm ci
COPY web/TradingSystem.Dashboard.Web/ ./
ARG VITE_API_BASE_URL=/api/v1
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
RUN npm run build

FROM nginx:1.28-alpine AS final
COPY web/TradingSystem.Dashboard.Web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 8080
