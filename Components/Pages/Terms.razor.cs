#nullable enable
namespace VitalReach.Web.Components.Pages;

public partial class Terms
{
    private static readonly IReadOnlyList<LegalSectionLink> Sections =
    [
        new("acceptance", "Acceptance of these terms"),
        new("about", "About VitalReach"),
        new("education", "Educational—not medical—information"),
        new("consultation", "Consultation intake and consent"),
        new("screening", "Screening flags and clinical review"),
        new("emergencies", "Emergencies and service interruptions"),
        new("products", "Product information and availability"),
        new("orders", "Order requests, quotes, and payments"),
        new("administrators", "Administrator accounts"),
        new("acceptable-use", "Acceptable use"),
        new("intellectual-property", "Intellectual property"),
        new("third-parties", "Third-party services and links"),
        new("warranties", "Disclaimer of warranties"),
        new("liability", "Limitation of liability"),
        new("indemnification", "Indemnification"),
        new("suspension", "Suspension and termination"),
        new("applicable-law", "Applicable law and disputes"),
        new("changes", "Changes to the website or terms"),
        new("general", "General provisions"),
        new("contact", "Contact"),
    ];
}
