using System.Text.Json;
var capture=Environment.GetEnvironmentVariable("EMUVORA_LAUNCH_CAPTURE");
if(capture!=null)File.WriteAllText(capture,JsonSerializer.Serialize(args));
await Task.Delay(100);
