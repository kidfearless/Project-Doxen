using Microsoft.AspNetCore.Components;

using Newtonsoft.Json;

using ProjectDoxen.Logic;

namespace ProjectDoxen.Components;

public partial class BuildStatusComponent
{
  [Inject] public AzureService Azure { get; set; } = null!;

  [Parameter] public string BuildUrl { get; set; } = "";
  public string Json { get; private set; }

  protected override async Task OnParametersSetAsync()
  {
    var result = await Azure.GetBuildStatusAsync(BuildUrl);
    this.Json = JsonConvert.SerializeObject(result, Formatting.Indented);
  }
}