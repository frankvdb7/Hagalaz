# Tasks

## 1. Extend the existing Raido meter

- [x] 1.1 Add the dispatch, write, reconnect, and client-timeout counters and histograms to `RaidoMetrics`, including bounded outcome tags and per-connection enablement flags; verify instrument names, units, and recorded tags with focused `MeterListener` tests.

## 2. Instrument message dispatch and writes

- [x] 2.1 Record one inbound dispatch measurement at the receive boundary and propagate built-in dispatcher outcomes without changing the public dispatcher contract; verify handled, unhandled, unauthorized, error, and control-message outcomes with `MeterListener` tests.
- [x] 2.2 Record one outbound measurement per connection write at `RaidoHubConnectionContext.WriteAsync`; verify direct and multi-client sends plus sent, canceled, failed, detached, and disabled-instrument behavior with `MeterListener` tests.

## 3. Instrument reconnect and timeout lifecycle

- [x] 3.1 Provide the existing metrics service to the TCP lifecycle before physical activation, then record detach, success, timeout, and terminal failure with duration from the logical detach timestamp; verify successful rebind and terminal paths with `MeterListener` tests.
- [x] 3.2 Count client timeout only when its terminal transition succeeds; verify repeated timeout checks count once and inactive connections do not count.
- [x] 3.3 Run strict OpenSpec validation, the Raido server and reconnect integration test projects, the ServiceDefaults build, and `git diff --check`; confirm the existing `Raido.Server` meter export needs no new wiring.
