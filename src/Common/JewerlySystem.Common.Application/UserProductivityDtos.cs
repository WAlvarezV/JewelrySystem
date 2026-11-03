namespace JewerlySystem.Common.Application;

public record UserProductivitySummaryDto(
    int TotalWeightedPoints,
    int TotalActionsCount,
    int TotalNewRecordsCount,
    int TotalNewRecordsPoints,
    int TotalUpdatesCount,
    int TotalUpdatesPoints,
    int TotalQuickAuditsCount,
    int TotalQuickAuditsPoints,
    int TotalDischargesCount,
    int ActiveUsersCount,
    string TopPerformerName,
    int TopPerformerPoints);

public record UserPerformanceDto(
    string UserId,
    string UserName,
    int TotalPoints,
    int TotalActions,
    int NewRecordsCount,
    int NewRecordsPoints,
    int UpdatesCount,
    int UpdatesPoints,
    int QuickAuditsCount,
    int QuickAuditsPoints,
    int DischargesCount,
    int DischargesPoints,
    DateTime? LastActivityAt,
    double TeamSharePercent,
    Dictionary<string, int> ActionBreakdown);

public record ActionDistributionDto(
    string Category,
    string ActionKey,
    string DisplayName,
    int TotalCount,
    int TotalPoints,
    double PercentageOfPoints,
    string ColorHex,
    string Icon);

public record DailyActivityPointDto(
    string DateLabel,
    DateTime Date,
    int TotalPoints,
    int TotalActions,
    int NewRecords,
    int Updates,
    int QuickAudits);

public record UserActivityLogItemDto(
    long Id,
    string UserId,
    string UserName,
    string ActionKey,
    string ActionName,
    string Category,
    string Description,
    int WeightScore,
    string EntityName,
    string? EntityReference,
    DateTime CreatedAt,
    string Icon,
    string ColorHex);

public record UserProductivityReportDto(
    DateTime DateFrom,
    DateTime DateTo,
    UserProductivitySummaryDto Summary,
    IReadOnlyList<UserPerformanceDto> UserPerformances,
    IReadOnlyList<ActionDistributionDto> ActionDistribution,
    IReadOnlyList<DailyActivityPointDto> DailyTrend,
    IReadOnlyList<UserActivityLogItemDto> RecentLogs);

public record GetUserProductivityQuery(
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    string? UserId = null,
    string? ModuleKey = null,
    Dictionary<string, int>? CustomWeights = null) : IQuery<UserProductivityReportDto>;
