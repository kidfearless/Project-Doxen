
using Microsoft.AspNetCore.Components;

using ProjectDoxen.Logic;

namespace ProjectDoxen.Components.Pages;

public partial class Home
{
	[Inject] public AzureService Azure { get; set; } = null!;
	protected override async Task OnInitializedAsync()
	{
		await RunTest();
	}

	public async Task RunTest()
	{
		var limits = await Azure.GetRateLimits();

	}
}