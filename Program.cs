using CliWrap;
using CliWrap.Buffered;

using Newtonsoft;
string _path = "E:/";
Command Wrap() => Cli.Wrap(_path);
CommandTask<BufferedCommandResult> ExecAsync(params string[] args)
{
  return Cli.Wrap(_path).WithArguments(args).ExecuteBufferedAsync();
}
var result = await Wrap().WithArguments("az account show").ExecuteBufferedAsync();
if (result?.StandardError.Contains("not recognized") is null or true)
{
  Console.Error.WriteLine("az cli not found...downloading");
  var link = "https://aka.ms/installazurecliwindowsx64";
  System.Diagnostics.Process.Start(link);
  Console.Error.WriteLine("Please install the az cli to continue");
  return;
}

if (result?.StandardError.Contains("Please run 'az login' to setup account.") is true)
{
  await Wrap().WithArguments("az login").ExecuteBufferedAsync();
}

await ExecAsync("az extension add --upgrade -n azure-devops");

var configuration = await ExecAsync("az devops configure -l");
if (configuration?.StandardOutput?.Contains("organization") is true)
{
  Console.WriteLine("Azure DevOps Organization already configured");
}
else
{
  Console.WriteLine("Please enter your Azure DevOps Organization");
  var org = Console.ReadLine();
  if (org != null)
  {
    await ExecAsync($"az devops configure --defaults organization={org}");
  }
  else
  {
    Console.Error.WriteLine("Organization is required");
    return;
  }
}

Console.WriteLine("Please enter a story number");
var storyNumber = Console.ReadLine();

// find all the git repos in the current directory
var repos = Directory.GetDirectories(_path, ".git", SearchOption.AllDirectories);
// check through git if the branch exists locally
var branchExists = repos
  .Select(x => x.Replace(".git", ""))
  .Select(x => Cli.Wrap("git")
    .WithArguments($"branch --list {storyNumber}")
    .WithWorkingDirectory(x)
    .ExecuteBufferedAsync())
  .ToList();

var results = await Task.WhenAll(branchExists.Select(x => x.Task));
