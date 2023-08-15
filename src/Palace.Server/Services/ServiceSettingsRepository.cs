using System.Collections.Immutable;

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

	public async Task<(bool success, Guid id, List<ValidationFailure> brokenRules)> SaveServiceSettings(MicroServiceSettings serviceSettings)
	{
		Sanitize(serviceSettings);
		var validation = await _validator.ValidateAsync(serviceSettings);
		if (!validation.IsValid)
		{
			return (false, Guid.Empty, validation.Errors.ToList());
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
			return (false, serviceSettings.Id, new List<ValidationFailure>
			{
				{ 
					new ValidationFailure
					{
						ErrorMessage = ex.Message
					}
				}
			});
		}

		return (true, serviceSettings.Id, new List<ValidationFailure>());
	}

	internal async Task<ArgumentsByHost?> GetArgumentsByHostForService(string hostName, Guid serviceSettingsId)
	{
		var db = await _dbContextFactory.CreateDbContextAsync();
		var query = from abh in db.ArgumentsByHosts
					where abh.HostName == hostName
						&& abh.ServiceSettingId == serviceSettingsId
					select abh;

		var result = await query.FirstOrDefaultAsync();
		return result;
	}

	internal async Task<List<Palace.Shared.ArgumentsByHost>> GetArgumentsByService(Guid serviceSettingsId)
	{
		var db = await _dbContextFactory.CreateDbContextAsync();
		var result = await db.ArgumentsByHosts.Where(i => i.ServiceSettingId == serviceSettingsId).ToListAsync();
		return result;
	}

	internal async Task<MicroServiceSettings?> GetByServiceName(string serviceName)
	{
		var db = await _dbContextFactory.CreateDbContextAsync();
		var result = await db.MicroServiceSettings.FirstOrDefaultAsync(i => i.ServiceName == serviceName);
		return result;
	}

	internal async Task<int> SaveArgumentsByHost(List<ArgumentsByHost> argumentsByHosts)
	{
		var db = await _dbContextFactory.CreateDbContextAsync();
		foreach (var item in argumentsByHosts)
		{
			if (item.Id == Guid.Empty) // New
			{
				if (string.IsNullOrWhiteSpace(item.Arguments))
				{
					// Dont save
					continue;
				}
				item.Id = Guid.NewGuid();
				item.Arguments = item.Arguments.Trim();
				db.ArgumentsByHosts.Add(item);
				db.Entry(item).State = EntityState.Added;
			}
			else
			{
				if (string.IsNullOrWhiteSpace(item.Arguments))
				{
					db.ArgumentsByHosts.Remove(item);
					db.Entry(item).State = EntityState.Deleted;
				}
				else
				{
					item.Arguments = item.Arguments.Trim();
					db.ArgumentsByHosts.Attach(item);
					db.Entry(item).State = EntityState.Modified;
				}
			}
		}
		var changeCount = await db.SaveChangesAsync();
		return changeCount;
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
