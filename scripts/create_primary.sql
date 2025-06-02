-- Grant privileges to the existing user
GRANT ALL PRIVILEGES ON DATABASE strongdatabase_primary TO primary_user;

\c strongdatabase_primary;

-- Customer Table
CREATE TABLE cliente (
    id SERIAL PRIMARY KEY,
    nome VARCHAR(100) NOT NULL,
    email VARCHAR(100) NOT NULL UNIQUE
);

-- Product Table
CREATE TABLE produto (
    id SERIAL PRIMARY KEY,
    nome VARCHAR(100) NOT NULL,
    preco NUMERIC(10,2) NOT NULL
);

-- Order Table
CREATE TABLE compra (
    id SERIAL PRIMARY KEY,
    cliente_id INTEGER NOT NULL REFERENCES cliente(id),
    produto_id INTEGER NOT NULL REFERENCES produto(id),
    quantidade INTEGER NOT NULL,
    data_compra TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Sample data
INSERT INTO cliente (nome, email) VALUES
  ('John Silva', 'john@email.com'),
  ('Mary Johnson', 'mary@email.com'),
  ('Robert Brown', 'robert@email.com'),
  ('Sarah Davis', 'sarah@email.com');

INSERT INTO produto (nome, preco) VALUES
  ('Gaming Laptop', 2500.00),
  ('Wireless Mouse', 75.00),
  ('Mechanical Keyboard', 150.00),
  ('4K Monitor', 450.00),
  ('USB-C Hub', 89.99);

INSERT INTO compra (cliente_id, produto_id, quantidade) VALUES
  (1, 1, 1),  -- John bought 1 Gaming Laptop
  (2, 2, 2),  -- Mary bought 2 Wireless Mouse
  (3, 3, 1),  -- Robert bought 1 Mechanical Keyboard
  (4, 4, 1),  -- Sarah bought 1 4K Monitor
  (1, 5, 1),  -- John bought 1 USB-C Hub
  (2, 3, 1);  -- Mary bought 1 Mechanical Keyboard

-- Replication user
DO $$
BEGIN
   IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'replicator') THEN
      CREATE ROLE replicator WITH REPLICATION LOGIN PASSWORD 'replicator_pass';
   END IF;
END$$; 