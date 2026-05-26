-- Canonical compatibility wrapper.
-- Keeps legacy runbooks that call 01-Schema.sql working while the maintained source remains 01-Schema-Current.sql.
:r .\01-Schema-Current.sql
