namespace PSA.WebApp.Models;

public class BreadcrumbItemViewModel
{
    public required string Label { get; init; }
    public string? Url { get; init; }
    public bool IsCurrent { get; init; }
}
