using System.Text.Json.Serialization;

namespace JobTracker.Api.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ApplicationStatus
{
    Applied,
    Interview,
    Offer,
    Rejected
}