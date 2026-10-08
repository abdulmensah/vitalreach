using System.ComponentModel.DataAnnotations;

namespace VitalReach.Web.Data;

public sealed class ConsultationSubmission
{
    public Guid Id { get; set; }
    public DateTime CreatedUtc { get; set; }
    public int Priority { get; set; }
    public string Status { get; set; } = "Awaiting review";
    public string ProtectedContent { get; set; } = "";
    public string ProtectedReview { get; set; } = "";
    public Guid Version { get; set; } = Guid.NewGuid();
}

public sealed class ConsultationAudit
{
    public long Id { get; set; }
    public Guid? SubmissionId { get; set; }
    public DateTime CreatedUtc { get; set; }
    public string Reviewer { get; set; } = "";
    public string Action { get; set; } = "";
}

public sealed class IntakeEntry
{
    public Guid SubmissionId { get; set; } = Guid.NewGuid();
    [Required, StringLength(120)] public string Name { get; set; } = "";
    [Range(18, 120, ErrorMessage = "This form is for adults. Visitors under 18 should ask staff for an assisted assessment.")] public int? Age { get; set; }
    [Required] public string Sex { get; set; } = "Prefer not to say";
    public string Pregnancy { get; set; } = "Unknown / prefer not to say";
    [StringLength(100)] public string Contact { get; set; } = "";
    [StringLength(2000)] public string Notes { get; set; } = "";
    public bool Consent { get; set; }
    public List<string> Forms { get; set; } = [];
    public Dictionary<string, string> Answers { get; set; } = [];
    public List<BloodPressureReading> BloodPressure { get; set; } = [new(), new(), new()];
    public List<GlucoseReading> Glucose { get; set; } = [new(), new(), new()];
    public List<MedicationEntry> Medications { get; set; } = IntakeCatalog.MedicationNames.Select(n => new MedicationEntry { Name = n }).ToList();
    public decimal? Hba1c { get; set; }
    public DateTime? Hba1cDate { get; set; }
    public decimal? Egfr { get; set; }
    public DateTime? EgfrDate { get; set; }
    public decimal? Uacr { get; set; }
    public DateTime? UacrDate { get; set; }
    public bool Yes(string id) => Answers.GetValueOrDefault(id) == "Yes";
}
public sealed class BloodPressureReading
{
    public DateTime? Date { get; set; }
    public int? Systolic { get; set; }
    public int? Diastolic { get; set; }
}
public sealed class GlucoseReading
{
    public DateTime? Date { get; set; }
    public decimal? Value { get; set; }
    public string Type { get; set; } = "Unknown";
    public string Unit { get; set; } = "mg/dL";
    public decimal? MgDl => Unit == "mmol/L" ? Value * 18m : Value;
}
public sealed class MedicationEntry
{
    public string Name { get; set; } = "";
    public string Using { get; set; } = "Unknown";
    public string Dose { get; set; } = "";
    public string Duration { get; set; } = "";
    public string Source { get; set; } = "Unknown";
}
public sealed record ScreeningFlag(string Rule, int Priority, string Title, string Evidence, string Source);
public sealed record IntakeEnvelope(IntakeEntry Entry, List<ScreeningFlag> Flags, DateTime ConsentedUtc,
    string ConsentVersion = "2026-09-30", string FormVersion = "1", string RuleVersion = "1",
    string PrivacyVersion = "", string TermsVersion = "");
public sealed record ClinicalReview(string Disposition, string Notes, string Reviewer, DateTime ReviewedUtc);
public sealed record IntakeDetail(ConsultationSubmission Record, IntakeEnvelope Content, ClinicalReview? Review);
