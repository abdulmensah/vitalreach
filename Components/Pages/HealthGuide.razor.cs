#nullable enable

namespace VitalReach.Web.Components.Pages;

public partial class HealthGuide
{
    private static readonly GuideTopic[] GuideTopics =
    [
        new("01", "Risk factors", "#risk"),
        new("02", "Screening", "#screening"),
        new("03", "Daily protection", "#protect"),
        new("04", "Medicine safety", "#medicines")
    ];

    private static readonly GuideCard[] RiskFactors =
    [
        new("01", "Diabetes", "High blood sugar can damage the small blood vessels and filtering structures in the kidneys over time."),
        new("02", "High blood pressure", "Uncontrolled blood pressure can damage kidney blood vessels; kidney disease can also make blood pressure harder to manage."),
        new("03", "Heart disease", "Heart and kidney health are closely connected, and reduced blood flow or heart failure can increase kidney risk."),
        new("04", "Family history", "A close relative with chronic kidney disease or kidney failure may mean you should discuss testing earlier.")
    ];

    private static readonly ScreeningTest[] ScreeningTests =
    [
        new("eGFR", "A blood-test estimate", "Estimated glomerular filtration rate uses a blood creatinine result and other information to estimate how well the kidneys filter blood.", "Ask: What is my eGFR, and what does its trend mean for me?"),
        new("uACR", "A urine albumin check", "Urine albumin-to-creatinine ratio checks for albumin, a blood protein that can pass into urine when the kidneys are damaged.", "Ask: What is my uACR, and should the test be repeated?")
    ];

    private static readonly GuideCard[] ProtectionActions =
    [
        new("01", "Manage blood pressure", "Know your numbers, take prescribed medicines as directed, and agree on a personal target with your healthcare professional."),
        new("02", "Manage blood sugar", "If you have diabetes, follow your treatment plan and ask how often kidney blood and urine tests are appropriate."),
        new("03", "Build sustainable habits", "Choose balanced meals, limit excess salt, stay active as appropriate, avoid tobacco, and attend regular checkups."),
        new("04", "Hydrate appropriately", "Fluid needs are personal. If you have kidney or heart disease, follow the specific fluid guidance provided by your care team.")
    ];

    private static readonly string[] VisitQuestions =
    [
        "What is my eGFR?",
        "What is my urine albumin or uACR result?",
        "What is my blood-pressure goal?",
        "If I have diabetes, what is my blood-sugar goal?",
        "Could any of my medicines or supplements affect my kidneys?"
    ];

    private sealed record GuideTopic(string Number, string Label, string Href);
    private sealed record GuideCard(string Number, string Title, string Description);
    private sealed record ScreeningTest(string Abbreviation, string Title, string Description, string Question);
}
