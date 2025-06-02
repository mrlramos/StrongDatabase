#!/bin/bash

echo "========================================"
echo "StrongDatabase - Post-Up Configuration"
echo "========================================"
echo
echo "This script configures replication after initial startup"
echo

echo "[1/4] Copying custom pg_hba.conf to primary database..."
docker cp ./docker/primary/pg_hba.conf primary-db:/var/lib/postgresql/data/pg_hba.conf
if [ $? -ne 0 ]; then
    echo "ERROR: Failed to copy pg_hba.conf"
    exit 1
fi

echo "[2/4] Reloading PostgreSQL configuration..."
docker exec primary-db psql -U primary_user -d strongdatabase_primary -c "SELECT pg_reload_conf();"
if [ $? -ne 0 ]; then
    echo "ERROR: Failed to reload configuration"
    exit 1
fi

echo "[3/4] Restarting replica and standby containers..."
docker-compose restart standby-db replica1-db replica2-db
if [ $? -ne 0 ]; then
    echo "ERROR: Failed to restart containers"
    exit 1
fi

echo "[4/4] Waiting for containers to stabilize..."
sleep 10

echo
echo "========================================"
echo "Configuration completed successfully!"
echo "All databases should now be replicating properly."
echo "========================================" 