-- SQL Server Creation Script for IlPuntoG Database

CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(255) NOT NULL,
    Email NVARCHAR(255) NOT NULL,
    Username NVARCHAR(255) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(MAX) NOT NULL
);

CREATE TABLE Branches (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(255) NOT NULL,
    Address NVARCHAR(MAX) NOT NULL
);

CREATE TABLE Projects (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(255) NOT NULL,
    Status INT NOT NULL DEFAULT 0, -- ProjectStatus Enum
    BranchId INT NOT NULL,
    CONSTRAINT FK_Projects_Branches FOREIGN KEY (BranchId) REFERENCES Branches(Id) ON DELETE CASCADE
);

CREATE TABLE Appointments (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Description NVARCHAR(MAX) NOT NULL,
    StartTime DATETIME2 NOT NULL,
    Duration TIME NOT NULL,
    ProjectId INT NULL,
    BranchId INT NOT NULL,
    CONSTRAINT FK_Appointments_Branches FOREIGN KEY (BranchId) REFERENCES Branches(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Appointments_Projects FOREIGN KEY (ProjectId) REFERENCES Projects(Id) ON DELETE SET NULL
);

CREATE TABLE TodoTasks (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX) NOT NULL,
    Priority INT NOT NULL DEFAULT 0, -- TaskPriority Enum
    Deadline DATETIME2 NULL,
    Status INT NOT NULL DEFAULT 0, -- TodoStatus Enum
    ProjectId INT NOT NULL,
    AssignedToUserId INT NULL,
    CONSTRAINT FK_TodoTasks_Projects FOREIGN KEY (ProjectId) REFERENCES Projects(Id) ON DELETE CASCADE,
    CONSTRAINT FK_TodoTasks_Users FOREIGN KEY (AssignedToUserId) REFERENCES Users(Id) ON DELETE SET NULL
);
