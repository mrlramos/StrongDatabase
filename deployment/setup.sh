#!/bin/bash

echo "🚀 StrongDatabase - Simplified Setup"
echo "===================================="
echo

# Function to check if command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Check dependencies
echo "🔍 Checking dependencies..."
if ! command_exists docker; then
    echo "❌ Docker not found. Please install Docker first."
    exit 1
fi

if ! command_exists docker-compose; then
    echo "❌ Docker Compose not found. Please install Docker Compose first."
    exit 1
fi

echo "✅ Dependencies OK!"
echo

# Clean up previous runs
echo "🧹 Cleaning up previous runs..."
cd ..
docker-compose down -v --remove-orphans 2>/dev/null || true
docker system prune -f
echo "✅ Cleanup complete!"
echo

# Make scripts executable
echo "🔧 Setting up permissions..."
chmod +x database/setup-replica.sh
echo "✅ Permissions set!"
echo

# Start the simplified stack
echo "🚀 Starting StrongDatabase (simplified)..."
docker-compose up -d

echo
echo "⏳ Waiting for services to start..."
sleep 30

# Check service health
echo "🔍 Checking service health..."

# Check Primary
echo -n "Primary DB: "
if docker exec primary-db pg_isready -U postgres -d strongdatabase >/dev/null 2>&1; then
    echo "✅ Healthy"
else
    echo "❌ Unhealthy"
fi

# Check API
echo -n "API: "
if curl -s http://localhost:5000/health >/dev/null 2>&1; then
    echo "✅ Healthy"
else
    echo "❌ Unhealthy"
fi

# Check Elasticsearch
echo -n "Elasticsearch: "
if curl -s http://localhost:9200 >/dev/null 2>&1; then
    echo "✅ Healthy"
else
    echo "❌ Unhealthy"
fi

echo
echo "🎉 Setup Complete!"
echo "=================="
echo
echo "📋 Available URLs:"
echo "   🌐 API: http://localhost:5000"
echo "   📖 Swagger: http://localhost:5000/swagger"
echo "   🏥 Health: http://localhost:5000/health"
echo "   📊 Kibana: http://localhost:5601"
echo "   🔍 Elasticsearch: http://localhost:9200"
echo
echo "📝 Quick Tests:"
echo "   curl http://localhost:5000/api/customer"
echo "   curl http://localhost:5000/health"
echo
echo "📦 View containers: docker-compose ps"
echo "📜 View logs: docker-compose logs -f" 