using Microsoft.EntityFrameworkCore;

namespace VitalReach.Web.Data;

// Source-backed initial articles. Import receipts prevent subsequent admin edits
// (including unpublishing) from being overwritten by an application restart.
public static class MissionActivitySeed
{
    private const string PreviousAttribution = "This collage brings together images and notices from the community work previously carried out under Rashias and now continued under VitalReach International.";
    private const string FoundationAttribution = "This collage brings together images and notices from the charitable and community work of Rashias Foundation, a registered nonprofit organization in Ghana. VitalReach International, also a registered nonprofit in Ghana, continues that work.";
    public static async Task ApplyAsync(CatalogDbContext db)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS MissionActivityImports (ImportKey TEXT NOT NULL PRIMARY KEY)");
        foreach (var (key, article) in Articles())
        {
            var inserted = await db.Database.ExecuteSqlInterpolatedAsync($"INSERT OR IGNORE INTO MissionActivityImports (ImportKey) VALUES ({key})");
            if (inserted == 0) continue;
            db.MissionActivities.Add(article);
            await db.SaveChangesAsync();
        }
        // Correct only the earlier imported attribution; preserve the rest of each
        // article, including admin edits and publication choices.
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE MissionActivities SET Story = replace(Story, {PreviousAttribution}, {FoundationAttribution}), Version = Version + 1 WHERE instr(Story, {PreviousAttribution}) > 0");
        var waterUpdate = Articles().Single(x => x.Key == "mission-water-tank-v1").Article;
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE MissionActivities SET Title = {waterUpdate.Title}, ActivityDate = {waterUpdate.ActivityDate}, Location = {waterUpdate.Location}, Summary = {waterUpdate.Summary}, Story = {waterUpdate.Story}, ImageUrl = {waterUpdate.ImageUrl}, ImageAlt = {waterUpdate.ImageAlt}, SourceNote = {waterUpdate.SourceNote}, Version = Version + 1 WHERE Title = 'Rashias Foundation community water-access support' AND ImageUrl = '/images/mission/rashias-community-water-tank.png' AND ActivityDate IS NULL AND Version = 0");
        await transaction.CommitAsync();
    }

    private static IEnumerable<(string Key, MissionActivity Article)> Articles()
    {
        yield return ("mission-eid-v1", New("Eid feeding and gift distribution",
            "Eid meals for children and gift packages for widows and older people bring practical support and celebration together.",
            "The mission's Eid activities include meals for children and gift packages for widows and older people. Packages include food and clothing.\n\nThese efforts help people in vulnerable circumstances share in the joy and dignity of the blessed Eid celebrations.", "Owner-provided activity description, October 7, 2026. Event date and precise location not supplied."));
        yield return ("mission-training-v1", New("Safe patient-handling workshops for healthcare professionals",
            "Hands-on training in safe patient transfer and lifting techniques for hospital staff, community clinic workers and paramedics.",
            "Hands-on workshops have been conducted for healthcare professionals, including hospital staff, community clinic workers and paramedics.\n\nThe workshops focused on safe patient transfer and lifting techniques, with the aim of preventing work-related injuries.", "Owner-provided activity description, October 7, 2026. No training accreditation or measured injury reduction claimed."));
        yield return ("mission-health-v1", New("Community health screenings and first aid outreach",
            "Health screenings and first aid support delivered in collaboration with local nurses and doctors.",
            "Healthcare outreach brings health screenings and first aid support to the community in collaboration with local nurses and doctors.\n\nThe supplied community-work collage also highlights free health screening. These activities sit alongside health-awareness work associated with the Wellness Center.", "Owner description and supplied community-work collage, October 7, 2026. No dates, clinical results or participant counts supplied."));
        yield return ("mission-scholarships-v1", New("Educational scholarships and school essentials",
            "Books, uniforms and school-fee support help students in need continue their education.",
            "The mission has provided books, school uniforms and fee support for students in need.\n\nThe community-work collage also highlights scholarships for students facing financial hardship. This support addresses practical barriers to learning.", "Owner description and collage. Dates, student identities, amounts and schools not supplied for this general programme."));
        var water = New("Borehole water access: support pledged",
            "Support has been pledged for clean water access in a surrounding village through a borehole initiative.",
            "Support has been pledged for clean water access in a surrounding village through a borehole initiative.\n\nThis entry records a pledge. Completion, installation and delivery of water access have not yet been confirmed.", "Owner explicitly described this as support pledged. Village and date not supplied.");
        water.IsPledged = true;
        yield return ("mission-water-v1", water);
        var collage = New("Community support: a look back at the work",
            "A collection of community-work images highlights health screening, educational support and support for older people and people with disabilities.",
            FoundationAttribution + "\n\nIts readable text highlights free health screening, scholarships, support for older people and support for people with disabilities.\n\nThe collage is presented as an overview of the work, rather than a record of one dated event.", "Source: owner-supplied collage codex-clipboard-a57956d5-74fd-40fa-a71a-eb3aec070f51.png. Small text is not fully legible; no autism-specific service or clinical treatment inferred from logos.");
        collage.ImageUrl = "/images/mission/community-work-collage.png";
        collage.ImageAlt = "Collage of community outreach photographs and notices covering health screening, scholarships and community support";
        yield return ("mission-collage-v1", collage);
        yield return ("mission-certificate-001-v1", Certificate("Donation to Countryside Children's Welfare Home",
            "Countryside Children's Welfare Home (C.C.W.H)", "Bawjiasi, Ghana", "VRI2024/001", "certificate-countryside.jpeg"));
        yield return ("mission-certificate-002-v1", Certificate("Donation to GMM Dar El Salam Basic School Complex",
            "GMM Dar El Salam Basic School Complex", "Kadewaso, Eastern Region", "VRI2024/002", "certificate-gmm-school.jpeg"));
        yield return ("mission-certificate-003-v1", Certificate("Donation to Afeeda Sarah",
            "Afeeda Sarah", "Kasoa, Central Region", "VRI2024/003", "certificate-afeeda-sarah.jpeg"));
        var iqra = Certificate("Donation to IQRA International School", "IQRA International School",
            "Kasoa Bojiwase Road", "VRI2026/004", "certificate-iqra-school.jpeg");
        iqra.ActivityDate = new DateTime(2026, 4, 28);
        iqra.SourceNote = "Owner-supplied certificate VRI2026/004 explicitly dates the donation April 28th 2026. Location transcribed as printed.";
        yield return ("mission-certificate-004-v1", iqra);
        var dedeiman = New("Rashias Foundation community support in Dedeiman",
            "A plaque dated 31 December 2022 records Rashias Foundation's support for the people of Dedeiman, in collaboration with the Ghana Muslim Mission Medical Team.",
            "A commemorative plaque in Dedeiman, Ga West, Greater Accra, Ghana records Rashias Foundation as the donor, in collaboration with the Ghana Muslim Mission Medical Team. It names the people of Dedeiman as the beneficiaries and gives the date as 31 December 2022.\n\nThis historical record belongs to Rashias Foundation's community work. VitalReach International continues the charitable functions previously performed by the foundation.\n\nThe plaque does not specify the donated items or the type of project, so those details are not attributed to this entry.",
            "Source: owner-supplied plaque photograph codex-clipboard-46a935f0-2681-4fad-bad2-fd4c33db417f.jpg. Date 31/12/2022 and location transcribed from plaque. No borehole completion, feeding activity or medical procedure inferred.");
        dedeiman.ActivityDate = new DateTime(2022, 12, 31);
        dedeiman.Location = "Dedeiman, Ga West, Greater Accra, Ghana";
        dedeiman.ImageUrl = "/images/mission/rashias-dedeiman-plaque-2022.jpg";
        dedeiman.ImageAlt = "Plaque naming Rashias Foundation and Ghana Muslim Mission Medical Team, the people of Dedeiman as beneficiaries, and the date 31 December 2022";
        yield return ("mission-dedeiman-2022-v1", dedeiman);
        var tank = New("Water-access support for Ashifla Otaten",
            "A plaque dated 3 June 2023 credits Rashias Foundation, in collaboration with the Ghana Muslim Mission Medical Team, for support benefiting the people of Ashifla Otaten.",
            "Photographs document a water-storage tank and a tiled water point. The clearer plaque photograph names the people of Ashifla Otaten as beneficiaries and gives the location as Ga South Municipal, Greater Accra, Ghana. It is dated 3 June 2023.\n\nThe plaque credits Rashias Foundation in collaboration with the Ghana Muslim Mission Medical Team. This historical work belongs to the foundation's community activities, whose charitable functions continue through VitalReach International.",
            "Sources: owner-supplied photographs codex-clipboard-e4402824-15e1-4763-bcaa-79884104cc5e.png and codex-clipboard-4769654e-18c6-4f57-ab03-06f4971475af.png. Clearer plaque reads PEOPLE OF ASHIFLA OTATEN; Ga South, Municipal Greater Accra, Ghana; 3/6/2023 (day/month/year). No tank capacity, water-quality testing, borehole construction or relationship to the separate borehole pledge inferred.");
        tank.ActivityDate = new DateTime(2023, 6, 3);
        tank.Location = "Ashifla Otaten, Ga South Municipal, Greater Accra, Ghana";
        tank.ImageUrl = "/images/mission/rashias-ashifla-otaten-plaque-2023.png";
        tank.ImageAlt = "Tiled water point with a plaque naming Rashias Foundation, Ghana Muslim Mission Medical Team and the people of Ashifla Otaten, dated 3 June 2023";
        yield return ("mission-water-tank-v1", tank);
    }

    private static MissionActivity New(string title, string summary, string story, string source) => new()
    { Title = title, Summary = summary, Story = story, SourceNote = source, IsPublished = true, IsShowcased = true };

    private static MissionActivity Certificate(string title, string recipient, string location, string number, string image)
    {
        var article = New(title, $"Clothing, children's shoes, food items and cash donated to {recipient}.",
            $"VitalReach International's donation certificate records a contribution of clothing, children's shoes, food items and cash to {recipient}, in {location}.\n\nThe contribution forms part of the organization's community development and humanitarian support work.\n\nCertificate reference: {number}.",
            $"Owner-supplied certificate {number}. Donation date not stated; year in certificate identifier is not treated as an event date. No cash amount supplied.");
        article.Location = location;
        article.ImageUrl = $"/images/mission/{image}";
        article.ImageAlt = $"VitalReach International donation certificate {number} for {recipient}";
        return article;
    }
}
