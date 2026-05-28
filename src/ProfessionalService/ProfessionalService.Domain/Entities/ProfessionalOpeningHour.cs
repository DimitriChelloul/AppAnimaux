namespace ProfessionalService.Domain.Entities;

public sealed class ProfessionalOpeningHour
{
    public Guid Id { get; init; }
    public Guid ProfessionalId { get; init; }
    public short DayOfWeek { get; init; }
    public TimeOnly? OpensAt { get; init; }
    public TimeOnly? ClosesAt { get; init; }
    public bool IsClosed { get; init; }
}
