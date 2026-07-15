using FluentValidation;
using FluentValidation.AspNetCore;
using PaymentService.BLL.DTOs;
using PaymentService.BLL.Interfaces;
using PaymentService.BLL.Options;
using PaymentService.BLL.Providers;
using PaymentService.BLL.Services;
using PaymentService.BLL.Validators;
using PaymentService.DAL.Interfaces;
using PaymentService.DAL.Repositories;
using PaymentService.DAL.UnitOfWork;
using Shared.Messaging.Abstractions;
using Shared.Messaging.Outbox;
using Shared.Messaging.RabbitMq;
using Shared.Messaging.Routing;
using Shared.Messaging.Extensions;
using Shared.Persistence.Extensions;
using Shared.Persistence.Transactions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddPostgresPersistence(builder.Configuration);
builder.Services.AddOutboxMessaging(builder.Configuration);
builder.Services.Configure<StripeOptions>(builder.Configuration.GetSection("Stripe"));
builder.Services.Configure<AppleOptions>(builder.Configuration.GetSection("Apple"));
builder.Services.Configure<GooglePlayOptions>(builder.Configuration.GetSection("GooglePlay"));
builder.Services.Configure<PaymentOptions>(builder.Configuration.GetSection("Payment"));

builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
builder.Services.AddScoped<IUserSubscriptionRepository, UserSubscriptionRepository>();
builder.Services.AddScoped<IProfessionalSubscriptionRepository, ProfessionalSubscriptionRepository>();
builder.Services.AddScoped<ISubscriptionInvoiceRepository, SubscriptionInvoiceRepository>();
builder.Services.AddScoped<ISubscriptionEntitlementRepository, SubscriptionEntitlementRepository>();
builder.Services.AddScoped<IExternalPurchaseReceiptRepository, ExternalPurchaseReceiptRepository>();
builder.Services.AddScoped<IWebhookEventRepository, WebhookEventRepository>();
builder.Services.AddScoped<IPaymentAuditLogRepository, PaymentAuditLogRepository>();
builder.Services.AddScoped<IPaymentUnitOfWork, PaymentUnitOfWork>();

builder.Services.AddScoped<IPaymentAppService, PaymentAppService>();
builder.Services.AddScoped<SubscriptionEventPublisher>();
builder.Services.AddScoped<IUserSubscriptionService, UserSubscriptionService>();
builder.Services.AddScoped<IProfessionalSubscriptionService, ProfessionalSubscriptionService>();
builder.Services.AddScoped<ISubscriptionPlanService, SubscriptionPlanService>();
builder.Services.AddScoped<ISubscriptionEntitlementService, SubscriptionEntitlementService>();
builder.Services.AddScoped<IApplePurchaseService, ApplePurchaseService>();
builder.Services.AddScoped<IGooglePurchaseService, GooglePurchaseService>();
builder.Services.AddScoped<ISubscriptionWebhookService, SubscriptionWebhookService>();
builder.Services.AddScoped<IPaymentAuditService, PaymentAuditService>();
builder.Services.AddHttpClient<IStripeBillingService, StripeBillingService>();
builder.Services.AddScoped<IValidator<ValidateApplePurchaseDto>, ValidateApplePurchaseDtoValidator>();
builder.Services.AddScoped<IValidator<ValidateGooglePurchaseDto>, ValidateGooglePurchaseDtoValidator>();
builder.Services.AddScoped<IValidator<CreateProfessionalSubscriptionDto>, CreateProfessionalSubscriptionDtoValidator>();
builder.Services.AddScoped<IValidator<ChangeProfessionalPlanDto>, ChangeProfessionalPlanDtoValidator>();


var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseTransactionalOutbox();
app.MapControllers();

app.Run();
