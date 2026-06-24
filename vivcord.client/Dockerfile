#Build angular app
FROM node:24-slim AS build

WORKDIR /app

# Copy dependency manifests
COPY package.json package-lock.json ./

# Install dependencies
RUN npm ci

# Copy rest
COPY . .

# Build Angular app in production mode
RUN npx ng build --configuration=production

#Serve Angular app with Nginx
FROM nginx:alpine AS runtime

COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copy Angular build output from build stage
COPY --from=build /app/dist/vivcord.client/browser /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
