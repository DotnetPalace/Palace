using Microsoft.AspNetCore.Components.Web;

namespace Palace.WebApp.Pages;

public partial class UploadPackage
{
    [Inject]
    Palace.Server.Configuration.GlobalSettings Settings { get; set; } = default!;
    [Inject] 
    NavigationManager NavigationManager { get; set; } = default!;
    [Inject] 
    ILogger<UploadPackage> Logger { get; set; } = default!;

    CustomValidator customValidator = new();
    Palace.Server.Models.UploadedFile uploadedFile = new();
    bool uploading = false;
    ElementReference fileDropContainer;
    string HoverClass = null!;

    public async Task LoadFile(InputFileChangeEventArgs e)
    {
        uploading = true;
        StateHasChanged();

        if (!e.File.Name.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
        {
            customValidator.DisplayErrors(new Dictionary<string, List<string>>
            {
                { "FileName", new List<string> { "Zipfile only allowed" } }
            });
            return;
        }

        if (e.File.Size == 0)
        {
            customValidator.DisplayErrors(new Dictionary<string, List<string>>
            {
                { "FileName", new List<string> { "Zipfile is empty" } }
            });
            return;
        }
        var fileName = Path.Combine(Settings.TempFolder, $"{Guid.NewGuid()}-{e.File.Name}");

        using (var stream = e.File.OpenReadStream(100 * 1024 * 1024))
        {
            using (var fileContent = new StreamContent(stream))
            {
                using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    await fileContent.CopyToAsync(fs);
                    Logger.LogInformation("File {0} uploaded", fileName);
                }
            }
        }

        // TODO : Verifier si le zip est correct

        var finalFileName = Path.Combine(Settings.StagingFolder, e.File.Name);
        await Task.Delay(1000);
        try
        {
            File.Copy(fileName, finalFileName, true);
            File.Delete(fileName);
        }
        catch (Exception ex)
        {
            customValidator.DisplayErrors(new Dictionary<string, List<string>>
            {
                { "FileName", new List<string> { ex.Message } }
            });
            uploading = false;
            StateHasChanged();
            return;
        }

        NavigationManager.NavigateTo("/Packages");
    }

    void OnDragEnter(DragEventArgs e) => HoverClass = "hover";
    void OnDragLeave(DragEventArgs e) => HoverClass = string.Empty;

}