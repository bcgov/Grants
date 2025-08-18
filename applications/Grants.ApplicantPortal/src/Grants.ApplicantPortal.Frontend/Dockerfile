# Use the official Node.js image as the base image
FROM node:22-slim AS build

# Set the working directory
WORKDIR /app

# Copy package.json and package-lock.json
COPY package.json package-lock.json ./

# Install dependencies
RUN npm install

# Copy the rest of the application code
COPY . .

# Build the Angular application
RUN echo "Building..." && npm run build

# The final stage now runs the appropriate server
FROM node:22-slim AS runtime

# Set the working directory
WORKDIR /app

# Copy the built application from the build stage
COPY --from=build /app/dist ./dist
COPY --from=build /app/package*.json ./
COPY --from=build /app/server.js ./

# Install deployment dependencies only
RUN npm install --omit=dev

# Set environment variables
ENV PORT=4000
ENV BACKEND_SERVICE_URL=http://backend:5100
ENV ENABLE_API_PROXY=false

# Expose port 4000
EXPOSE 4000

# Start the SPA server directly
CMD ["node", "server.js"]