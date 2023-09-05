namespace Palace.WebApp.Pages.Components;

public partial class Tabs : ComponentBase
{
    [Parameter]
    public RenderFragment ChildContent { get; set; } = default!;

    [Parameter]
    public EventCallback<Tab> OnTabChanged { get; set; }

    Tab activeTab = default!;
    List<Tab> TabList = new();

    public void AddTab(Tab tab)
    {
        if (TabList.Any(i => i.Name == tab.Name))
        {
            return;
        }
        TabList.Add(tab);
        if (TabList.Count == 1)
        {
            activeTab = tab;
            tab.Active = "active";
        }
        else
        {
            tab.SetVisibility(false);
        }
        StateHasChanged();
    }

    async Task ShowTab(Tab tab)
    {
        activeTab = tab;
        foreach (var item in TabList)
        {
            if (item.Name == tab.Name)
            {
                item.SetVisibility(true);
                item.Active = "active";
                if (item.OnClick.HasDelegate)
                {
                    await item.OnClick.InvokeAsync();
                }
            }
            else
            {
                item.SetVisibility(false);
                item.Active = string.Empty;
            }
        }
        if (OnTabChanged.HasDelegate)
        {
            await OnTabChanged.InvokeAsync(tab);
        }
        StateHasChanged();
    }
}

