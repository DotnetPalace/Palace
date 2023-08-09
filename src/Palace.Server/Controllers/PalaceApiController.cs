﻿using System.Reflection.PortableExecutable;

using Microsoft.AspNetCore.Mvc;

using Palace.Server.Services;

namespace Palace.Server.Controllers;

[ApiController]
[Microsoft.AspNetCore.Mvc.Route("api/palace")]
public class PalaceApiController : ControllerBase
{
    private readonly Configuration.GlobalSettings _settings;
    private readonly ILogger<PalaceApiController> _logger;
    private readonly Orchestrator _orchestrator;

    public PalaceApiController(Configuration.GlobalSettings settings,
        ILogger<PalaceApiController> logger,
        Services.Orchestrator orchestrator)
    {
        _settings = settings;
        _logger = logger;
        _orchestrator = orchestrator;
    }

    [Microsoft.AspNetCore.Mvc.Route("ping")]
    [HttpGet]
    public IActionResult Ping()
    {
        return Ok(new
        {
            DateTime = DateTime.Now,
        });
    }

    [HttpGet]
    [Microsoft.AspNetCore.Mvc.Route("download/{packageFileName}")]
    public IActionResult DownloadPackage([FromHeader] string authorization, string packageFileName)
    {
        if (string.IsNullOrWhiteSpace(packageFileName))
        {
            return BadRequest();
        }

        EnsureGoodAuthorization(authorization);

        var list = _orchestrator.GetPackageInfoList();
        var serviceInfo = list.FirstOrDefault(i => i.PackageFileName.Equals(packageFileName, StringComparison.InvariantCultureIgnoreCase));
        if (serviceInfo == null)
        {
            return NotFound($"this service name {packageFileName} does not exist");
        }

        var fileName = Path.Combine(_settings.RepositoryFolder, packageFileName);

        var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
        return File(stream, "application/zip", packageFileName);
    }

    [HttpPost]
    [Microsoft.AspNetCore.Mvc.Route("upload-package")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> UploadPackage([FromHeader] string authorization)
    {
        EnsureGoodAuthorization(authorization);

        if (!Request.HasFormContentType)
        {
            var message = "accept only mimetype 'multipart/form-data'";
            _logger.LogWarning(message);
            return BadRequest(message);
        }

        var form = await Request.ReadFormAsync();

        if (!form.Files.Any())
        {
            var message = "there is no package";
            _logger.LogWarning(message);
            return BadRequest(message);
        }

        var package = form.Files.First();
        if (package is null || package.Length == 0)
        {
            var message = "package is null or empty";
            _logger.LogWarning(message);
            return BadRequest(message);
        }

        _logger.LogInformation($"{package.FileName} uploaded");
        var fileName = Path.GetFileName(package.FileName);
        var destination = Path.Combine(_settings.StagingFolder, fileName);
        if (System.IO.File.Exists(destination))
        {
            System.IO.File.Delete(destination);
        }
        using var writer = System.IO.File.Create(destination);
        await package.CopyToAsync(writer);

        _logger.LogInformation($"{package.FileName} deployed to {destination}");

        return Ok();
    }

    private void EnsureGoodAuthorization(string authorization)
    {
        if (string.IsNullOrWhiteSpace(authorization))
        {
            throw new UnauthorizedAccessException("api key needed");
        }
        if (authorization.IndexOf($"{_settings.ApiKey}", StringComparison.InvariantCultureIgnoreCase) == -1)
        {
            throw new UnauthorizedAccessException("bad api key");
        }
    }
}
