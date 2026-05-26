SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.discussion_threads', N'U') IS NULL
BEGIN
    PRINT 'Table [dbo].[discussion_threads] was not found. No changes applied.';
    RETURN;
END;

IF COL_LENGTH('dbo.discussion_threads', 'ThreadType') IS NULL
BEGIN
    ALTER TABLE dbo.discussion_threads
        ADD ThreadType NVARCHAR(50) NOT NULL
            CONSTRAINT DF_discussion_threads_ThreadType DEFAULT(N'Issue');
END;

IF COL_LENGTH('dbo.discussion_threads', 'IssueSubType') IS NULL
BEGIN
    ALTER TABLE dbo.discussion_threads
        ADD IssueSubType NVARCHAR(100) NULL;
END;

IF COL_LENGTH('dbo.discussion_threads', 'IsSolved') IS NULL
BEGIN
    ALTER TABLE dbo.discussion_threads
        ADD IsSolved BIT NOT NULL
            CONSTRAINT DF_discussion_threads_IsSolved DEFAULT(0);
END;

IF COL_LENGTH('dbo.discussion_threads', 'ResolvedBy') IS NULL
BEGIN
    ALTER TABLE dbo.discussion_threads
        ADD ResolvedBy UNIQUEIDENTIFIER NULL;
END;

IF COL_LENGTH('dbo.discussion_threads', 'ResolvedAt') IS NULL
BEGIN
    ALTER TABLE dbo.discussion_threads
        ADD ResolvedAt DATETIME2 NULL;
END;

IF COL_LENGTH('dbo.discussion_threads', 'TicketNumber') IS NULL
BEGIN
    ALTER TABLE dbo.discussion_threads
        ADD TicketNumber NVARCHAR(40) NOT NULL
            CONSTRAINT DF_discussion_threads_TicketNumber DEFAULT(N'');
END;

IF COL_LENGTH('dbo.discussion_threads', 'IsVisibleToAll') IS NULL
BEGIN
    ALTER TABLE dbo.discussion_threads
        ADD IsVisibleToAll BIT NOT NULL
            CONSTRAINT DF_discussion_threads_IsVisibleToAll DEFAULT(0);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_discussion_threads_TicketNumber'
      AND object_id = OBJECT_ID(N'dbo.discussion_threads'))
BEGIN
    CREATE INDEX IX_discussion_threads_TicketNumber
        ON dbo.discussion_threads(TicketNumber);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_discussion_threads_IsSolved'
      AND object_id = OBJECT_ID(N'dbo.discussion_threads'))
BEGIN
    CREATE INDEX IX_discussion_threads_IsSolved
        ON dbo.discussion_threads(IsSolved);
END;

PRINT 'Discussion schema backfill completed.';
