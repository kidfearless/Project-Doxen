using CliWrap;
using CliWrap.Buffered;

using Newtonsoft.Json;

public static class WrapExtensions
{
  public static async Task<T?> ExecuteJsonAsync<T>(this Command command)
  {
    var result = await command.ExecuteBufferedAsync();
    var output = result.StandardOutput;
    var returnValue = JsonConvert.DeserializeObject<T>(output);
    return returnValue;
  }
}



