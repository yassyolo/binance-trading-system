# Stage 15 migration

Apply `007_service_health_alerts_audit.sql` after migration 006. Restart all services so each instance starts publishing heartbeats. Run one Jobs Worker instance with AlertEngine enabled.
