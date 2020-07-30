using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using HeyRed.Mime;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.WebUtilities;
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
        private readonly string _tmpPath;

        public ApiController(ILogger<ApiController> logger)
        {
            _logger = logger;
            var realKey = Environment.GetEnvironmentVariable("APIKEY");

            var storagePath = Environment.GetEnvironmentVariable("STORAGEPATH");
            var url = Environment.GetEnvironmentVariable("URL");
            var tmpPath = Environment.GetEnvironmentVariable("TMPPATH");
            
            if (storagePath == null || url == null)
            {
                throw new Exception("Storage path or URL Env variable not set");
            }

            if (tmpPath == null)
            {
                tmpPath = Path.GetTempPath();
            }

            _tmpPath = tmpPath;
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

            var boundary = Request.GetMultipartBoundary();
            
            var reader = new MultipartReader(boundary, Request.Body);
            var section = await reader.ReadNextSectionAsync();
            
            if (section.GetContentDispositionHeader() != null)
            {
                var fileSection = section.AsFileSection();

                var contentType = section.ContentType;

                // Generate random filename
                string hash;
                using (var md5 = MD5.Create())
                {
                    hash = BitConverter.ToString(md5.ComputeHash(Encoding.ASCII.GetBytes(fileSection.FileName))).Replace("-","").ToLower();
                }
                
                var timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds().ToString();
                var filename = _generateFilename();
                filename += timestamp.Substring(timestamp.Length - 2);
                filename += hash.Substring(0, 2);
                var extension = MimeTypesMap.GetExtension(contentType);
                filename += $".{extension}";

                
                string path = Path.Combine(_storagePath, $"{filename}");
                using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                    fileSection.FileStream.Position = 0;
                    await fileSection.FileStream.CopyToAsync(fileStream);
                }
                
                
                return $"{_url}/{filename}";
            }
            else
            {
                throw new ArgumentException("No Content-Disposition header");
            }

        }

        private Random _random = new Random();
        private string _generateFilename()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, 4)
                .Select(s => s[_random.Next(s.Length)]).ToArray());
        }

        [HttpGet]
        public async Task<ContentResult> Get()
        {
            var r = @$"
                <!DOCTYPE html>
                <html>
                    <title>Wannabe CDN</title>
                    <body>
                        <a href='https://github.com/JMPJNS/file-upload'>Source Code Here</a>
                    </body>
                </html> 
            ";
            
            return new ContentResult
            {
                ContentType = "text/html",
                Content = r
            };
        }

        [HttpGet]
        [Route("/{folder}/{name?}")]
        public async Task<IActionResult> DownloadImage()
        {
            var re = Request;

            string filename;

            var folderValue = re.RouteValues["folder"];
            var filenameValue = re.RouteValues["name"];
            
            if (filenameValue == null)
            {
                if (folderValue != null)
                {
                    filename = folderValue.ToString();
                }
                else
                {
                    throw new ArgumentException("Filename cannot be empty");
                }
            }
            else
            {
                filename = folderValue.ToString() + "/" + filenameValue.ToString();
            }

            if (filename.Contains("..") || filename.Contains("~") || filename.StartsWith("/"))
            {
                throw new ArgumentException("Illegal Filename");
            }
            
            string path = Path.Combine(_storagePath, $"{filename}");
            try
            {
                var content = System.IO.File.Open(path, FileMode.Open);
                var extension = Path.GetExtension(path);
                var mimeType = MimeTypesMap.GetMimeType(extension);
                
                System.Net.Mime.ContentDisposition cd = new System.Net.Mime.ContentDisposition
                {
                    Inline = true
                };
                Response.Headers.Add("Content-Disposition", cd.ToString());
                Response.Headers.Add("X-Content-Type-Options", "nosniff");

                return File(content, mimeType);
            }
            catch (FileNotFoundException e)
            {
                Response.StatusCode = 404;
                throw new FileNotFoundException($"File {filename} not found");
            }
        }
        
    }
}