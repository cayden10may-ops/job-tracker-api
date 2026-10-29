namespace JobTracker.Api.Dtos;

public record StatsResponse(
    int Total,
    Dictionary<string, int> ByStatus,
    int ReachedInterview,
    int ReceivedOffer,
    double? InterviewRatePercent,
    double? OfferRatePercent,
    double? AverageDaysToInterview);