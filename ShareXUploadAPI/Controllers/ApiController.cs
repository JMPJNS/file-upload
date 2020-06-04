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

        [HttpGet]
        public string Get()
        {
            return "File Upload API";
        }

        [HttpPost]
        public async Task<string> UploadImage()
        {
            var re = Request;

            // Console.WriteLine("Headers:\n");
            // foreach (var header in re.Headers)
            // {
            //     Console.WriteLine($"\t{header.Key}: {header.Value.ToString()}");
            // }
            
            Console.WriteLine("\n\n");
            var apiKey = re.Headers.FirstOrDefault(x => x.Key.ToLower() == "x-api-key").Value;

            var realKey = Environment.GetEnvironmentVariable("APIKEY");

            var storagePath = Environment.GetEnvironmentVariable("STORAGEPATH");
            var url = Environment.GetEnvironmentVariable("URL");

            if (apiKey.ToString() != realKey)
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

            string path = Path.Combine("", $"{filename}");
            
            await System.IO.File.WriteAllBytesAsync(path, content);
            
            return $"{url}/{filename}";
        }
        
    }
}