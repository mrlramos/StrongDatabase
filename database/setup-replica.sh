#!/bin/bash
set -e

echo "🔧 Setting up PostgreSQL replica..."

# Wait for primary to be ready
echo "⏳ Waiting for primary database..."
until pg_isready -h ${POSTGRES_PRIMARY_HOST} -p ${POSTGRES_PRIMARY_PORT} -U ${POSTGRES_REPLICATION_USER}; do
    echo "   Waiting for primary..."
    sleep 2
done

echo "✅ Primary is ready!"

# If this is a fresh setup, clone data from primary
if [ ! -s "$PGDATA/PG_VERSION" ]; then
    echo "🔄 Cloning data from primary..."
    
    # Create replication user in primary (if it doesn't exist)
    export PGPASSWORD=${POSTGRES_PASSWORD}
    psql -h ${POSTGRES_PRIMARY_HOST} -p ${POSTGRES_PRIMARY_PORT} -U postgres -d strongdatabase -c "
        DO \$\$
        BEGIN
            IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = '${POSTGRES_REPLICATION_USER}') THEN
                CREATE ROLE ${POSTGRES_REPLICATION_USER} WITH REPLICATION LOGIN PASSWORD '${POSTGRES_REPLICATION_PASSWORD}';
            END IF;
        END\$\$;
    " || echo "Replication user might already exist or primary not ready yet"
    
    # Perform base backup
    export PGPASSWORD=${POSTGRES_REPLICATION_PASSWORD}
    pg_basebackup -h ${POSTGRES_PRIMARY_HOST} -p ${POSTGRES_PRIMARY_PORT} -D ${PGDATA} -U ${POSTGRES_REPLICATION_USER} -v -P -W -R
    
    # Set up replica configuration
    echo "primary_conninfo = 'host=${POSTGRES_PRIMARY_HOST} port=${POSTGRES_PRIMARY_PORT} user=${POSTGRES_REPLICATION_USER} password=${POSTGRES_REPLICATION_PASSWORD} application_name=${HOSTNAME}'" >> ${PGDATA}/postgresql.auto.conf
    
    # Create standby signal
    touch ${PGDATA}/standby.signal
    
    # Set permissions
    chown -R postgres:postgres ${PGDATA}
    chmod 700 ${PGDATA}
    
    echo "✅ Replica setup complete!"
fi

# Start PostgreSQL
exec docker-entrypoint.sh postgres -c config_file=/etc/postgresql/postgresql.conf 