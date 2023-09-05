namespace Palace.WebApp.Pages.Components;

public class CustomValidator : ComponentBase
{
    private ValidationMessageStore _messageStore = default!;
    [CascadingParameter]
    public EditContext CurrentEditContext { get; set; } = default!;

    protected override void OnInitialized()
    {
        if (CurrentEditContext == null)
        {
            throw new InvalidOperationException();
        }
        _messageStore = new(CurrentEditContext);
        CurrentEditContext.OnValidationRequested += (s, arg) => _messageStore.Clear();
    }

    public void DisplayErrors(string error)
    {
        _messageStore.Add(CurrentEditContext.Field("all"), error);
        CurrentEditContext.NotifyValidationStateChanged();
    }

    public void DisplayErrors(Dictionary<string, List<string>> errors)
    {
        foreach (var error in errors)
        {
            _messageStore.Add(CurrentEditContext.Field(error.Key), error.Value);
        }
        CurrentEditContext.NotifyValidationStateChanged();
    }

    public void DisplayErrors(FluentValidation.Results.ValidationResult validation)
    {
        foreach (var error in validation.Errors)
        {
            _messageStore.Add(CurrentEditContext.Field(error.PropertyName), error.ErrorMessage);
        }
        CurrentEditContext.NotifyValidationStateChanged();
    }

	public void DisplayErrors(List<FluentValidation.Results.ValidationFailure> errors)
	{
		foreach (var error in errors)
		{
			_messageStore.Add(CurrentEditContext.Field(error.PropertyName), error.ErrorMessage);
		}
		CurrentEditContext.NotifyValidationStateChanged();
	}


	public void DisplayErrors(Exception ex)
    {
        _messageStore.Add(CurrentEditContext.Field("all"), ex.Message);
        CurrentEditContext.NotifyValidationStateChanged();
    }
}
