using JewerlySystem.Modules.Cash.Domain;

namespace JewerlySystem.Modules.Cash.Application.DTOs;

public record DailyRecordDto(
    int Id,
    DateTime Date,
    string Number,
    int? Reference,
    string Description,
    int Value,
    RecordType RecordType,
    PaymentMethod PaymentMethod);

public record ConsolidatedRecordDto(
    DateTime Date,
    int TotalIngresos,
    int TotalEgresos,
    int Balance);
