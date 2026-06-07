namespace PaymentService.Domain.ValueObjects;

public readonly record struct Money(decimal Amount, string Currency)
{
    public static Money Eur(decimal amount) => new(amount, "EUR");
}
