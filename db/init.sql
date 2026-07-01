-- Creează baza + tabelele la primul start al containerului SQL.
-- Rulat automat de entrypoint-ul de init (vezi docker-compose.yml).

IF DB_ID('SyncretDB') IS NULL
BEGIN
    CREATE DATABASE SyncretDB;
END
GO

USE SyncretDB;
GO

-- Istoric evenimente
IF OBJECT_ID('dbo.ProcessLogs', 'U') IS NULL
CREATE TABLE ProcessLogs (
    Id        INT IDENTITY(1,1) PRIMARY KEY,
    Timestamp DATETIME      NOT NULL DEFAULT GETDATE(),
    Component NVARCHAR(100) NOT NULL,
    EventType NVARCHAR(50)  NOT NULL,
    Message   NVARCHAR(MAX) NULL
);
GO

-- Stare curentă (un singur rând, Id = 1)
IF OBJECT_ID('dbo.ProcessState', 'U') IS NULL
CREATE TABLE ProcessState (
    Id         INT PRIMARY KEY,
    M1         BIT NOT NULL,
    M2         BIT NOT NULL,
    M3         BIT NOT NULL,
    M4         BIT NOT NULL,
    IsAlarm    BIT NOT NULL,
    ClapetaPos NVARCHAR(10) NOT NULL,
    IsRunning  BIT NOT NULL DEFAULT 1,
    UpdatedAt  DATETIME     NOT NULL
);
GO

-- SEED OBLIGATORIU: rândul Id=1 trebuie să existe înainte de primul UPDATE
IF NOT EXISTS (SELECT 1 FROM ProcessState WHERE Id = 1)
INSERT INTO ProcessState (Id, M1, M2, M3, M4, IsAlarm, ClapetaPos, IsRunning, UpdatedAt)
VALUES (1, 0, 0, 0, 0, 0, 'None', 1, GETUTCDATE());
GO

-- Index pentru /api/logs și /api/stats
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ProcessLogs_Timestamp')
CREATE INDEX IX_ProcessLogs_Timestamp ON ProcessLogs(Timestamp DESC);
GO

-- Utilizatori (autentificare + roluri)
IF OBJECT_ID('dbo.Users', 'U') IS NULL
CREATE TABLE Users (
    Id           INT IDENTITY(1,1) PRIMARY KEY,
    Username     NVARCHAR(50)  NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    Role         NVARCHAR(20)  NOT NULL
);
GO
