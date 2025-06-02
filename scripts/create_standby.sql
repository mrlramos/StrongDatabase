-- Grant privileges to the existing user
GRANT ALL PRIVILEGES ON DATABASE strongdatabase_standby TO standby_user;

\c strongdatabase_standby;

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

-- Sample data (will be replaced by replication from primary)
INSERT INTO cliente (nome, email) VALUES
  ('John Silva', 'john@email.com'),
  ('Mary Johnson', 'mary@email.com');

INSERT INTO produto (nome, preco) VALUES
  ('Gaming Laptop', 2500.00),
  ('Wireless Mouse', 75.00);

INSERT INTO compra (cliente_id, produto_id, quantidade) VALUES
  (1, 1, 1),
  (2, 2, 2); 