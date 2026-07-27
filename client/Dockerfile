FROM node:20-alpine

WORKDIR /app

# Copy package files first for faster caching
COPY package*.json ./
RUN npm install

# Copy the rest of your Angular app code
COPY . .

EXPOSE 4200

# Start Angular dev server on all network interfaces
CMD ["npm", "start"]