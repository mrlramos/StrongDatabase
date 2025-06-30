-- StrongDatabase - Single Database Schema
-- This will be replicated to all nodes

\c strongdatabase;

-- Customer Table
CREATE TABLE "Customers" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(100) NOT NULL,
    "Email" VARCHAR(100) NOT NULL UNIQUE
);

-- Product Table
CREATE TABLE "Products" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(100) NOT NULL,
    "Price" NUMERIC(10,2) NOT NULL
);

-- Order Table
CREATE TABLE "Orders" (
    "Id" SERIAL PRIMARY KEY,
    "CustomerId" INTEGER NOT NULL REFERENCES "Customers"("Id"),
    "ProductId" INTEGER NOT NULL REFERENCES "Products"("Id"),
    "Quantity" INTEGER NOT NULL,
    "OrderDate" TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Sample data
INSERT INTO "Customers" ("Name", "Email") VALUES
  ('John Silva', 'john@email.com'),
  ('Mary Johnson', 'mary@email.com'),
  ('Robert Brown', 'robert@email.com'),
  ('Sarah Davis', 'sarah@email.com');

INSERT INTO "Products" ("Name", "Price") VALUES
  ('Gaming Laptop', 2500.00),
  ('Wireless Mouse', 75.00),
  ('Mechanical Keyboard', 150.00),
  ('4K Monitor', 450.00),
  ('USB-C Hub', 89.99);

INSERT INTO "Orders" ("CustomerId", "ProductId", "Quantity") VALUES
  (1, 1, 1),  -- John bought 1 Gaming Laptop
  (2, 2, 2),  -- Mary bought 2 Wireless Mouse
  (3, 3, 1),  -- Robert bought 1 Mechanical Keyboard
  (4, 4, 1),  -- Sarah bought 1 4K Monitor
  (1, 5, 1),  -- John bought 1 USB-C Hub
  (2, 3, 1);  -- Mary bought 1 Mechanical Keyboard

-- Replication user for streaming replication
DO $$
BEGIN
   IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'replicator') THEN
      CREATE ROLE replicator WITH REPLICATION LOGIN PASSWORD 'replicator_pass';
      GRANT CONNECT ON DATABASE strongdatabase TO replicator;
   END IF;
END$$; 