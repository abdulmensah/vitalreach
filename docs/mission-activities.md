# Mission activity articles

The Ghana Mission timeline reads published, showcased `MissionActivities` records from SQLite. Admins manage them at `/admin/mission-activities`. Dated records appear newest first, followed by undated records. A missing date is displayed as “Date not recorded”; location is optional. Pledged support has a separate label on both the timeline and article page.

The October 7 content import stores ten articles: five owner-described activities, one overview from the supplied collage, and four donation certificates. Only IQRA International School has an explicit event date (April 28, 2026). Certificate identifiers and WhatsApp filenames are not treated as event dates. Source notes are visible in admin only. No dates, counts, amounts, clinical outcomes or autism-specific services are inferred from the collage.

`MissionActivitySchema.EnsureAsync` upgrades the former required-date column while preserving article IDs and versions. `MissionActivitySeed.ApplyAsync` imports the source-backed records once using transactional receipts in `MissionActivityImports`. Subsequent starts do not overwrite edits or republish hidden articles. The normal startup seeder runs both operations; no live environment is changed until deployment/startup there.

For an existing local database, after taking the required project snapshot:

```powershell
dotnet run --project tests/MissionActivityChecks --configuration Release -- --import E:\Workspace\vitalreach\App_Data\vitalreach.db
```

Restart any running older application after this schema upgrade. The local import was applied October 7, 2026. Test, QA and production were not changed.

Run the migration, import-idempotency, publication, date-ordering and validation checks without modifying the local database:

```powershell
dotnet run --project tests/MissionActivityChecks --configuration Release
```

Certificate and collage images remain unmodified in `wwwroot/images/mission`. The timeline displays complete images without cropping certificate text. Remastered orphanage photographs remain in the separate page gallery; they are not asserted to depict a particular certificate recipient without confirmation.
