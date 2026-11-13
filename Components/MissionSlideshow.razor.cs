#nullable enable
namespace VitalReach.Web.Components;

public partial class MissionSlideshow
{
    private sealed record Slide(string File, string Alt, string Caption)
    {
        public string Image => $"/images/mission/{File}.jpg";
    }

    private static readonly Slide[] Slides =
    [
        new("health-screen-04", "Community members, including a wheelchair user, seated with healthcare workers at screening tables", "Making time for community health screenings."),
        new("madarasa-visit-donations", "Young people standing together indoors during a donation visit", "Coming together during a madarasa donation visit."),
        new("orphanage-visit-outdoors", "Children and adults gathered outdoors beside bags and boxes of donated supplies", "Everyday essentials shared during an orphanage visit."),
        new("orphanage-visit-presentation-2", "A group of children and young people gathered indoors during a visit", "Moments of connection during a community visit."),
        new("orphanage-visit-together-remastered", "A woman in a VitalReach shirt seated with children beside donated shoes and supplies", "Sharing time, care and company."),
        new("rashias-community-borehole-donations", "Two men standing beside a community water tank and a Rashias Foundation dedication plaque", "Water-access support through Rashias Foundation’s community work."),
        new("health-screen-01", "A community health outreach event with consultation tables beneath outdoor canopies", "Bringing healthcare outreach into the community.")
    ];

    private int ActiveIndex;
    private Slide Current => Slides[ActiveIndex];
    private void Select(int index) => ActiveIndex = index;
    private void Previous() => ActiveIndex = (ActiveIndex + Slides.Length - 1) % Slides.Length;
    private void Next() => ActiveIndex = (ActiveIndex + 1) % Slides.Length;
}
