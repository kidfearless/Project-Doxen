using Microsoft.AspNetCore.Components;

using MudBlazor;

using ProjectDoxen.Manager;

namespace ProjectDoxen.Components.Pages;

public partial class Settings
{
	public MudTextField<string> TokenField { get; set; } = null!;
	[Inject] public SimpleCredentialManager Credentials { get; set; } = null!;
	[Inject] public ISnackbar Snackbar { get; set; } = null!;

	public string PasswordInputIcon = Icons.Material.Filled.VisibilityOff;
	public InputType PasswordInput = InputType.Password;
	public bool IsShow = false;

	protected override void OnAfterRender(bool firstRender)
	{
		if (firstRender)
		{
			TokenField.SetText(GetDisplayToken());
		}
	}

	private string GetDisplayToken()
	{
		var blob = Credentials.GetCredential(WindowsCredEnum.PersonalAccessToken)?.CredentialBlob;
		if (blob is null)
		{
			return "";
		}

		if (IsShow)
		{
			return blob;
		}
		string fake = new('*', blob.Length);

		return fake;
	}

	public void SaveToken()
	{
		Credentials.SaveCredential(WindowsCredEnum.PersonalAccessToken, TokenField.Value);
		Snackbar.Add("Token saved!", Severity.Success);
		TokenField.SetText(GetDisplayToken());

	}

	void ToggleVisibility()
	{
		if (IsShow)
		{
			IsShow = false;
			PasswordInputIcon = Icons.Material.Filled.VisibilityOff;
			PasswordInput = InputType.Password;
		}
		else
		{
			IsShow = true;
			PasswordInputIcon = Icons.Material.Filled.Visibility;
			PasswordInput = InputType.Text;
		}
		TokenField.SetText(GetDisplayToken());
	}
}