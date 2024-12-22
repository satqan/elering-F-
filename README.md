# Elering Energy Dashboard

## Dependencies

* Node.js 18.0+
* .NET SDK 8.0+
* Docker (optional)

## Getting Started

1. Clone the repository
```bash
git clone [your-repository-url]
cd elering-dashboard-fsharp
```

2. Install dependencies
```bash
npm install
dotnet tool restore
```

3. Run the application
```bash
npm run start:dev
# start client and server separately
# npm run start:client
# npm run start:server
```

4. Open http://localhost:8080 in your browser

## Docker Deployment

Build and run the application using Docker:

```bash
# Build the image
docker build -t elering-dashboard .

# Run the container
docker run --name elering-app --rm -p 8080:8080 elering-dashboard
```

### develop in docker container instead:

note: this has some caveats, because docker does not have access to filesystem updates for hot reloading

```bash
# build the dev image
docker build -t dev -f Dockerfile-dev .

# run 3 instances of the dev image:
# - F# to js compiler in watch mode
# - client on port 8080
# - server on port 5000

# start compiling F# to JS in watch mode
docker run --rm -ti -v ".:/app" dev -c "npm run start:client-docker"

# make changes to src/Client/Client.fs and see changes in src/Client/Client.fs.js

# start the client on port 8080
docker run --rm -ti -p 8080:8080 -v ".:/app" dev -c "npm run start:vite"

# open http://localhost:8080/ in the browser to see the client

# start the server on port 5000
docker run --rm -ti -p 5000:5000 -v ".:/app" dev -c "npm run start:server"

```
