using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using HeyRed.Mime;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ShareXUploadAPI.Controllers
{
    [ApiController]
    [Route("")]
    public class ApiController : ControllerBase
    {
        private readonly ILogger<ApiController> _logger;
        private readonly string _realKey;
        private readonly string _storagePath;
        private readonly string _url;

        public ApiController(ILogger<ApiController> logger)
        {
            _logger = logger;
            var realKey = Environment.GetEnvironmentVariable("APIKEY");

            var storagePath = Environment.GetEnvironmentVariable("STORAGEPATH");
            var url = Environment.GetEnvironmentVariable("URL");
            
            if (storagePath == null || url == null)
            {
                throw new Exception("Storage path or URL Env variable not set");
            }

            _realKey = realKey;
            _url = url;
            _storagePath = storagePath;
        }

        [HttpPost]
        public async Task<string> UploadImage()
        {
            var re = Request;
            var apiKey = re.Headers.FirstOrDefault(x => x.Key.ToLower() == "x-api-key").Value;

            if (apiKey.ToString() != _realKey)
            {
                throw new ArgumentException("Invalid or no API Key provided (x-api-key header)");
            }

            if (re.Form.Files.Count == 0)
            {
                throw new ArgumentException( "No File in Form Body");
            } 
            
            IFormFile file = re.Form.Files.First();

            byte[] content;
            string hash;
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                content = ms.ToArray();
            }

            using (var md5 = MD5.Create())
            {
                hash = BitConverter.ToString(md5.ComputeHash(content)).Replace("-","").ToLower();
            }
            
            var timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds().ToString();

            string filename = _generateFilename();

            filename += timestamp.Substring(timestamp.Length - 2);
            filename += hash.Substring(0, 2);
            
            var extension = MimeTypesMap.GetExtension(file.ContentType);
            filename += $".{extension}";

            string path = Path.Combine(_storagePath, $"{filename}");
            
            await System.IO.File.WriteAllBytesAsync(path, content);
            
            return $"{_url}/{filename}";
        }

        private Random _random = new Random();
        private string _generateFilename()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, 4)
                .Select(s => s[_random.Next(s.Length)]).ToArray());
        }

        [HttpGet]
        [Route("/{name}")]
        public async Task<byte[]> DownloadImage()
        {
            var re = Request;
            var filename = re.RouteValues["name"];
            if (filename == null)
            {
                throw new ArgumentException("Filename cannot be empty");
            }
            string path = Path.Combine(_storagePath, $"{filename}");
            try
            {
                var content = await System.IO.File.ReadAllBytesAsync(path);

                if (content == null)
                {
                    throw new IOException("File not Found");
                }

                return content;
            }
            catch (FileNotFoundException e)
            {
                throw new FileNotFoundException($"File {filename} not found");
            }
        }
        
    }
}