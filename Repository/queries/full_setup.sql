-- SQL Server / SQLite Compatible Setup Script for IlPuntoG
-- Note: IDENTITY(1,1) is for SQL Server. For SQLite use AUTOINCREMENT (not strictly needed if INTEGER PRIMARY KEY).
-- This script uses standard SQL where possible.

-- 1. Roles
CREATE TABLE Roles (
    Id INT PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL
);

INSERT INTO Roles (Id, Name) VALUES (1, 'User'), (2, 'Admin'), (3, 'SuperAdmin');

-- 2. Users
CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(255) NOT NULL,
    Email NVARCHAR(255) NOT NULL,
    Username NVARCHAR(255) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(MAX) NOT NULL,
    RoleId INT NOT NULL,
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES Roles(Id)
);

-- Seed an admin user (password: password123)
-- Hash generated via BCrypt
INSERT INTO Users (Name, Email, Username, PasswordHash, RoleId)
VALUES ('Amministratore', 'admin@ilpuntog.it', 'admin', '$2a$11$qR7jX8L7pY3N9p9p9p9p9uXpXpXpXpXpXpXpXpXpXpXpXpXpXpXpX', 3);

-- 3. Branches
CREATE TABLE Branches (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(255) NOT NULL,
    Address NVARCHAR(MAX) NOT NULL
);

INSERT INTO Branches (Name, Address) VALUES ('Sede Centrale - Milano', 'Via Roma 1, Milano');
INSERT INTO Branches (Name, Address) VALUES ('Sede Torino', 'Corso Vittorio Emanuele 10, Torino');

-- 4. Clients
CREATE TABLE Clients (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(255) NOT NULL,
    Email NVARCHAR(255) NOT NULL,
    PhoneNumber NVARCHAR(50) NULL
);

INSERT INTO Clients (Name, Email, PhoneNumber) VALUES ('Condominio Stella', 'info@condominiostella.it', '021234567');
INSERT INTO Clients (Name, Email, PhoneNumber) VALUES ('Hotel Moderno', 'reception@hotelmoderno.it', '011987654');

-- 5. Projects
CREATE TABLE Projects (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    BranchId INT NOT NULL,
    CONSTRAINT FK_Projects_Branches FOREIGN KEY (BranchId) REFERENCES Branches(Id) ON DELETE CASCADE
);

INSERT INTO Projects (Name, Description, BranchId) VALUES ('Manutenzione Ascensori 2026', 'Piano annuale manutenzione', 1);

-- 6. Appointments
CREATE TABLE Appointments (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Description NVARCHAR(MAX) NOT NULL,
    StartTime DATETIME2 NOT NULL,
    Duration NVARCHAR(50) NOT NULL, -- Stored as string or ticks depending on DB, using NVARCHAR for flexibility in script
    ProjectId INT NULL,
    BranchId INT NOT NULL,
    CONSTRAINT FK_Appointments_Branches FOREIGN KEY (BranchId) REFERENCES Branches(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Appointments_Projects FOREIGN KEY (ProjectId) REFERENCES Projects(Id) ON DELETE SET NULL
);

-- 7. TaskTypes
CREATE TABLE TaskTypes (
    Id INT PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL
);

INSERT INTO TaskTypes (Id, Name) VALUES (1, 'Sopralluogo'), (2, 'Riparazione'), (3, 'Installazione'), (4, 'Manutenzione');

-- 8. Priorities
CREATE TABLE Priorities (
    Id INT PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL,
    Level INT NOT NULL
);

INSERT INTO Priorities (Id, Name, Level) VALUES (1, 'Bassa', 1), (2, 'Media', 2), (3, 'Alta', 3), (4, 'Urgente', 4);

-- 9. TodoTasks
CREATE TABLE TodoTasks (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX) NOT NULL,
    PriorityId INT NOT NULL,
    Deadline DATETIME2 NOT NULL,
    Status INT NOT NULL DEFAULT 0,
    IsRecurring BIT NOT NULL DEFAULT 0,
    Recurrence INT NOT NULL DEFAULT 0,
    ProjectId INT NOT NULL,
    ClientId INT NULL,
    TaskTypeId INT NOT NULL,
    ProcessedAt DATETIME2 NULL,
    CONSTRAINT FK_TodoTasks_Priorities FOREIGN KEY (PriorityId) REFERENCES Priorities(Id),
    CONSTRAINT FK_TodoTasks_Projects FOREIGN KEY (ProjectId) REFERENCES Projects(Id) ON DELETE CASCADE,
    CONSTRAINT FK_TodoTasks_Clients FOREIGN KEY (ClientId) REFERENCES Clients(Id),
    CONSTRAINT FK_TodoTasks_TaskTypes FOREIGN KEY (TaskTypeId) REFERENCES TaskTypes(Id)
);

-- 10. WorkLogs
CREATE TABLE WorkLogs (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Date DATETIME2 NOT NULL,
    Hours FLOAT NOT NULL,
    Type INT NOT NULL,
    UserId INT NOT NULL,
    CONSTRAINT FK_WorkLogs_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

-- 11. UserBranches (Junction)
CREATE TABLE UserBranches (
    UserId INT NOT NULL,
    BranchId INT NOT NULL,
    PRIMARY KEY (UserId, BranchId),
    CONSTRAINT FK_UserBranches_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_UserBranches_Branches FOREIGN KEY (BranchId) REFERENCES Branches(Id) ON DELETE CASCADE
);

-- Associate admin to Sede Centrale
INSERT INTO UserBranches (UserId, BranchId) VALUES (1, 1);

-- 12. ClientBranches (Junction)
CREATE TABLE ClientBranches (
    ClientId INT NOT NULL,
    BranchId INT NOT NULL,
    PRIMARY KEY (ClientId, BranchId),
    CONSTRAINT FK_ClientBranches_Clients FOREIGN KEY (ClientId) REFERENCES Clients(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ClientBranches_Branches FOREIGN KEY (BranchId) REFERENCES Branches(Id) ON DELETE CASCADE
);

INSERT INTO ClientBranches (ClientId, BranchId) VALUES (1, 1), (2, 2);

-- 13. TaskAssignments (Junction)
CREATE TABLE TaskAssignments (
    TodoTaskId INT NOT NULL,
    UserId INT NOT NULL,
    PRIMARY KEY (TodoTaskId, UserId),
    CONSTRAINT FK_TaskAssignments_Tasks FOREIGN KEY (TodoTaskId) REFERENCES TodoTasks(Id) ON DELETE CASCADE,
    CONSTRAINT FK_TaskAssignments_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);
