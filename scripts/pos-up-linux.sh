#!/bin/bash
# Script para ajustar replicação após docker-compose up

# Aguarda o primary-db subir
until docker ps --filter "name=primary-db" --filter "status=running" | grep primary-db; do
  echo "Aguardando o primary-db iniciar..."
  sleep 2
done

# Copia o pg_hba.conf customizado
docker cp ./docker/primary/pg_hba.conf primary-db:/var/lib/postgresql/data/pg_hba.conf

# Reinicia o primary-db para aplicar o novo pg_hba.conf
docker restart primary-db

# Reinicia as réplicas e standby para tentarem novamente a replicação
docker-compose restart standby-db replica1-db replica2-db

echo "Ambiente de replicação ajustado com sucesso!" 