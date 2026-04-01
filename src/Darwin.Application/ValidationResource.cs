namespace Darwin.Application
{
    /// <summary>
    /// Marker type for shared application-layer validation resources.
    /// Hosts such as WebAdmin and WebApi can resolve these resources through IStringLocalizer
    /// without taking a dependency on one another's UI resource trees.
    /// </summary>
    public sealed class ValidationResource
    {
    }
}
