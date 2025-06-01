-- Concede privilégios ao usuário já existente
GRANT ALL PRIVILEGES ON DATABASE strongdatabase_replica1 TO replica1_user;

\c strongdatabase_replica1;

-- Tabela de Clientes
CREATE TABLE cliente (
    id SERIAL PRIMARY KEY,
    nome VARCHAR(100) NOT NULL,
    email VARCHAR(100) NOT NULL UNIQUE
);

-- Tabela de Produtos
CREATE TABLE produto (
    id SERIAL PRIMARY KEY,
    nome VARCHAR(100) NOT NULL,
    preco NUMERIC(10,2) NOT NULL
);

-- Tabela de Compras
CREATE TABLE compra (
    id SERIAL PRIMARY KEY,
    cliente_id INTEGER NOT NULL REFERENCES cliente(id),
    produto_id INTEGER NOT NULL REFERENCES produto(id),
    quantidade INTEGER NOT NULL,
    data_compra TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Dados de exemplo
INSERT INTO cliente (nome, email) VALUES
  ('João Silva', 'joao@email.com'),
  ('Maria Souza', 'maria@email.com');

INSERT INTO produto (nome, preco) VALUES
  ('Notebook', 3500.00),
  ('Mouse', 80.00);

INSERT INTO compra (cliente_id, produto_id, quantidade) VALUES
  (1, 1, 1),
  (2, 2, 2); 