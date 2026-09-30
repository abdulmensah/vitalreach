namespace VitalReach.Web.Data;

/// <summary>Decision support only. No diagnoses, risk probabilities, or treatment recommendations.</summary>
public static class IntakeScreening
{
    public const string DiabetesSource = "https://www.niddk.nih.gov/health-information/diabetes/overview/tests-diagnosis";
    public const string KidneySource = "https://www.niddk.nih.gov/health-information/kidney-disease/chronic-kidney-disease-ckd/tests-diagnosis";
    public const string BpSource = "https://www.heart.org/en/health-topics/high-blood-pressure/understanding-blood-pressure-readings/when-to-call-911-for-high-blood-pressure";
    public const string StrokeSource = "https://www.cdc.gov/stroke/signs-symptoms/index.html";
    public const string MentalSource = "https://www.nimh.nih.gov/health/publications/warning-signs-of-suicide";
    public const string LiverSource = "https://www.nhs.uk/conditions/jaundice/";
    public static string PriorityLabel(int priority) => priority switch { 3 => "Immediate help", 2 => "Prompt clinical review", 1 => "Clinical review", _ => "Incomplete / no automated flag" };

    public static List<ScreeningFlag> Analyze(IntakeEntry entry, DateTime today)
    {
        var flags = new List<ScreeningFlag>();
        void Add(string rule, int priority, string title, string evidence, string source = "") => flags.Add(new(rule, priority, title, evidence, source));
        foreach (var question in IntakeCatalog.Safety.Where(q => entry.Yes(q.Id)))
            Add(question.Id, 3, "Get help now. Alert center staff or call local emergency services. Do not wait for form review.", question.Label,
                question.Id switch { "safety.4" => MentalSource, "safety.3" => StrokeSource,
                    "safety.5" => "https://www.cdc.gov/diabetes/about/diabetic-ketoacidosis.html",
                    _ => "https://www.cdc.gov/heart-disease/about/heart-attack.html" });
        if (entry.Forms.Contains("cardio"))
            foreach (var q in IntakeCatalog.Forms.Single(f => f.Id == "cardio").Sections.Last().Questions.Where(q => entry.Yes(q.Id)))
                Add(q.Id, 3, "Possible stroke warning sign: seek emergency help now. Note when symptoms started.", q.Label, StrokeSource);
        if (entry.Forms.Contains("mental") && entry.Yes("mental.s.10"))
            Add("mental.safety", 2, "Immediate safety assessment by a clinician is needed. If there is danger now, seek emergency help and stay with a trusted person.", "Self-harm thoughts reported in the past two weeks.", MentalSource);
        if (entry.Forms.Contains("liver") && entry.Yes("liver.s.1"))
            Add("liver.jaundice", 2, "Jaundice needs urgent medical assessment.", "Yellow eyes / skin reported; this does not establish a cause.", LiverSource);
        if (entry.Forms.Contains("liver") && entry.Yes("liver.s.10"))
            Add("liver.confusion", 2, "Prompt assessment of confusion / unusual drowsiness is needed; if happening now, seek emergency help.", "Confusion or drowsiness reported.", LiverSource);

        bool adultStandard = entry.Age >= 18 && entry.Pregnancy == "No / not applicable";
        if (!adultStandard) Add("scope", 1, "Individual clinical interpretation required", "Pregnancy status is yes/unknown, or adult age is unconfirmed. Standard adult numeric thresholds were not applied.");
        if (!adultStandard && (entry.BloodPressure.Any(b => b.Systolic >= 140 || b.Diastolic >= 90) || entry.Glucose.Any(g => g.MgDl < 70 || g.MgDl >= 200)))
            Add("scope.measurements", 2, "Have a clinician assess your reported blood pressure / glucose promptly, particularly during pregnancy or after childbirth.", "Results may need urgent individual interpretation. If symptoms are happening now, alert staff immediately.", BpSource);
        if (adultStandard)
        {
            foreach (var bp in entry.BloodPressure.Where(b => b.Systolic.HasValue && b.Diastolic.HasValue && b.Date.HasValue))
            {
                var evidence = $"{bp.Date:yyyy-MM-dd}: {bp.Systolic}/{bp.Diastolic} mmHg (self-reported).";
                if (bp.Systolic >= 180 || bp.Diastolic >= 120)
                    Add("bp.severe", 2, bp.Date!.Value.Date == today.Date ? "Very high blood pressure: contact a clinician urgently. With chest pain, breathing difficulty or neurological symptoms, get emergency help now." : "A previous very high blood pressure reading needs prompt review; it does not establish your current pressure.", evidence, BpSource);
                else if (bp.Systolic >= 140 || bp.Diastolic >= 90) Add("bp.high", 1, "Elevated blood pressure reading needs clinical assessment and repeat measurement.", evidence, BpSource);
            }
            if (entry.Forms.Contains("diabetes"))
            {
                foreach (var glucose in entry.Glucose.Where(g => g.Value.HasValue && g.Date.HasValue))
                {
                    var evidence = $"{glucose.Date:yyyy-MM-dd}: {glucose.Value} {glucose.Unit}, {glucose.Type} (self-reported).";
                    var value = glucose.MgDl;
                    if (value < 70) Add("glucose.low", 2, "Low glucose reported: prompt assessment needed; if this is current or you feel unwell, alert staff immediately.", evidence, "https://www.cdc.gov/diabetes/about/low-blood-sugar-hypoglycemia.html");
                    else if (value >= 300) Add("glucose.veryhigh", 2, "Very high glucose reported: prompt clinical assessment needed. Current vomiting, deep breathing, confusion or severe dehydration needs emergency help.", evidence, "https://www.cdc.gov/diabetes/about/diabetic-ketoacidosis.html");
                    else if ((glucose.Type == "Fasting" && value >= 126) || value >= 200) Add("glucose.high", 1, "Glucose result needs diagnostic evaluation; a self-reported reading alone is not a diagnosis.", evidence, DiabetesSource);
                    else if (glucose.Type == "Fasting" && value >= 100) Add("glucose.raised", 1, "Raised fasting glucose needs follow-up testing.", evidence, DiabetesSource);
                }
                if (entry.Hba1c >= 5.7m) Add("a1c", 1, entry.Hba1c >= 6.5m ? "HbA1c needs diagnostic evaluation and confirmation." : "Raised HbA1c needs follow-up.", $"HbA1c {entry.Hba1c}% on {entry.Hba1cDate:yyyy-MM-dd}.", DiabetesSource);
            }
            if (entry.Forms.Contains("kidney"))
            {
                if (entry.Egfr < 60) Add("kidney.egfr", 1, "Reduced eGFR needs clinical review. One result cannot establish chronic kidney disease.", $"eGFR {entry.Egfr} mL/min/1.73 m² on {entry.EgfrDate:yyyy-MM-dd}.", KidneySource);
                if (entry.Uacr >= 30) Add("kidney.uacr", 1, "Urine albumin result needs confirmation and clinical review.", $"uACR {entry.Uacr} mg/g on {entry.UacrDate:yyyy-MM-dd}.", KidneySource);
            }
        }
        // Checklist concern flags are intentionally descriptive, not diagnostic point scores.
        foreach (var form in IntakeCatalog.Forms.Where(f => entry.Forms.Contains(f.Id)))
        {
            var concerns = form.Sections.SelectMany(s => s.Questions).Where(q => entry.Yes(q.Id) &&
                (q.Id.Contains(".s.") || q.Id.Contains(".r.") || q.Id.Contains(".w.") || q.Id.Contains(".f.")))
                .Where(q => q.Id is not ("kidney.r.1" or "kidney.r.2" or "cardio.r.1" or "diabetes.r.1")).ToList();
            if (concerns.Count > 0) Add(form.Id + ".checklist", 1, form.Title + ": discuss reported symptoms / history with the clinician", string.Join("; ", concerns.Select(q => q.Label)));
        }
        if (entry.Forms.Contains("kidney") && entry.Yes("kidney.t.4")) Add("kidney.urine", 1, "Reported protein or blood in urine needs clinical review.", IntakeCatalog.Label("kidney.t.4"), KidneySource);
        if (entry.Forms.Contains("liver") && entry.Yes("liver.t.4")) Add("liver.tests", 1, "Reported abnormal liver tests need clinical review.", IntakeCatalog.Label("liver.t.4"), LiverSource);
        if (entry.Medications.Any(m => m.Using == "Yes") && entry.Forms.Any(f => f is "bp" or "liver")) Add("medicines", 1, "Review reported medicines with a prescriber or pharmacist. Do not stop or change medicines based on this form.", string.Join(", ", entry.Medications.Where(m => m.Using == "Yes").Select(m => m.Name)));
        int unknown = IntakeCatalog.Questions(entry).Count(q => !entry.Answers.TryGetValue(q.Id, out var a) || a is "Unknown" or "Prefer not to say");
        if (unknown > 0) Add("incomplete", 0, "Assessment is incomplete", $"{unknown} screening questions are unknown or declined. Missing answers do not mean no symptoms.");
        Add("review.required", 0, "A clinician must review this submission", "This checklist cannot rule out disease, confirm a diagnosis, or replace examination and laboratory testing. The inbox is not monitored for emergencies.");
        return flags.OrderByDescending(f => f.Priority).ToList();
    }
}

