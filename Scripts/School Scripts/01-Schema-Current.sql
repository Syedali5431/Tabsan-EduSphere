SET ANSI_NULLS ON;
GO

SET QUOTED_IDENTIFIER ON;
GO

/*
  School script pack - schema entrypoint.
  This wrapper keeps script structure aligned with the main Scripts folder.
*/

PRINT 'Executing shared schema script for School script pack...';
:r "..\01-Schema-Current.sql"
/*
  School domain wrapper for schema setup.
  Delegates to the shared schema script to keep schema source-of-truth centralized.
*/

:r "..\01-Schema-Current.sql"
