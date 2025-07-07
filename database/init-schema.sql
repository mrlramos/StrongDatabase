-- StrongDatabase - MySQL Schema
-- This will be replicated to all nodes

USE strongdatabase;

-- Customer Table
CREATE TABLE `Customers` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `Name` VARCHAR(100) NOT NULL,
    `Email` VARCHAR(100) NOT NULL UNIQUE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Product Table
CREATE TABLE `Products` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `Name` VARCHAR(100) NOT NULL,
    `Price` DECIMAL(10,2) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Order Table
CREATE TABLE `Orders` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `CustomerId` INT NOT NULL,
    `ProductId` INT NOT NULL,
    `Quantity` INT NOT NULL,
    `OrderDate` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (`CustomerId`) REFERENCES `Customers`(`Id`),
    FOREIGN KEY (`ProductId`) REFERENCES `Products`(`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Sample data
INSERT INTO `Customers` (`Name`, `Email`) VALUES
  ('John Silva', 'john@email.com'),
  ('Mary Johnson', 'mary@email.com'),
  ('Robert Brown', 'robert@email.com'),
  ('Sarah Davis', 'sarah@email.com');

INSERT INTO `Products` (`Name`, `Price`) VALUES
  ('Gaming Laptop', 2500.00),
  ('Wireless Mouse', 75.00),
  ('Mechanical Keyboard', 150.00),
  ('4K Monitor', 450.00),
  ('USB-C Hub', 89.99);

INSERT INTO `Orders` (`CustomerId`, `ProductId`, `Quantity`) VALUES
  (1, 1, 1),  -- John bought 1 Gaming Laptop
  (2, 2, 2),  -- Mary bought 2 Wireless Mouse
  (3, 3, 1),  -- Robert bought 1 Mechanical Keyboard
  (4, 4, 1),  -- Sarah bought 1 4K Monitor
  (1, 5, 1),  -- John bought 1 USB-C Hub
  (2, 3, 1);  -- Mary bought 1 Mechanical Keyboard

-- Create replication user
CREATE USER IF NOT EXISTS 'replicator'@'%' IDENTIFIED BY 'replicator_pass';
GRANT REPLICATION SLAVE ON *.* TO 'replicator'@'%';
GRANT SELECT ON strongdatabase.* TO 'replicator'@'%';
FLUSH PRIVILEGES; 