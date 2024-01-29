using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.VisualStudio;


using Newtonsoft.Json;

using ProjectDoxen.Manager;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.Azure.Pipelines.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.Client;

namespace ProjectDoxen.Logic;

#nullable disable
public partial class AzureArray<T>
{
  public long Count { get; set; }
  public T[] Value { get; set; }
}

public partial class ApiBuild
{
  public Links Links { get; set; }
  public Uri Url { get; set; }
  public long Id { get; set; }
  public long Revision { get; set; }
  public string Name { get; set; }
  public Folder Folder { get; set; }
}

public partial class Links
{
  public Self Self { get; set; }
  public Self Web { get; set; }
}

public partial class Self
{
  public Uri Href { get; set; }
}

public enum Folder { Automation, AzStatic, CxOne, Empty, Graveflex, Infrastructure, InternalApi, Poc };

public partial class BuildStatus
{
  public BuildStatusLinks Links { get; set; }
  public Properties Properties { get; set; }
  public object[] Tags { get; set; }
  public object[] ValidationResults { get; set; }
  public Plan[] Plans { get; set; }
  public TriggerInfo TriggerInfo { get; set; }
  public long Id { get; set; }
  public string BuildNumber { get; set; }
  public string Status { get; set; }
  public DateTimeOffset QueueTime { get; set; }
  public DateTimeOffset StartTime { get; set; }
  public Uri Url { get; set; }
  public Definition Definition { get; set; }
  public Project Project { get; set; }
  public string Uri { get; set; }
  public string SourceBranch { get; set; }
  public string SourceVersion { get; set; }
  public Queue Queue { get; set; }
  public string Priority { get; set; }
  public string Reason { get; set; }
  public LastChangedBy RequestedFor { get; set; }
  public LastChangedBy RequestedBy { get; set; }
  public DateTimeOffset LastChangedDate { get; set; }
  public LastChangedBy LastChangedBy { get; set; }
  public Plan OrchestrationPlan { get; set; }
  public Logs Logs { get; set; }
  public Repository Repository { get; set; }
  public bool RetainedByRelease { get; set; }
  public object TriggeredByBuild { get; set; }
  public bool AppendCommitMessageToRunName { get; set; }
}

public partial class Definition
{
  public object[] Drafts { get; set; }
  public long Id { get; set; }
  public string Name { get; set; }
  public Uri Url { get; set; }
  public string Uri { get; set; }
  public string Path { get; set; }
  public string Type { get; set; }
  public string QueueStatus { get; set; }
  public long Revision { get; set; }
  public Project Project { get; set; }
}

public partial class Project
{
  public Guid Id { get; set; }
  public string Name { get; set; }
  public string Description { get; set; }
  public Uri Url { get; set; }
  public string State { get; set; }
  public long Revision { get; set; }
  public string Visibility { get; set; }
  public DateTimeOffset LastUpdateTime { get; set; }
}

public partial class LastChangedBy
{
  public string DisplayName { get; set; }
  public Uri Url { get; set; }
  public LastChangedByLinks Links { get; set; }
  public Guid Id { get; set; }
  public string UniqueName { get; set; }
  public Uri ImageUrl { get; set; }
  public string Descriptor { get; set; }
}

public partial class LastChangedByLinks
{
  public Badge Avatar { get; set; }
}

public partial class Badge
{
  public Uri Href { get; set; }
}

public partial class BuildStatusLinks
{
  public Badge Self { get; set; }
  public Badge Web { get; set; }
  public Badge SourceVersionDisplayUri { get; set; }
  public Badge Timeline { get; set; }
  public Badge Badge { get; set; }
}

public partial class Logs
{
  public long Id { get; set; }
  public string Type { get; set; }
  public Uri Url { get; set; }
}

public partial class Plan
{
  public Guid PlanId { get; set; }
}

public partial class Properties
{
}

public partial class Queue
{
  public long Id { get; set; }
  public string Name { get; set; }
  public Pool Pool { get; set; }
}

public partial class Pool
{
  public long Id { get; set; }
  public string Name { get; set; }
  public bool IsHosted { get; set; }
}

public partial class Repository
{
  public string Id { get; set; }
  public string Type { get; set; }
  public object Clean { get; set; }
  public bool CheckoutSubmodules { get; set; }
}

public partial class TriggerInfo
{
  public string CiSourceBranch { get; set; }
  public string CiSourceSha { get; set; }
  public string CiMessage { get; set; }
  public string CiTriggerRepository { get; set; }
}




public class AzureService
{
  private readonly HttpClient _httpClient;
  private readonly string _projectName;
  private readonly string _token;
  private readonly string _organizationUrl;


  public AzureService(HttpClient client, SimpleCredentialManager credentials)
  {
    this._httpClient = client;
    this._projectName = credentials.GetCredential(WindowsCredEnum.ProjectName)?.CredentialBlob ?? throw new ArgumentNullException(nameof(_projectName));
    this._token = credentials.GetCredential(WindowsCredEnum.PersonalAccessToken)?.CredentialBlob ?? throw new ArgumentNullException(nameof(_token));
    this._organizationUrl = credentials.GetCredential(WindowsCredEnum.OrganizationUrl)?.CredentialBlob ?? throw new ArgumentNullException(nameof(_organizationUrl));
    var basicToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{_token}"));
    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basicToken);

    // Create a connection with PAT for authentication
    VssConnection connection = new VssConnection(new Uri(_organizationUrl), new VssClientCredentials()
    {
    });


    // Create instance of WorkItemTrackingHttpClient using VssConnection
    PipelinesHttpClient pipelinesClient = new PipelinesHttpClient(new(_organizationUrl), connection.Credentials);
    var pipelines = pipelinesClient.ListPipelinesAsync(_projectName).Result;
  }

  public async Task<AzureArray<ApiBuild>> GetApiBuildsAsync()
  {
    var url = $"{_organizationUrl}/{_projectName}/_apis/build/builds?api-version=6.0";

    var request = new HttpRequestMessage(HttpMethod.Get, url);
    var response = await _httpClient.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    var enumerableResult = JsonConvert.DeserializeObject<AzureArray<ApiBuild>>(result);

    return enumerableResult;
  }

  public async Task<BuildStatus> GetBuildStatusAsync(string buildUrl)
  {
    var response = await _httpClient.GetAsync(buildUrl);
    var result = await response.Content.ReadAsStringAsync();
    var buildStatus = JsonConvert.DeserializeObject<BuildStatus>(result);

    return buildStatus;
  }

  public async Task<string> GetRateLimits()
  {
    throw new NotImplementedException();
  }

}
