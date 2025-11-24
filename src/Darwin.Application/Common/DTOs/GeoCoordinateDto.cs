using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Darwin.Application.Common.DTOs
{
    /// <summary>
    /// DTO representation of GeoCoordinate value object for Application layer.
    /// </summary>
    public sealed class GeoCoordinateDto
    {
        /// <summary>
        /// Gets the latitude in decimal degrees. Valid range: -90..+90.
        /// </summary>
        public double Latitude { get; init; }

        /// <summary>
        /// Gets the longitude in decimal degrees. Valid range: -180..+180.
        /// </summary>
        public double Longitude { get; init; }

        /// <summary>
        /// Gets the optional altitude in meters above sea level.
        /// This is rarely needed for mobile use-cases but is included
        /// to keep the contract aligned with the domain value object.
        /// </summary>
        public double? AltitudeMeters { get; init; }
    }
}
