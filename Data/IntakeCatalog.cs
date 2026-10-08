namespace VitalReach.Web.Data;

public sealed record IntakeQuestion(string Id, string Label);
public sealed record IntakeSection(string Title, IntakeQuestion[] Questions);
public sealed record IntakeForm(string Id, string Title, string SourceId, IntakeSection[] Sections);

public static class IntakeCatalog
{
    public static readonly string[] Choices = ["Unknown", "Yes", "No", "Prefer not to say"];
    public static readonly string[] MedicationNames = ["Tramadol", "Tapentadol", "Codeine / codeine-containing products", "Diclofenac / other NSAIDs", "Ibuprofen", "High-dose / frequent paracetamol (acetaminophen)", "Tramadol / paracetamol combination product", "Herbal / traditional pain remedies", "Other (describe in notes)"];
    public static readonly IntakeQuestion[] Safety = Q("safety", "Chest pain or pressure happening now", "Severe difficulty breathing, confusion, collapse, or difficulty staying awake now", "Sudden face droop, one-sided arm weakness, speech difficulty, vision loss, loss of balance, or a sudden worst-ever headache now", "Thoughts of self-harm or suicide now, or unable to keep yourself safe", "Persistent vomiting with rapid/deep breathing or severe dehydration now");
    private static IntakeQuestion[] Q(string prefix, params string[] labels) => labels.Select((l, i) => new IntakeQuestion($"{prefix}.{i + 1}", l)).ToArray();
    private static IntakeSection S(string title, string prefix, params string[] labels) => new(title, Q(prefix, labels));
    public static readonly IntakeForm[] Forms =
    [
        new("kidney", "Kidney health", "VRWH-CHK-CKD-001-F", [
            S("History and risk factors", "kidney.r", "Age between 20 and 50", "Male sex", "Known or suspected high blood pressure", "Known diabetes", "Family history of kidney disease, hypertension, or diabetes", "Regular long-term use of opioids or NSAID pain medicines", "Regular herbal or traditional remedies", "Regular alcohol use", "Informal employment, unemployment, or difficulty accessing regular healthcare", "No routine primary-care checkups", "Previous unexplained swelling, foamy urine, or reduced urine"),
            S("Symptoms experienced recently", "kidney.s", "Swelling of legs, ankles, face, or around eyes", "Persistent fatigue or weakness", "Foamy, bloody, or unusually dark urine", "Change in urine volume or frequency", "Persistent nausea, vomiting, or appetite loss", "Persistent itching", "Muscle cramps, especially at night", "Flank or lower-back pain", "Unexplained breathlessness"),
            S("Tests already performed (do not guess)", "kidney.t", "Blood pressure has been measured", "A blood pressure reading of 140/90 or above has been reported", "Urine dipstick has been performed", "Protein or blood was reported on urine dipstick", "Creatinine / eGFR blood test obtained", "Blood glucose obtained", "A clinician has checked for swelling / edema")]),
        new("bp", "Blood pressure and pain medicines", "VRWH-CHK-BPMED-001-F", [
            S("Blood pressure history", "bp.r", "Known hypertension", "Currently taking prescribed blood-pressure medicine", "Blood pressure has risen over the last three readings"),
            S("Medicine use concerns", "bp.s", "Early refills or needing higher doses", "Using pain medicine for a purpose other than pain", "Obtaining medicine from multiple or unlicensed sources", "Withdrawal symptoms when medicine is unavailable", "Increasing the dose or frequency without a prescriber", "No regular prescriber oversight")]),
        new("cardio", "Cardiovascular and stroke health", "VRWH-CHK-CVD-001-F", [
            S("History and measurements", "cardio.h", "Know your usual blood pressure", "Taking prescribed blood-pressure medicine", "Skipped or stopped prescribed blood-pressure medicine (explain in notes)", "Pulse has been recorded", "Pulse was reported as regular"),
            S("Risk factors", "cardio.r", "Age above 40, or younger with other cardiovascular risks", "Diabetes or high blood glucose", "Known high cholesterol or lipid disorder", "Tobacco use", "Alcohol use", "Frequent salty, processed, or fried foods", "Limited physical activity", "Overweight", "Family history of high blood pressure, heart disease, or stroke", "Chronic kidney disease", "Regular opioid or NSAID use"),
            S("Symptoms recently (use the safety questions for symptoms happening now)", "cardio.s", "Chest pain or pressure at rest or with activity", "Breathlessness with mild activity or lying flat", "Palpitations", "Swelling of legs", "Fatigue or reduced exercise tolerance", "Frequent or severe headaches", "Dizziness or fainting"),
            S("Sudden symptoms happening NOW — tell staff immediately", "cardio.e", "Face drooping or numbness", "Arm weakness or numbness", "Difficulty speaking or understanding speech", "Sudden vision loss or double vision", "Sudden worst-ever headache", "Sudden loss of balance or coordination")]),
        new("diabetes", "Diabetes screening", "VRWH-CHK-DM-001-F", [
            S("Measurements already performed", "diabetes.m", "Blood pressure measured", "BMI recorded", "Central / abdominal obesity identified by a clinician"),
            S("Risk factors", "diabetes.r", "Age above 40", "Overweight", "Family history of diabetes", "Hypertension", "Kidney or cardiovascular disease", "Limited physical activity", "Frequent sugary foods, drinks, or refined carbohydrates", "Previous high glucose or prediabetes", "Previous gestational diabetes", "Previously delivered a baby over 4 kg", "Polycystic ovary syndrome"),
            S("Symptoms experienced recently", "diabetes.s", "Unusual thirst", "Frequent urination, especially at night", "Unexplained weight loss", "Unusual hunger", "Fatigue", "Blurred vision", "Slow-healing wounds", "Frequent infections", "Numbness, tingling, or pain in hands or feet", "Darkened skin patches around neck or armpits"),
            S("Tests already performed", "diabetes.t", "Blood glucose test", "HbA1c test", "Urine glucose / ketone test", "Foot examination", "Vision / eye assessment")]),
        new("liver", "Liver health and medicines", "VRWH-CHK-LIVER-001-F", [
            S("Medicine and alcohol history", "liver.r", "Alcohol taken together with pain medicines", "Daily opioid use or use for more than five years", "Tramadol above 400 mg/day or above prescribed dose", "Taking multiple medicines that may affect the liver", "History of liver disease", "No regular healthcare follow-up", "Chronic kidney disease", "Self-medication or medicines from unlicensed sources"),
            S("Symptoms experienced recently", "liver.s", "Yellow eyes or skin (jaundice)", "Unusually dark urine", "Pale stools", "Persistent nausea or vomiting", "Loss of appetite", "Right upper abdominal pain or swelling", "Unusual fatigue", "Easy bruising or bleeding", "Persistent itching", "Confusion or unusual drowsiness"),
            S("Tests already performed", "liver.t", "A clinician has checked for jaundice", "Abdominal examination", "Liver function blood tests obtained", "Abnormal liver function results reported", "Kidney function checked")]),
        new("mental", "Medicine dependence and mental wellbeing", "VRWH-CHK-MHSD-001-F", [
            S("Medicine use in the past 12 months — answered privately, without judgment", "mental.r", "Using more medicine or for longer than intended", "Difficulty cutting down", "Much time spent obtaining, using, or recovering from medicine", "Strong cravings", "Needing increasing amounts to get the same effect (tolerance)", "Continued use despite physical harm", "Use in hazardous situations such as driving", "Neglected responsibilities", "Relationship conflict due to use", "Multiple or unlicensed sources"),
            S("When medicine is delayed or reduced", "mental.w", "Sweating or chills", "Tremors or body aches", "Nausea, vomiting, or diarrhea", "Anxiety or agitation", "Insomnia", "Runny nose, watery eyes, or yawning", "Fast pulse or high blood pressure"),
            S("Mental wellbeing in the past two weeks", "mental.s", "Low mood", "Loss of interest or pleasure", "Anxiety", "Sleep changes", "Appetite or weight changes", "Trouble concentrating", "Hopelessness or worthlessness", "Irritability", "Withdrawal from other people", "Thoughts of self-harm or that life is not worth living"),
            S("Effects on daily life", "mental.f", "Work difficulties or job loss", "Financial strain", "Family conflict", "Legal difficulties", "No regular prescribing clinician")])
    ];
    public static IEnumerable<IntakeQuestion> Questions(IntakeEntry entry) => Safety.Concat(Forms.Where(f => entry.Forms.Contains(f.Id)).SelectMany(f => f.Sections).SelectMany(s => s.Questions));
    public static string Label(string id) => Safety.Concat(Forms.SelectMany(f => f.Sections).SelectMany(s => s.Questions)).FirstOrDefault(q => q.Id == id)?.Label ?? id;
}

