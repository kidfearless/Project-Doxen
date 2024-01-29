using Microsoft.AspNetCore.Components;

using MudBlazor;

using ProjectDoxen.Logic;
using ProjectDoxen.Manager;

namespace ProjectDoxen.Components;

public partial class ListApisComponent
{
  public IEnumerable<ApiBuild> ApiBuilds { get; private set; } = [];
  [Inject] AzureService Azure { get; set; } = null!;
  [Inject] IDialogService DialogService { get; set; } = null!;

  protected override async Task OnInitializedAsync()
  {
    await GetApis();
  }

  private async Task GetApis()
  {
    AzureArray<ApiBuild>? res = await Azure.GetApiBuildsAsync();

    this.ApiBuilds = res.Value;
  }

  public async Task OpenBuildStatus(string url)
  {
    var parameters = new DialogParameters();
    var options = new DialogOptions();
    parameters.Add("BuildUrl", url);

    await DialogService.ShowAsync(typeof(BuildStatusComponent), "BuildStatus", parameters, options);

  }
}