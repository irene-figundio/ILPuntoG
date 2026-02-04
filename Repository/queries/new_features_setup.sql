-- SQL Script to update IlPuntoG database with new features (Documents, AuditLogs, and schema updates)

-- 1. Update TaskTypes
ALTER TABLE TaskTypes ADD IsBase BIT NOT NULL DEFAULT 1;

-- Seed new TaskTypes
INSERT INTO TaskTypes (Id, Name, IsBase) VALUES (5, 'Sviluppo', 0);
INSERT INTO TaskTypes (Id, Name, IsBase) VALUES (6, 'Ferie', 0);
INSERT INTO TaskTypes (Id, Name, IsBase) VALUES (7, 'Malattia', 0);

-- 2. Update TodoTasks
ALTER TABLE TodoTasks ADD GitLabRepoUrl NVARCHAR(MAX) NULL;

-- 3. Update Branches
ALTER TABLE Branches ADD HexColor NVARCHAR(10) NOT NULL DEFAULT '#3498db';

-- 4. Update Appointments
ALTER TABLE Appointments ADD IsRecurring BIT NOT NULL DEFAULT 0;
ALTER TABLE Appointments ADD Recurrence INT NOT NULL DEFAULT 0;

-- 5. Update Projects (Add ClientId for Branch Detail requirements)
ALTER TABLE Projects ADD ClientId INT NULL;
-- CONSTRAINT FK_Projects_Clients FOREIGN KEY (ClientId) REFERENCES Clients(Id) -- Optional depending on existing constraints

-- 6. New Table: Documents
CREATE TABLE Documents (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(255) NOT NULL,
    FileName NVARCHAR(MAX) NOT NULL,
    FilePath NVARCHAR(MAX) NOT NULL,
    ContentType NVARCHAR(100) NOT NULL,
    UploadDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    WorkLogId INT NULL,
    UserId INT NULL,
    CONSTRAINT FK_Documents_WorkLogs FOREIGN KEY (WorkLogId) REFERENCES WorkLogs(Id) ON DELETE SET NULL,
    CONSTRAINT FK_Documents_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE SET NULL
);

-- 7. New Table: AuditLogs
CREATE TABLE AuditLogs (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NULL,
    Action NVARCHAR(100) NOT NULL,
    EntityName NVARCHAR(100) NOT NULL,
    EntityId NVARCHAR(100) NULL,
    Timestamp DATETIME2 NOT NULL DEFAULT GETDATE(),
    Details NVARCHAR(MAX) NULL,
    CONSTRAINT FK_AuditLogs_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE SET NULL
);
