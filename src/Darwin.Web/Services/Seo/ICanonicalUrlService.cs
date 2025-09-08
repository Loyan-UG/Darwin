namespace Darwin.Web.Services.Seo
{
    /// <summary>
    /// Builds canonical URLs for public pages based on culture and slugs.
    /// </summary>
    public interface ICanonicalUrlService
    {
        string Page(string culture, string slug);         // e.g., /de-DE/page/ueber-uns
        string Category(string culture, string slug);     // e.g., /de-DE/c/obst
        string Product(string culture, string slug);      // e.g., /de-DE/p/bio-apfel
        string Absolute(string relative);                 // absolute with current host
    }
}
