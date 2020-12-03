using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AdvertDetector.Controllers
{
    [ApiController]
    [Route("api")]
    public class ApiController : ControllerBase
    {
        private readonly ILogger<ApiController> _logger;
        private readonly Detector detector;

        public ApiController(ILogger<ApiController> logger, Detector detector)
        {
            _logger = logger;
            this.detector = detector;
        }

        [HttpPost("checkSentence")]
        public async Task<IActionResult> CheckSentence()
        {
            try
            {
                using (StreamReader stream = new StreamReader(HttpContext.Request.Body))
                {
                    string body = await stream.ReadToEndAsync();
                    var prediction = detector.Predict(body);
                    if (body.Length != 0)
                    {
                        var remoteIpAddress = HttpContext.Connection.RemoteIpAddress;

                        if (HttpContext.Request.Headers.TryGetValue("User-Agent", out StringValues value))
                        {
                            _logger.Log(LogLevel.Information,$"{remoteIpAddress}:{value.ToString()} | {prediction.Probability}: {body}");
                        }
                        else
                        {
                            _logger.Log(LogLevel.Information,$"{remoteIpAddress} | {prediction.Probability}: {body}");
                        }
                    }
                    return Ok(prediction);
                }
            }
            catch(Exception ex)
            {
                return NoContent();
            }
        }
    }
}
