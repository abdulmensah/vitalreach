#nullable enable
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using VitalReach.Web.Data;

namespace VitalReach.Web.Components.Pages;

public partial class WellnessCenter
{
    private const string PlanVisitUrl = "/contact?message=Hello%20VitalReach%2C%0A%0AI%20would%20like%20to%20plan%20a%20visit%20to%20the%20Shopping%20%26%20Wellness%20Center.%20Please%20share%20the%20available%20dates%20and%20times.%0A%0AThank%20you.";
    private const string ConsultationUrl = "/contact?message=Hello%20VitalReach%2C%0A%0AI%20would%20like%20more%20information%20about%20wellness%20guidance%20at%20the%20Shopping%20%26%20Wellness%20Center.%20Please%20contact%20me%20with%20the%20next%20steps.%0A%0AThank%20you.";
    private const string CenterInformationUrl = "/contact?message=Hello%20VitalReach%2C%0A%0AI%20would%20like%20more%20information%20about%20the%20Shopping%20%26%20Wellness%20Center.%0A%0AThank%20you.";

    private static readonly ServiceCard[] Services =
    [
        new("01", "Shop the collection", "Explore VitalReach products in person and get help understanding formats, labels, intended use, and ordering options.", "/shop", "Explore products"),
        new("02", "Wellness guidance", "Talk through responsible daily routines, product questions, and the topics you may want to discuss with your healthcare team.", ConsultationUrl, "Request information"),
        new("03", "Health education", "Access clear kidney-health education covering screening, blood pressure, diabetes, medication safety, and prevention.", "/health-guide", "Visit the health guide")
    ];

    private static readonly ExperienceCard[] Experiences =
    [
        new(
            "Curated shopping",
            "Discover what fits your routine",
            "Browse an organized collection in a calm setting where there is time to compare, read, and ask before you choose.",
            "/images/wellness-center/wellness-center-retail-gallery.png",
            "Illuminated VitalReach product gallery and discovery table inside the wellness center",
            ["Shop by wellness goal and product category", "Compare sizes, formats, and product information", "Ask about availability and ordering options"],
            "/shop",
            "Browse the collection"),
        new(
            "Thoughtful guidance",
            "Make space for better questions",
            "Our conversation area offers a quieter place to explore product information and build a more informed, responsible wellness plan.",
            "/images/wellness-center/wellness-center-consultation.png",
            "Private VitalReach wellness guidance room with teal seating and product information tablet",
            ["Bring your current product list or questions", "Learn how to read labels and usage information", "Identify questions for a qualified health professional"],
            ConsultationUrl,
            "Ask about guidance")
    ];

    private static readonly VisitStep[] VisitSteps =
    [
        new("01", "Plan ahead", "Contact us to confirm opening hours, product availability, and whether you would like dedicated time with a team member."),
        new("02", "Explore at your pace", "Browse the collection, compare product information, and tell us what you would like to understand better."),
        new("03", "Leave with a clear next step", "Take away useful product information, education resources, and any questions that belong with your healthcare professional.")
    ];

    [Inject] private IDbContextFactory<CatalogDbContext> DbFactory { get; set; } = default!;
    private HeadquartersSettings? Headquarters;

    protected override async Task OnInitializedAsync()
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        Headquarters = await db.Headquarters.AsNoTracking().SingleAsync(x => x.Id == 1);
    }

    private sealed record ServiceCard(string Number, string Title, string Description, string Href, string LinkText);
    private sealed record ExperienceCard(string Eyebrow, string Title, string Description, string ImageUrl, string ImageAlt, string[] Details, string Href, string LinkText);
    private sealed record VisitStep(string Number, string Title, string Description);
}
