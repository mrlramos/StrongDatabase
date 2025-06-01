#!/bin/bash
set -e

if [ ! -s "$PGDATA/PG_VERSION" ]; then
  echo "==> Aguardando o primary-db ficar disponível..."
  until pg_isready -h primary-db -p 5432; do
    sleep 2
  done
  echo "==> Clonando dados do primário para replica2..."
  pg_basebackup -h primary-db -D "$PGDATA" -U replicator -v -P --wal-method=stream
  echo "primary_conninfo = 'host=primary-db port=5432 user=replicator password=replicator_pass'" >> "$PGDATA/postgresql.auto.conf"
  touch "$PGDATA/standby.signal"
  chown -R postgres:postgres "$PGDATA"
fi

exec docker-entrypoint.sh postgres 