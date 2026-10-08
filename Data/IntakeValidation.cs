namespace VitalReach.Web.Data;

public static class IntakeValidation
{
    public static List<string> Errors(IntakeEntry e, DateTime today)
    {
        List<string> errors = [];
        if (!e.Consent) errors.Add("Please confirm consent before submitting.");
        if (string.IsNullOrWhiteSpace(e.Name) || e.Name.Length > 120) errors.Add("Enter a name (up to 120 characters).");
        if (e.Age is null or < 18 or > 120) errors.Add("Enter your age. This online intake is for adults aged 18 or above; ask staff for an assisted assessment for a child.");
        if (e.Contact.Length > 100 || e.Notes.Length > 2000) errors.Add("Contact details or notes are too long.");
        if (e.SubmissionId == Guid.Empty || e.Forms.Count is < 1 or > 6 || e.Forms.Distinct().Count() != e.Forms.Count || e.Forms.Any(id => !IntakeCatalog.Forms.Any(f => f.Id == id))) errors.Add("Choose at least one valid checklist.");
        if (e.Pregnancy is not ("No / not applicable" or "Yes" or "Unknown / prefer not to say")) errors.Add("Select a valid pregnancy response.");
        if (e.Sex is not ("Female" or "Male" or "Other" or "Prefer not to say")) errors.Add("Select a valid sex response.");
        var ids = IntakeCatalog.Questions(e).Select(q => q.Id).ToHashSet();
        if (e.Answers.Count > 220 || e.Answers.Any(a => !ids.Contains(a.Key) || !IntakeCatalog.Choices.Contains(a.Value))) errors.Add("The checklist contains an invalid answer. Reload and try again.");
        if (e.BloodPressure.Count != 3 || e.Glucose.Count != 3 || e.Medications.Count != IntakeCatalog.MedicationNames.Length) errors.Add("The measurement rows are invalid.");
        bool DateValid(DateTime? date) => date.HasValue && date.Value.Date <= today.Date && date.Value.Date >= today.Date.AddYears(-10);
        foreach (var b in e.BloodPressure.Where(b => b.Date.HasValue || b.Systolic.HasValue || b.Diastolic.HasValue))
            if (!DateValid(b.Date) || b.Systolic is null or < 40 or > 300 || b.Diastolic is null or < 20 or > 200 || b.Systolic <= b.Diastolic) errors.Add("For each BP reading, enter a past/current date, systolic 40–300 and diastolic 20–200 mmHg, with systolic higher than diastolic. Ask staff to verify readings outside these limits.");
        foreach (var g in e.Glucose.Where(g => g.Date.HasValue || g.Value.HasValue))
            if (!DateValid(g.Date) || g.Unit is not ("mg/dL" or "mmol/L") || g.Type is not ("Unknown" or "Fasting" or "Random") || g.MgDl is null or < 10 or > 1500) errors.Add("For each glucose result, enter its date, type, unit, and value (10–1500 mg/dL or equivalent). Ask staff to verify results outside these limits.");
        void Lab(decimal? value, DateTime? date, decimal min, decimal max, string label)
        { if ((value.HasValue || date.HasValue) && (!DateValid(date) || value is null || value < min || value > max)) errors.Add($"Enter a valid {label} result and date, or leave both blank."); }
        Lab(e.Hba1c, e.Hba1cDate, 2, 25, "HbA1c (2–25%)");
        Lab(e.Egfr, e.EgfrDate, 0, 250, "eGFR (0–250 mL/min/1.73 m²)");
        Lab(e.Uacr, e.UacrDate, 0, 20000, "uACR (0–20000 mg/g)");
        foreach (var m in e.Medications)
            if (!IntakeCatalog.MedicationNames.Contains(m.Name) || !IntakeCatalog.Choices.Contains(m.Using) || m.Dose.Length > 120 || m.Duration.Length > 120 || m.Source is not ("Unknown" or "Prescription" or "Over the counter" or "Other")) errors.Add("Check medicine details and text lengths (maximum 120 characters each).");
        if (e.Medications.Select(m => m.Name).Distinct().Count() != IntakeCatalog.MedicationNames.Length) errors.Add("Medication rows must be unique.");
        return errors.Distinct().ToList();
    }
}
