# GameWorld clustering

Each configured GameWorld is one independently recoverable workload. A workload MUST have a unique positive `HAGALAZ_WORLD_ID` and a client-reachable `HAGALAZ_World__AdvertisedEndpoint__Host`/`Port` pair. Run exactly one serving replica for each identity during normal operation and rollout; a replacement reuses the same identity after the previous process has stopped.

The workload should restart automatically after process or node failure. Configure the orchestrator probes as follows:

- `/health` is the readiness probe. It is unhealthy until initialization, endpoint startup, generation registration, renewal, and conflict checks all pass.
- `/alive` is the liveness probe. It remains independent of RabbitMQ registration so a deadlocked or unhealthy world can be restarted.

Example production environment:

```text
HAGALAZ_WORLD_ID=1
HAGALAZ_World__Name=World 1
HAGALAZ_World__AdvertisedEndpoint__Host=game-world-1.example.net
HAGALAZ_World__AdvertisedEndpoint__Port=43594
```

The checked-in Aspire configuration runs `hagalaz-services-gameworld-1` on TCP/HTTPS/HTTP ports `443/7010/5010` and `hagalaz-services-gameworld-2` on `444/7011/5011`, with world IDs `1` and `2`. The two resources share the existing database, RabbitMQ, and Redis dependencies but advertise distinct client endpoints.

A replacement process publishes a new generation for the same logical world. Consumers ignore delayed offline messages from older generations, and a failed renewal removes the process from readiness while local consumers expire its lease from the lobby world list.
