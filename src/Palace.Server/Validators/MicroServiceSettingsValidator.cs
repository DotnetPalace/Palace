using FluentValidation;

namespace Palace.Server.Validators; 

public class MicroServiceSettingsValidator : FluentValidation.AbstractValidator<Palace.Shared.MicroServiceSettings>
{
    public MicroServiceSettingsValidator() 
    {
        RuleFor(i => i.ServiceName).NotEmpty().NotNull();
        RuleFor(i => i.ServiceName).CustomAsync(async (serviceName, ctx, cancellationToken) =>
        {
            await Task.Yield();
            if (string.IsNullOrEmpty(serviceName))
            {
                return;
            }
            if (serviceName.IndexOf("/") > 0
                || serviceName.IndexOf("\\") > 0)
            {
                ctx.AddFailure(nameof(ctx.InstanceToValidate.ServiceName), "ServiceName must be a single word");
            }
            if (serviceName.IndexOf(" ") > 0)
            {
                ctx.AddFailure(nameof(ctx.InstanceToValidate.ServiceName), "ServiceName must be a single word");
            }
        });
        RuleFor(i => i.MainAssembly).NotEmpty().NotNull();
        RuleFor(i => i.MainAssembly).CustomAsync(async (mainAssembly, ctx, cancellationToken) =>
        {
            await Task.Yield();
            if (string.IsNullOrEmpty(mainAssembly))
            {
                return;
            }
            if (!mainAssembly.EndsWith(".dll"))
            {
                ctx.AddFailure(nameof(ctx.InstanceToValidate.MainAssembly), "MainAssembly must be a dll file with .dll extension");
            }
            if (mainAssembly.IndexOf("/") > 0
                || mainAssembly.IndexOf("\\") > 0)
            {
                ctx.AddFailure(nameof(ctx.InstanceToValidate.MainAssembly), "MainAssembly must be a full path");
            }
        });
        RuleFor(i => i.PackageFileName).NotEmpty().NotNull();
        RuleFor(i => i.PackageFileName).CustomAsync(async (packageFileName, ctx, cancellationToken) =>
        {
            await Task.Yield();
            if (string.IsNullOrEmpty(packageFileName))
            {
                return;
            }
            if (!packageFileName.EndsWith(".zip"))
            {
                ctx.AddFailure(nameof(ctx.InstanceToValidate.PackageFileName), "PackageFileName must be a zip file with .zip extension");
            }
        });
        RuleFor(i => i.ThreadLimitBeforeRestart).GreaterThanOrEqualTo(1).When(i => i.ThreadLimitBeforeRestart.HasValue);
        RuleFor(i => i.ThreadLimitBeforeAlert).GreaterThanOrEqualTo(1).When(i => i.ThreadLimitBeforeAlert.HasValue);
        RuleFor(i => i.NoHealthCheckCountBeforeRestart).GreaterThanOrEqualTo(1).When(i => i.NoHealthCheckCountBeforeRestart.HasValue);
        RuleFor(i => i.NoHealthCheckCountCountBeforeAlert).GreaterThanOrEqualTo(1).When(i => i.NoHealthCheckCountCountBeforeAlert.HasValue);
        RuleFor(i => i.MaxWorkingSetLimitBeforeRestart).GreaterThanOrEqualTo(1).When(i => i.MaxWorkingSetLimitBeforeRestart.HasValue);
        RuleFor(i => i.MaxWorkingSetLimitBeforeAlert).GreaterThanOrEqualTo(1).When(i => i.MaxWorkingSetLimitBeforeAlert.HasValue);
    }
}
