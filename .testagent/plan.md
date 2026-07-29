# Test plan

| Requirement | Test |
| --- | --- |
| Apply all migrations to an empty database | `Migrations_ApplyToEmptyMySqlDatabase_WithoutPendingChanges` |
| Run concurrent service migrators | `Migrations_AreIdempotent_WhenMultipleServicesStart` |
