using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RoadAware.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileController : ControllerBase
    {
        private readonly ILogger<FileController> _logger;
        private readonly string _openAiEndpoint = "";
        private readonly string _openAiApiKey = "";
        private readonly string _openAiDeployment = "gpt-4o";
        private readonly string _openAiApiVersion = "2024-02-15-preview";
        private readonly string _systemPrompt = @"You are an AI assistant that helps find the depth, width, and location of pothole by analysing the pictures shared.
        Your role is to tell the details and analyse them the best you can so as to help the councils prioritize the most damaging ones and fix them first.

        Tell depth of a pothole on a scale like deep, very deep, shallow, flat etc.
        Also tell width of pothole to say on a scale like very wide, wide, medium, small.
        Also tell the location of pothole in detail whether in middle of main road, side of main road, Rural road, Busy urban roads, highway road, cycle lane, bus lane, next to a bend, etc.
        Tell the type of road it is on like A, B, C type roads in England. Look for signs of roads and other things in image and tell from there. Don't makeup if can't tell from image.
        Tell any other important characteristics you see to help prioritize and fix the pothole.
        IMPORTANT: IF MULTIPLE REPORTED IN ONE PICTURE THEN IGNORE AND SAY MULTIPLE REPORTED and tell in general how big/wide and damaging they look.

        Give short and concise answers in the following format and Id should be a unique number:
        Id
        Name of Pothole - Use the uploaded image name
        Depth
        Wide
        Hazardous - Like too hazardous, damaging, not damaging at all
        Reason for Hazard
        Multiple potholes or one only
        Type of Road

        Use the answers to further decide the priority of fixing each pothole from 1-10 with 1 being highest priority and 10 being lowest priority.

        Use below factors to judge the priority and add the priority field in list of fields above.
        Depth and width of Pothole: The single most important factor is the physical severity of the pothole: its depth, width. Deeper and larger potholes are more likely to cause vehicle damage or accidents, so they are prioritised for faster repair.
        Road Category: the type of road (its official classification or importance in the network) guides prioritisation. An A-road or primary route (key for commerce, buses, emergency services, etc.) generally takes precedence over a local residential street.
        Position and Safety Hazard to Users – Technical. Councils also evaluate where the pothole is located and how it might affect different road users. A hole in the wheel path of traffic or at a busy junction poses a higher immediate risk than one on the road edge. Likewise, a defect in a cycle lane or where motorcycles travel is critical even if small, since two-wheeler users are especially vulnerable.
        Use all these factors into account, use the given images, do analysis and assess the pothole priority.

        Final result should be in a proper HTML table with rows and columns (not markdown, not plain text, not code block). Use <table>, <tr>, <th>, <td> tags. Do not return markdown or code block, only valid HTML table markup.";

        // Placeholder for pothole JSON data (replace with real data source as needed)
        private static string _lastPotholeJson = @"[{""Id"":1,""Depth"":""Deep"",""Wide"":""Wide"",""Hazardous"":""Damaging"",""Reason for Hazard"":""Main road"",""Multiple potholes or one only"":""One only"",""Type of Road"":""A"",""Location"":""Middle of main road""}]";

        public FileController(ILogger<FileController> logger)
        {
            _logger = logger;
        }

        [HttpPost("analyze-potholes")]
        public async Task<IActionResult> AnalyzePotholeImages([FromForm] List<IFormFile> files, [FromForm] string userMessage)
        {
            if ((files == null || files.Count == 0) && string.IsNullOrWhiteSpace(userMessage))
                return BadRequest("No files or message received.");

            try
            {
                //// If user asks to prioritize potholes, use the prioritization prompt and JSON
                //if (!string.IsNullOrWhiteSpace(userMessage) && userMessage.ToLower().Contains("Analysze and priotize potholes"))
                //{
                //    var result = await CallPrioritizePotholesAsync();
                //    return result;
                //}

                // Otherwise, do the normal image analysis
                var userContent = new List<object>();
                if (!string.IsNullOrWhiteSpace(userMessage))
                {
                    userContent.Add(new { type = "text", text = userMessage });
                }
                if (files != null)
                {
                    foreach (var file in files)
                    {
                        using var ms = new MemoryStream();
                        await file.CopyToAsync(ms);
                        var bytes = ms.ToArray();
                        var base64 = Convert.ToBase64String(bytes);
                        var mimeType = file.ContentType;
                        var dataUrl = $"data:{mimeType};base64,{base64}";
                        userContent.Add(new { type = "image_url", image_url = new { url = dataUrl } });
                    }
                }

                var messages2 = new object[]
                {
                    new { role = "system", content = _systemPrompt },
                    new { role = "user", content = userContent }
                };

                var openAiRequest2 = new
                {
                    messages = messages2,
                    max_tokens = 512
                };

                using var httpClient2 = new HttpClient();
                httpClient2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _openAiApiKey);
                httpClient2.DefaultRequestHeaders.Add("api-key", _openAiApiKey);

                var url2 = $"{_openAiEndpoint}/openai/deployments/{_openAiDeployment}/chat/completions?api-version={_openAiApiVersion}";
                var content2 = new StringContent(JsonSerializer.Serialize(openAiRequest2), Encoding.UTF8, "application/json");
                var request = new HttpRequestMessage(HttpMethod.Post, url2)
                {
                    Content = content2
                };
                var response2 = await httpClient2.SendAsync(request, System.Net.Http.HttpCompletionOption.ResponseHeadersRead);

                var responseString2 = await response2.Content.ReadAsStringAsync();

                if (!response2.IsSuccessStatusCode)
                {
                    _logger.LogError($"OpenAI error: {response2.StatusCode} {responseString2}");
                    return StatusCode((int)response2.StatusCode, responseString2);
                }

                // Try to extract JSON from the AI response and store it for prioritization
                string extractedJson = ExtractJsonFromOpenAiResponse(responseString2);
                if (!string.IsNullOrWhiteSpace(extractedJson))
                {
                    _lastPotholeJson = extractedJson;
                }

                return Ok(responseString2);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing pothole images");
                return StatusCode(500, "Error analyzing pothole images");
            }
        }

        // Helper to extract JSON array or object from OpenAI response string
        private static string ExtractJsonFromOpenAiResponse(string responseString)
        {
            try
            {
                // Try to parse as JSON and extract the content
                using var doc = JsonDocument.Parse(responseString);
                var content = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content").GetString();
                if (string.IsNullOrWhiteSpace(content)) return null;
                // Try to find a JSON array or object in the content
                var match = Regex.Match(content, @"(\[.*?\]|\{.*?\})", RegexOptions.Singleline);
                if (match.Success)
                {
                    return match.Value;
                }
            }
            catch { }
            return null;
        }

        [HttpPost("analyze-potholes/stream")]
        public async Task AnalyzePotholeImagesStream([FromForm] List<IFormFile> files, [FromForm] string userMessage)
        {
            Response.ContentType = "text/plain";
            try
            {
                var userContent = new List<object>();
                if (!string.IsNullOrWhiteSpace(userMessage))
                {
                    userContent.Add(new { type = "text", text = userMessage });
                }
                if (files != null)
                {
                    foreach (var file in files)
                    {
                        using var ms = new MemoryStream();
                        await file.CopyToAsync(ms);
                        var bytes = ms.ToArray();
                        var base64 = Convert.ToBase64String(bytes);
                        var mimeType = file.ContentType;
                        var dataUrl = $"data:{mimeType};base64,{base64}";
                        userContent.Add(new { type = "image_url", image_url = new { url = dataUrl } });
                    }
                }

                var messages2 = new object[]
                {
                    new { role = "system", content = _systemPrompt },
                    new { role = "user", content = userContent }
                };

                var openAiRequest2 = new
                {
                    messages = messages2,
                    max_tokens = 5000,
                    stream = true
                };

                using var httpClient2 = new HttpClient();
                httpClient2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _openAiApiKey);
                httpClient2.DefaultRequestHeaders.Add("api-key", _openAiApiKey);

                var url2 = $"{_openAiEndpoint}/openai/deployments/{_openAiDeployment}/chat/completions?api-version={_openAiApiVersion}";
                var content2 = new StringContent(JsonSerializer.Serialize(openAiRequest2), Encoding.UTF8, "application/json");
                var request = new HttpRequestMessage(HttpMethod.Post, url2)
                {
                    Content = content2
                };
                var response2 = await httpClient2.SendAsync(request, System.Net.Http.HttpCompletionOption.ResponseHeadersRead, default);

                using var responseStream = await response2.Content.ReadAsStreamAsync();
                using var writer = new StreamWriter(Response.Body, Encoding.UTF8, leaveOpen: true);
                using var reader = new StreamReader(responseStream);
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (line.StartsWith("data: "))
                    {
                        var data = line.Substring("data: ".Length);
                        if (data == "[DONE]") break;
                        // Parse the streamed chunk and extract the content delta
                        try
                        {
                            var json = JsonDocument.Parse(data);
                            var content = json.RootElement
                                .GetProperty("choices")[0]
                                .GetProperty("delta")
                                .TryGetProperty("content", out var contentProp) ? contentProp.GetString() : null;
                            if (!string.IsNullOrEmpty(content))
                            {
                                await writer.WriteAsync(content);
                                await writer.FlushAsync();
                            }
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error streaming OpenAI response");
                // Optionally write error to response
            }
        }

        // New: Separate method for prioritization
        //private async Task<IActionResult> CallPrioritizePotholesAsync()
        //{
        //    var messages = new object[]
        //    {
        //        new { role = "system", content = _prioritizePrompt },
        //        new { role = "user", content = _lastPotholeJson }
        //    };
        //    var openAiRequest = new
        //    {
        //        messages = messages,
        //        max_tokens = 512
        //    };
        //    using var httpClient = new HttpClient();
        //    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _openAiApiKey);
        //    httpClient.DefaultRequestHeaders.Add("api-key", _openAiApiKey);
        //    var url = $"{_openAiEndpoint}/openai/deployments/{_openAiDeployment}/chat/completions?api-version={_openAiApiVersion}";
        //    var content = new StringContent(JsonSerializer.Serialize(openAiRequest), Encoding.UTF8, "application/json");
        //    var response = await httpClient.PostAsync(url, content);
        //    var responseString = await response.Content.ReadAsStringAsync();
        //    if (!response.IsSuccessStatusCode)
        //    {
        //        _logger.LogError($"OpenAI error: {response.StatusCode} {responseString}");
        //        return StatusCode((int)response.StatusCode, responseString);
        //    }
        //    return Ok(responseString);
        //}
    }
}