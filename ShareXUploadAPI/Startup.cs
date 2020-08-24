using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ShareXUploadAPI
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<KestrelServerOptions>(options =>
            {
                options.Limits.MaxRequestBodySize = 250000000; // if don't set default value is: 30 MB
            });
            
            services.AddControllers();
            services.AddRazorPages();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // if (env.IsDevelopment())
            // {
            //     app.UseDeveloperExceptionPage();
            // }
            // else
            {
                app.UseExceptionHandler(errorApp =>
                {
                    errorApp.Run(async context =>
                    {
                        var exceptionHandlerPathFeature = 
                            context.Features.Get<IExceptionHandlerPathFeature>();
                        
                        context.Response.StatusCode = 500;
                        context.Response.ContentType = "text/html";

                        if (exceptionHandlerPathFeature?.Error is FileNotFoundException)
                        {
                            context.Response.StatusCode = 404;

                            var sendText = "<html lang='en'>"+
                                            "<head><style>"+
                                            "h1 {text-align: center;}"+
                                            "p {text-align: center;}"+
                                            "div {text-align: center;}"+
                                            "img {text-align: center; display: block; margin-left: auto; margin-right: auto;}"+
                                            "</style></head>"+
                                            "<body>"+
                                            "<img src='https://cdn.jmp.blue/1OYM24bc.png'/>"+
                                            $"<p>{exceptionHandlerPathFeature?.Error.Message}</p>"+
                                            "</body></html>";

                            await context.Response.WriteAsync(sendText);
                        }
                        else
                        {
                            await context.Response.WriteAsync("<html lang=\"en\"><body>\r\n");
                            await context.Response.WriteAsync("ERROR!<br><br>\r\n");
                        
                            await context.Response.WriteAsync(exceptionHandlerPathFeature?.Error.Message);
                        
                            await context.Response.WriteAsync("</body></html>\r\n");
                        }
                        
                        await context.Response.WriteAsync(new string(' ', 512)); // IE padding
                    });
                });
                app.UseHsts();
            }

            // app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
            });
        }
    }
}