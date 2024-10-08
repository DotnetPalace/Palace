﻿using FluentValidation;
using FluentValidation.Results;

using Microsoft.EntityFrameworkCore;

namespace Palace.Server.Services;

public class ServiceSettingsRepository(
	IDbContextFactory<PalaceDbContext> dbContextFactory,
    IValidator<MicroServiceSettings> validator
	)
{
    public async Task<IEnumerable<MicroServiceSettings>> GetAll()
	{
		var db = await dbContextFactory.CreateDbContextAsync();
		return await db.MicroServiceSettings.ToListAsync();
	}

	public async Task<(bool success, Guid id, List<ValidationFailure> brokenRules)> SaveServiceSettings(MicroServiceSettings serviceSettings)
	{
		Sanitize(serviceSettings);
		var validation = await validator.ValidateAsync(serviceSettings);
		if (!validation.IsValid)
		{
			return (false, Guid.Empty, validation.Errors.ToList());
		}
		var db = await dbContextFactory.CreateDbContextAsync();
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
			await db.SaveChangesAsync();
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

	public async Task<bool> RemoveServiceSettings(MicroServiceSettings serviceSettings)
	{
		var db = await dbContextFactory.CreateDbContextAsync();
		var existing = await db.MicroServiceSettings.FindAsync(serviceSettings.Id);
		if (existing is null)
		{
			return false;
		}
		db.MicroServiceSettings.Attach(serviceSettings);
		db.Entry(serviceSettings).State = EntityState.Deleted;
		var changeCount = await db.SaveChangesAsync();
		return changeCount > 0;
	}

	public async Task<ArgumentsByHost?> GetArgumentsByHostForService(string hostName, Guid serviceSettingsId)
	{
		var db = await dbContextFactory.CreateDbContextAsync();
		var query = from abh in db.ArgumentsByHosts
					where abh.HostName == hostName
						&& abh.ServiceSettingId == serviceSettingsId
					select abh;

		var result = await query.FirstOrDefaultAsync();
		return result;
	}

	public async Task<List<Palace.Shared.ArgumentsByHost>> GetArgumentsByService(Guid serviceSettingsId)
	{
		var db = await dbContextFactory.CreateDbContextAsync();
		var result = await db.ArgumentsByHosts.Where(i => i.ServiceSettingId == serviceSettingsId).ToListAsync();
		return result;
	}

	public async Task<MicroServiceSettings?> GetByServiceName(string serviceName)
	{
		var db = await dbContextFactory.CreateDbContextAsync();
		var result = await db.MicroServiceSettings.FirstOrDefaultAsync(i => i.ServiceName == serviceName);
		return result;
	}

	public async Task<IEnumerable<MicroServiceSettings>> GetListByPackageFileName(string packageFileName)
	{
		var db = await dbContextFactory.CreateDbContextAsync();
		var result = await db.MicroServiceSettings.Where(i => i.PackageFileName == packageFileName).ToListAsync();
		return result;
	}

	public async Task<int> SaveArgumentsByHost(List<ArgumentsByHost> argumentsByHosts)
	{
		var db = await dbContextFactory.CreateDbContextAsync();
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
