using FluentValidation;
using FluentValidation.Results;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

using Palace.Server.Pages.Components;

namespace Palace.Server.Services;

public class ServiceSettingsRepository
{
	private readonly IDbContextFactory<PalaceDbContext> _dbContextFactory;
	private readonly IValidator<MicroServiceSettings> _validator;

	public ServiceSettingsRepository(IDbContextFactory<PalaceDbContext> dbContextFactory,
		IValidator<MicroServiceSettings> validator)
    {
		_dbContextFactory = dbContextFactory;
		_validator = validator;
	}

	public async Task<IEnumerable<MicroServiceSettings>> GetAll()
	{
		var db = await _dbContextFactory.CreateDbContextAsync();
		return await db.MicroServiceSettings.ToListAsync();
	}

	public async Task<(bool success, List<ValidationFailure> brokenRules)> SaveServiceSettings(MicroServiceSettings serviceSettings)
	{
		Sanitize(serviceSettings);
		var validation = await _validator.ValidateAsync(serviceSettings);
		if (!validation.IsValid)
		{
			return (false, validation.Errors.ToList());
		}
		var db = await _dbContextFactory.CreateDbContextAsync();
		if (serviceSettings.Id == Guid.Empty)
		{
			serviceSettings.Id = Guid.NewGuid();
			db.MicroServiceSettings.Add(serviceSettings);
			db.Entry(serviceSettings).State = EntityState.Added;
		}
		else
		{
			db.MicroServiceSettings.Attach(serviceSettings);
			db.Entry(serviceSettings).State = EntityState.Modified;
		}

		try
		{
			var changeCount = await db.SaveChangesAsync();
		}
		catch (Exception ex)
		{
			return (false, new List<ValidationFailure>
			{
				{ 
					new ValidationFailure
					{
						ErrorMessage = ex.Message
					}
				}
			});
		}

		return (true, new List<ValidationFailure>());
	}

	private void Sanitize(MicroServiceSettings serviceSettings)
	{
		serviceSettings.ServiceName = serviceSettings.ServiceName.Trim();
		serviceSettings.MainAssembly = serviceSettings.MainAssembly.TrimEnd('/');
		serviceSettings.Arguments = serviceSettings.Arguments?.Trim();
		serviceSettings.GroupName = serviceSettings.GroupName?.Trim();
		serviceSettings.PackageFileName = serviceSettings.PackageFileName.Trim();
	}
}
