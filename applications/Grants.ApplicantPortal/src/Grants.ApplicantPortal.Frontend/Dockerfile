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
RUN npm run build

# The final stage now only runs the Angular Universal server
FROM node:22-slim AS runtime

# Set the working directory
WORKDIR /app

# Copy the built application from the build stage
COPY --from=build /app /app

# Install production dependencies only
RUN npm install --omit=dev

# Set environment variables for the backend service
# This can be overridden at deployment time
ENV PORT=4000
ENV BACKEND_SERVICE_URL=http://backend-service

# Expose port 4000 for the Angular Universal server
EXPOSE 4000

# Start the Angular Universal server
CMD ["npm", "run", "serve:ssr"]