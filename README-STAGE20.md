# Stage 20 — Replay Engine

Stage 20 adds a sandboxed replay subsystem on top of the Stage 19 append-only Event Store.
Operational tables remain the live source of state. Replay reads immutable events and writes only to the `trading_replay` schema.

## Goals

- deterministic forward playback ordered by `global_position`;
- virtual UTC clock driven by event occurrence time;
- resumable replay jobs with PostgreSQL checkpoints;
- timeline/projection replay and strategy-comparison mode;
- persisted step results, summaries, failures, progress and cancellation;
- no dependency on Binance clients or live trade executors;
- API endpoints suitable for a future React replay screen.

## Runtime flow

```text
Dashboard POST /api/v1/replays
        ↓
trading_replay.jobs
        ↓
ReplayJobWorker
        ↓
IReplayEventSource reads trading_event_store.events ASC
        ↓
ReplayVirtualClock.AdvanceTo(event.OccurredAtUtc)
        ↓
ReplayAccumulator / optional IReplayStrategyEvaluator
        ↓
steps + checkpoint + deterministic hash + result
```

## Replay modes

- `Timeline`: validates and reproduces the exact chronological sequence.
- `Projection`: rebuilds replay counters/read-state without changing operational tables.
- `StrategyComparison`: sends recorded strategy-decision events to a registered `IReplayStrategyEvaluator` and stores matches/differences.

The included `recorded-strategy:1.0.0` evaluator is a safe baseline. A real candidate plugin adapter can be registered later, but it must return a decision only; it must not call Binance or production execution services.

## Safety boundary

`TradingSystem.ReplayEngine` references only `TradingSystem.EventStore`. It has no project reference to `TradingSystem.Binance`, Paper execution or live executors. The PostgreSQL implementation reads Event Store data and writes only replay-owned tables.

## API

```http
POST /api/v1/replays
GET  /api/v1/replays
GET  /api/v1/replays/{replayId}
GET  /api/v1/replays/{replayId}/result
GET  /api/v1/replays/{replayId}/steps
POST /api/v1/replays/{replayId}/cancel
```

Create and cancel operations require Operator authorization, write rate limiting and an `X-Idempotency-Key`.

Example request:

```json
{
  "name": "BOT8012 July decision replay",
  "mode": "StrategyComparison",
  "fromGlobalPosition": 10000,
  "toGlobalPosition": 12000,
  "botName": "BOT8012",
  "symbol": "BTCUSDC",
  "candidateStrategyPluginId": "recorded-strategy",
  "candidateStrategyVersion": "1.0.0",
  "batchSize": 250,
  "stopOnError": true
}
```

## Determinism

The engine hashes the ordered tuple of global position, source event id, event type and payload. Replaying an unchanged range in the same order produces the same `deterministicHash`. External time, random generators and market APIs are not consulted.

## Resume and cancellation

After every batch the engine stores:

- last processed global position;
- serialized accumulator state;
- replay steps;
- current deterministic hash.

A stale Processing job can be reclaimed and continues after its checkpoint. Cancellation is cooperative and checked between batches.

## Important limitation

Stage 20 replays recorded domain events. It does not reconstruct missing historical indicator inputs and does not claim that an arbitrary current strategy can always be rerun from old events. A candidate strategy evaluator needs enough versioned input in the recorded payload or a dedicated historical feature source.
