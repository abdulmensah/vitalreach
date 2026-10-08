# QA connection investigation — 2026-09-30

Reported symptom: after idle, the page appears locked briefly and automatically reconnects.

VPS observations: `vitalreach-qa` was active/running with `NRestarts=0`, started at 23:14 UTC. At 23:25 UTC, local health returned HTTP 200 in 0.004 seconds and public HTTPS health returned 200 in 0.116 seconds. No warning/error entries were returned by the recent priority-filtered journal query. The active Nginx configuration supports WebSocket upgrades and has a 100-second proxy read timeout. Available memory was approximately 1.18 GB. These observations do not demonstrate a server idle shutdown; they support investigating client connection interruption. No live server configuration was changed.

Blazor Interactive Server requires a live browser connection. Browser suspension, device sleep, network changes and application deployments can interrupt it. Existing evidence does not identify which caused the user's interruption. The proxy timeout exceeds the normal 15-second heartbeat and was not changed speculatively.

## Changes to test in QA

- Server and client allow 60 seconds without a heartbeat, with a 15-second keepalive interval.
- Disconnected sessions are retained in server memory for up to 10 minutes, capped at 100. This is recovery headroom, not durable draft storage; restart, eviction or expiry still loses unsaved state.
- A custom reconnection handler retries without automatically reloading a form, resumes retries on online/focus/visibility events, and distinguishes expired sessions from unavailable connections. Reload requires explicit confirmation.
- Connection up/down/closed events log an independent diagnostic identifier, never patient data, page URLs or reconnect credentials.

Reference: [Microsoft Blazor SignalR guidance](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/signalr?view=aspnetcore-10.0).

## Production readiness check

On the deployed QA build, use synthetic data to test an idle foreground tab, background tab, device lock/unlock, and network disconnect/reconnect. Verify a form's unsaved values survive a recoverable session, controls respond after reconnect, and no automatic reload occurs. Repeat on a real phone and the intended desktop browser. Then verify an expired/restarted session explains the loss and asks before reload. Inspect connection logs and process uptime during the reproduction. Long-idle UI validation is a manual release check; workflow success alone does not prove it passed.

Physical network interruptions and browser suspension cannot be eliminated by server configuration. Investigate any repeated disconnect while the tab is active before production promotion. Do not solve this with scheduled website pings: the application service is already continuously running.
