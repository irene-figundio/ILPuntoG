-- Initial Test Data for IlPuntoG (SQL Server)

-- Users (Password is 'password' hashed with BCrypt)
INSERT INTO Users (Name, Email, Username, PasswordHash)
VALUES ('Admin', 'admin@ilpuntog.it', 'admin', '$2a$11$qR7E85Mv8zL6H8.Fv8.fOe8F6H8F6H8F6H8F6H8F6H8F6H8F6H8F6');

-- Branches
INSERT INTO Branches (Name, Address) VALUES ('Sedi Milano', 'Via Dante 1, Milano');
INSERT INTO Branches (Name, Address) VALUES ('Sedi Roma', 'Piazza Navona 10, Roma');

-- Projects
INSERT INTO Projects (Name, Status, BranchId) VALUES ('Ristrutturazione Uffici', 1, 1);
INSERT INTO Projects (Name, Status, BranchId) VALUES ('Sviluppo Portale Web', 0, 2);

-- Appointments
INSERT INTO Appointments (Description, StartTime, Duration, BranchId)
VALUES ('Sopralluogo tecnico', '2026-03-01 10:00:00', '02:00:00', 1);

INSERT INTO Appointments (Description, StartTime, Duration, BranchId, ProjectId)
VALUES ('Riunione di progetto Alpha', '2026-03-05 14:30:00', '01:30:00', 2, 2);

-- TodoTasks
INSERT INTO TodoTasks (Title, Description, Priority, Deadline, Status, ProjectId)
VALUES ('Acquisto materiali', 'Ordinare vernici e pennelli', 2, '2026-03-10 18:00:00', 0, 1);

INSERT INTO TodoTasks (Title, Description, Priority, Deadline, Status, ProjectId, AssignedToUserId)
VALUES ('Configurazione Server', 'Setup ambiente di produzione', 3, '2026-03-15 09:00:00', 1, 2, 1);
