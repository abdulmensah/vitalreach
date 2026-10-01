#nullable enable
namespace VitalReach.Web.Components.Pages;

public partial class Privacy
{
    private static readonly IReadOnlyList<LegalSectionLink> Sections =
    [
        new("who-we-are", "Who we are and how to contact us"),
        new("information-collected", "Information we collect"),
        new("consultation-consent", "Consultation consent and choices"),
        new("screening-analysis", "Automated screening and human review"),
        new("purposes", "Why we use information"),
        new("access-and-sharing", "Access and sharing"),
        new("payments", "Payments and order records"),
        new("sign-in", "Google sign-in and administrator records"),
        new("cookies-and-sessions", "Cookies, forms and connections"),
        new("technical-records", "Technical records and safeguards"),
        new("retention", "Retention and backups"),
        new("your-rights", "Your requests and rights"),
        new("children-and-international", "Age limits and international processing"),
        new("policy-updates", "Changes to this policy"),
    ];
}
