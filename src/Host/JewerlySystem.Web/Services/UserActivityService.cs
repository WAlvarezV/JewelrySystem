using JewerlySystem.Common.Application;
using JewerlySystem.Common.Domain;
using JewerlySystem.Common.Infrastructure.Tenant;
using JewerlySystem.Web.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JewerlySystem.Web.Services;

public class UserActivityService : 
    IUserActivityTracker,
    IRequestHandler<GetUserProductivityQuery, Result<UserProductivityReportDto>>
{
    private readonly JewerlyDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UserActivityService> _logger;

    public UserActivityService(
        JewerlyDbContext dbContext,
        ITenantContext tenantContext,
        ICurrentUserService currentUserService,
        ILogger<UserActivityService> logger)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task TrackActivityAsync(
        string actionKey,
        string category,
        string description,
        int? weightScore = null,
        string entityName = "Item",
        string? entityReference = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = _tenantContext.CurrentTenant?.Id ?? "pomona";
            var userName = await _currentUserService.GetCurrentUserNameAsync() ?? "system";
            var userId = _currentUserService.UserId ?? userName;
            var weight = weightScore ?? ActionWeightRegistry.GetDefaultWeight(actionKey);
            var def = ActionWeightRegistry.GetDefinition(actionKey);
            var moduleKey = def?.ModuleKey ?? "INVENTORY";

            var log = new UserActivityLog(
                tenantId: tenantId,
                userId: userId,
                userName: userName,
                moduleKey: moduleKey,
                actionKey: actionKey,
                category: category,
                description: description,
                weightScore: weight,
                entityName: entityName,
                entityReference: entityReference);

            _dbContext.UserActivityLogs.Add(log);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not persist user activity log for {ActionKey}", actionKey);
        }
    }

    public async Task<Result<UserProductivityReportDto>> Handle(
        GetUserProductivityQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var tenantId = _tenantContext.CurrentTenant?.Id ?? "pomona";
            var dateFrom = request.DateFrom ?? DateTime.UtcNow.Date.AddDays(-30);
            var dateTo = request.DateTo ?? DateTime.UtcNow;

            // Asegurar que dateTo cubra el día completo
            if (dateTo.TimeOfDay == TimeSpan.Zero)
            {
                dateTo = dateTo.Date.AddDays(1).AddTicks(-1);
            }

            // Pesos efectivos (personalizados si se envían, o por defecto)
            var effectiveWeights = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in ActionWeightRegistry.Definitions)
            {
                effectiveWeights[kvp.Key] = kvp.Value.DefaultWeight;
            }
            if (request.CustomWeights != null)
            {
                foreach (var (k, v) in request.CustomWeights)
                {
                    effectiveWeights[k] = v;
                }
            }

            // 1. Consultar UserActivityLogs
            var logQuery = _dbContext.UserActivityLogs
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.CreatedAt >= dateFrom && x.CreatedAt <= dateTo);

            if (!string.IsNullOrWhiteSpace(request.UserId))
            {
                logQuery = logQuery.Where(x => x.UserId == request.UserId || x.UserName == request.UserId);
            }

            if (!string.IsNullOrWhiteSpace(request.ModuleKey) && !request.ModuleKey.Equals("ALL", StringComparison.OrdinalIgnoreCase))
            {
                logQuery = logQuery.Where(x => x.ModuleKey == request.ModuleKey);
            }

            var rawLogs = await logQuery.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);

            // 2. Trazabilidad retroactiva/híbrida: Incluir creaciones y modificaciones de Items preexistentes
            var knownRefKeys = new HashSet<string>(rawLogs.Select(l => $"{l.ActionKey}_{l.EntityReference}"));
            var synthesizedLogs = new List<UserActivityLogItemDto>();

            try
            {
                var existingItems = await _dbContext.Items
                    .AsNoTracking()
                    .Where(i => i.CreatedAt >= dateFrom && i.CreatedAt <= dateTo && !string.IsNullOrEmpty(i.CreatedBy))
                    .ToListAsync(cancellationToken);

                foreach (var item in existingItems)
                {
                    var isJewel = item.ItemTypeId != 14; // En Pomona 14 = Relojes, resto Joyería
                    var actionKey = isJewel ? ActionWeightRegistry.InventoryCreateJewel : ActionWeightRegistry.InventoryCreateWatch;
                    var keyCheck = $"{actionKey}_{item.Reference}";

                    if (!knownRefKeys.Contains(keyCheck))
                    {
                        var weight = effectiveWeights.GetValueOrDefault(actionKey, 10);
                        var def = ActionWeightRegistry.GetDefinition(actionKey);
                        synthesizedLogs.Add(new UserActivityLogItemDto(
                            Id: -item.Id,
                            UserId: item.CreatedBy ?? "system",
                            UserName: item.CreatedBy ?? "system",
                            ActionKey: actionKey,
                            ActionName: def?.DisplayName ?? "Creación de Artículo",
                            Category: ActionWeightRegistry.CategoryNewRecord,
                            Description: $"Creó {(isJewel ? "Joya" : "Reloj")} Ref #{item.Reference} - {item.Description}",
                            WeightScore: weight,
                            EntityName: "Item",
                            EntityReference: item.Reference.ToString(),
                            CreatedAt: item.CreatedAt,
                            Icon: def?.Icon ?? "bi-gem",
                            ColorHex: def?.ColorHex ?? "#10B981"
                        ));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error synthesizing baseline items for user productivity report.");
            }

            // 3. Mapear raw logs a DTOs
            var allLogDtos = rawLogs.Select(l =>
            {
                var weight = effectiveWeights.GetValueOrDefault(l.ActionKey, l.WeightScore);
                var def = ActionWeightRegistry.GetDefinition(l.ActionKey);
                return new UserActivityLogItemDto(
                    Id: l.Id,
                    UserId: l.UserId,
                    UserName: l.UserName,
                    ActionKey: l.ActionKey,
                    ActionName: def?.DisplayName ?? l.ActionKey,
                    Category: l.Category,
                    Description: l.Description,
                    WeightScore: weight,
                    EntityName: l.EntityName,
                    EntityReference: l.EntityReference,
                    CreatedAt: l.CreatedAt,
                    Icon: def?.Icon ?? "bi-check2",
                    ColorHex: def?.ColorHex ?? "#3B82F6"
                );
            }).Concat(synthesizedLogs).OrderByDescending(x => x.CreatedAt).ToList();

            // 4. Calcular Métricas por Usuario
            var userGroups = allLogDtos.GroupBy(x => x.UserName).ToList();
            var totalTeamPoints = allLogDtos.Sum(x => x.WeightScore);

            var userPerformances = new List<UserPerformanceDto>();
            foreach (var grp in userGroups)
            {
                var userName = grp.Key;
                var userId = grp.First().UserId;
                var totalUserPoints = grp.Sum(x => x.WeightScore);
                var totalUserActions = grp.Count();

                var newRecords = grp.Where(x => x.Category == ActionWeightRegistry.CategoryNewRecord).ToList();
                var updates = grp.Where(x => x.Category == ActionWeightRegistry.CategoryUpdate).ToList();
                var quickAudits = grp.Where(x => x.Category == ActionWeightRegistry.CategoryAuditQuick).ToList();
                var discharges = grp.Where(x => x.Category == ActionWeightRegistry.CategoryDischarge).ToList();

                var breakdown = grp
                    .GroupBy(x => x.ActionKey)
                    .ToDictionary(g => g.Key, g => g.Count());

                var teamShare = totalTeamPoints > 0 ? Math.Round((double)totalUserPoints / totalTeamPoints * 100, 1) : 0;

                userPerformances.Add(new UserPerformanceDto(
                    UserId: userId,
                    UserName: userName,
                    TotalPoints: totalUserPoints,
                    TotalActions: totalUserActions,
                    NewRecordsCount: newRecords.Count,
                    NewRecordsPoints: newRecords.Sum(x => x.WeightScore),
                    UpdatesCount: updates.Count,
                    UpdatesPoints: updates.Sum(x => x.WeightScore),
                    QuickAuditsCount: quickAudits.Count,
                    QuickAuditsPoints: quickAudits.Sum(x => x.WeightScore),
                    DischargesCount: discharges.Count,
                    DischargesPoints: discharges.Sum(x => x.WeightScore),
                    LastActivityAt: grp.Max(x => x.CreatedAt),
                    TeamSharePercent: teamShare,
                    ActionBreakdown: breakdown
                ));
            }

            userPerformances = userPerformances.OrderByDescending(x => x.TotalPoints).ToList();

            // 5. Distribución por Categoría de Acción
            var actionDistributions = allLogDtos
                .GroupBy(x => x.ActionKey)
                .Select(grp =>
                {
                    var actionKey = grp.Key;
                    var def = ActionWeightRegistry.GetDefinition(actionKey);
                    var count = grp.Count();
                    var pts = grp.Sum(x => x.WeightScore);
                    var pct = totalTeamPoints > 0 ? Math.Round((double)pts / totalTeamPoints * 100, 1) : 0;

                    return new ActionDistributionDto(
                        Category: def?.Category ?? grp.First().Category,
                        ActionKey: actionKey,
                        DisplayName: def?.DisplayName ?? actionKey,
                        TotalCount: count,
                        TotalPoints: pts,
                        PercentageOfPoints: pct,
                        ColorHex: def?.ColorHex ?? "#6B7280",
                        Icon: def?.Icon ?? "bi-activity"
                    );
                })
                .OrderByDescending(x => x.TotalPoints)
                .ToList();

            // 6. Tendencia Diaria de Actividad
            var dailyTrend = allLogDtos
                .GroupBy(x => x.CreatedAt.Date)
                .OrderBy(g => g.Key)
                .Select(grp => new DailyActivityPointDto(
                    DateLabel: grp.Key.ToString("dd MMM"),
                    Date: grp.Key,
                    TotalPoints: grp.Sum(x => x.WeightScore),
                    TotalActions: grp.Count(),
                    NewRecords: grp.Count(x => x.Category == ActionWeightRegistry.CategoryNewRecord),
                    Updates: grp.Count(x => x.Category == ActionWeightRegistry.CategoryUpdate),
                    QuickAudits: grp.Count(x => x.Category == ActionWeightRegistry.CategoryAuditQuick)
                ))
                .ToList();

            // 7. Resumen Ejecutivo Global
            var topPerformer = userPerformances.FirstOrDefault();
            var summary = new UserProductivitySummaryDto(
                TotalWeightedPoints: totalTeamPoints,
                TotalActionsCount: allLogDtos.Count,
                TotalNewRecordsCount: allLogDtos.Count(x => x.Category == ActionWeightRegistry.CategoryNewRecord),
                TotalNewRecordsPoints: allLogDtos.Where(x => x.Category == ActionWeightRegistry.CategoryNewRecord).Sum(x => x.WeightScore),
                TotalUpdatesCount: allLogDtos.Count(x => x.Category == ActionWeightRegistry.CategoryUpdate),
                TotalUpdatesPoints: allLogDtos.Where(x => x.Category == ActionWeightRegistry.CategoryUpdate).Sum(x => x.WeightScore),
                TotalQuickAuditsCount: allLogDtos.Count(x => x.Category == ActionWeightRegistry.CategoryAuditQuick),
                TotalQuickAuditsPoints: allLogDtos.Where(x => x.Category == ActionWeightRegistry.CategoryAuditQuick).Sum(x => x.WeightScore),
                TotalDischargesCount: allLogDtos.Count(x => x.Category == ActionWeightRegistry.CategoryDischarge),
                ActiveUsersCount: userPerformances.Count,
                TopPerformerName: topPerformer?.UserName ?? "Sin actividad",
                TopPerformerPoints: topPerformer?.TotalPoints ?? 0
            );

            var report = new UserProductivityReportDto(
                DateFrom: dateFrom,
                DateTo: dateTo,
                Summary: summary,
                UserPerformances: userPerformances,
                ActionDistribution: actionDistributions,
                DailyTrend: dailyTrend,
                RecentLogs: allLogDtos.Take(100).ToList()
            );

            return Result.Success(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating user productivity report.");
            return Result.Failure<UserProductivityReportDto>(
                Error.Failure("Productivity.ReportError", "No se pudo generar el reporte de productividad y auditoría."));
        }
    }
}
