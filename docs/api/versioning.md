# Versioning

The current stable path prefix is `/api/v1`. Existing operations can add optional fields
or new routes within v1. Changing the meaning of a field, removing a supported field,
or introducing an incompatible required field calls for a new version and migration
guidance. Preserve stored integer enum values. Clients should handle unknown future
enum values safely when reading responses.

Changes to the contract require a regenerated OpenAPI artifact, an independent
specification validation, and the contract synchronization test. Add a changelog entry
describing behavior, compatibility, and any operational steps.

EF migration identifiers are independent of API versions. Apply migrations through
the release procedure, including upgrade checks for the supported previous schema.
