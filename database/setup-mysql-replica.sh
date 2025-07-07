#!/bin/bash
set -e

echo "🔄 [MySQL Replica Setup] Iniciando configuração da replica..."

# Aguardar o MySQL estar pronto
echo "⏳ [MySQL Replica Setup] Aguardando MySQL ficar disponível..."
until mysqladmin ping -h"$MYSQL_MASTER_HOST" --silent; do
    echo "⏳ [MySQL Replica Setup] Aguardando master MySQL..."
    sleep 2
done

echo "✅ [MySQL Replica Setup] Master MySQL disponível!"

# Aguardar um pouco mais para garantir que está completamente inicializado
sleep 10

# Configurar a replicação
echo "🔧 [MySQL Replica Setup] Configurando replicação..."

mysql -u root -p"$MYSQL_ROOT_PASSWORD" <<-EOSQL
    -- Parar qualquer replicação existente
    STOP SLAVE;
    RESET SLAVE ALL;
    
    -- Configurar master
    CHANGE MASTER TO 
        MASTER_HOST='$MYSQL_MASTER_HOST',
        MASTER_PORT=$MYSQL_MASTER_PORT,
        MASTER_USER='$MYSQL_REPLICATION_USER',
        MASTER_PASSWORD='$MYSQL_REPLICATION_PASSWORD',
        MASTER_AUTO_POSITION=1;
    
    -- Iniciar replicação
    START SLAVE;
    
    -- Verificar status
    SHOW SLAVE STATUS\G
EOSQL

echo "✅ [MySQL Replica Setup] Replicação configurada com sucesso!"

# Manter o container rodando
echo "🚀 [MySQL Replica Setup] Iniciando MySQL..."
exec mysqld --defaults-file=/etc/mysql/conf.d/mysql-replica.cnf 