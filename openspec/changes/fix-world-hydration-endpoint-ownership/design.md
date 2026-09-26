## Context

Live RabbitMQ lists two consumers for CharacterHydrationState. Characters sends GetCharacterResponse within a second, while world authentication cancels about ten seconds later. Startup registers an in-memory saga repository on a shared endpoint.

## Goals / Non-Goals

Ensure hydration messages reach the process owning the saga. Do not redesign hydration, add shared storage, fix unrelated inbox cleanup SQL, or alter clipping.

## Decisions

Use the existing WorldInstanceIdentity to name a temporary MassTransit saga endpoint. Register a directed HydrateCharacter request client using the same name. Put these coupled registrations in one domain-specific extension used by production and tests. MassTransit supplies the reply address and temporary queue lifecycle.

Direct the request client to the endpoint exchange with non-durable, auto-delete settings. A queue address would attempt to redeclare the exclusive temporary queue from the sending connection.

## Risks / Trade-offs

In-flight hydration is lost if its process stops, matching existing in-memory ownership. Reconnecting clients start new requests. Historical shared queues are not deleted by this change.

## Validation

Exercise two independent providers against RabbitMQ with real saga repositories and directed clients, including repeat requests and a replacement provider. Validate the live endpoints after restarting both worlds. User performs final client login.

Verification: 810 GameWorld unit tests passed; the RabbitMQ integration regression passed with production delayed-message scheduling. Aspire rebuilt and started successfully, and both worlds report healthy. Live RabbitMQ shows two separate exclusive, non-durable hydration queues, each with one consumer and a 60-second expiration setting; the historical shared queue has zero consumers. OpenSpec validation and the diff whitespace check passed. Final in-client world entry remains for the user to confirm.
