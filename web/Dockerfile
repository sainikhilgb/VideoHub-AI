FROM node:20-alpine

# Set working directory
WORKDIR /app

# Copy package.json and package-lock.json
COPY package*.json ./

# Install dependencies
RUN npm ci

# Copy the rest of the application code
COPY . .

# Ensure /app is owned by the node user
RUN chown -R node:node /app

# Switch to the node user
USER node

# Expose Vite's default dev server port
EXPOSE 5173

# Command to run Vite in dev mode, bound to all network interfaces
CMD ["npm", "run", "dev", "--", "--host"]
