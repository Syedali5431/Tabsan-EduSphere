-- Canonical compatibility wrapper.
-- Keeps legacy runbooks that call 02-CoreSeed.sql working while the maintained source remains 02-Seed-Core.sql.
:r .\02-Seed-Core.sql
