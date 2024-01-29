global using ProjectDoxen.Models;

namespace ProjectDoxen.Models;


public enum WindowsCredEnum
{
	PersonalAccessToken,
	OrganizationUrl,
	ProjectName
}

public static class WindowsCredEnumExtensions
{
	public static string AsString(this WindowsCredEnum windowsCredEnum)
	{
		return windowsCredEnum switch
		{
			WindowsCredEnum.PersonalAccessToken => "PersonalAccessToken",
			WindowsCredEnum.OrganizationUrl => "OrganizationUrl",
			WindowsCredEnum.ProjectName => "ProjectName",
			_ => throw new ArgumentOutOfRangeException(nameof(windowsCredEnum), windowsCredEnum, null)
		};
	}
}
