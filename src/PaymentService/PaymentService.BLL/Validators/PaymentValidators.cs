namespace PaymentService.BLL.Validators;

using FluentValidation;
using PaymentService.BLL.DTOs;
using PaymentService.Domain.Enums;

public sealed class ValidateApplePurchaseDtoValidator : AbstractValidator<ValidateApplePurchaseDto>
{
    public ValidateApplePurchaseDtoValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x).Must(x => !string.IsNullOrWhiteSpace(x.TransactionId) || !string.IsNullOrWhiteSpace(x.ReceiptData))
            .WithMessage("TransactionId or ReceiptData is required.");
    }
}

public sealed class ValidateGooglePurchaseDtoValidator : AbstractValidator<ValidateGooglePurchaseDto>
{
    public ValidateGooglePurchaseDtoValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.PurchaseToken).NotEmpty();
        RuleFor(x => x.PackageName).NotEmpty();
    }
}

public sealed class CreateProfessionalSubscriptionDtoValidator : AbstractValidator<CreateProfessionalSubscriptionDto>
{
    public CreateProfessionalSubscriptionDtoValidator()
    {
        RuleFor(x => x.ProfessionalId).NotEmpty();
        RuleFor(x => x.PlanCode).Must(x => x is PlanCode.ProBasic or PlanCode.ProPlus or PlanCode.ProPremium);
        RuleFor(x => x.SuccessUrl).NotEmpty();
        RuleFor(x => x.CancelUrl).NotEmpty();
    }
}

public sealed class ChangeProfessionalPlanDtoValidator : AbstractValidator<ChangeProfessionalPlanDto>
{
    public ChangeProfessionalPlanDtoValidator()
    {
        RuleFor(x => x.ProfessionalId).NotEmpty();
        RuleFor(x => x.PlanCode).Must(x => x is PlanCode.ProFree or PlanCode.ProBasic or PlanCode.ProPlus or PlanCode.ProPremium);
    }
}
