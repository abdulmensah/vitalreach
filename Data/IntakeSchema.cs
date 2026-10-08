using Microsoft.EntityFrameworkCore;

namespace VitalReach.Web.Data;

public static class IntakeSchema
{
    public static Task EnsureAsync(CatalogDbContext db) => db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "ConsultationSubmissions" (
            "Id" TEXT NOT NULL CONSTRAINT "PK_ConsultationSubmissions" PRIMARY KEY,
            "CreatedUtc" TEXT NOT NULL, "Priority" INTEGER NOT NULL, "Status" TEXT NOT NULL,
            "ProtectedContent" TEXT NOT NULL, "ProtectedReview" TEXT NOT NULL, "Version" TEXT NOT NULL
        );
        CREATE INDEX IF NOT EXISTS "IX_ConsultationSubmissions_CreatedUtc" ON "ConsultationSubmissions" ("CreatedUtc");
        CREATE TABLE IF NOT EXISTS "ConsultationAudits" (
            "Id" INTEGER NOT NULL CONSTRAINT "PK_ConsultationAudits" PRIMARY KEY AUTOINCREMENT,
            "SubmissionId" TEXT NULL, "CreatedUtc" TEXT NOT NULL, "Reviewer" TEXT NOT NULL, "Action" TEXT NOT NULL
        );
        """);
}
