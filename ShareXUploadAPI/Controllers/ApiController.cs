using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ShareXUploadAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ApiController : ControllerBase
    {
        private readonly ILogger<ApiController> _logger;

        public ApiController(ILogger<ApiController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public async Task<string> UploadImage()
        {
            var re = Request;

            var apiKey = re.Headers.FirstOrDefault(x => x.Key == "x-api-key").Value;

            var realKey = Environment.GetEnvironmentVariable("APIKEY");

            if (apiKey != realKey)
            {
                return "Invalid or no API Key provided (x-api-key header)";
            }

            if (re.Form.Files.Count == 0)
            {
                return "No File in Form Body";
            } 
            
            IFormFile file = re.Form.Files.First();

            byte[] content;

            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                content = ms.ToArray();
            }

            var filename = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds().ToString();

            string path = Path.Combine("/drive/jonas/file/sx", $"{filename}.png");
            
            await System.IO.File.WriteAllBytesAsync(path, content);
            
            return $"https://files.jmp.blue/sx/{filename}.png";
        }
        
    }
}