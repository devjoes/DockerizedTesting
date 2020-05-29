using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace ExampleProjectNs
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.Configure(a =>
                    {
                        a.UseRouting();
                        a.UseEndpoints(r => r.MapGet("/", new RequestDelegate(
                            c => c.Response.WriteAsync("Hello world!"))));
                    });
                }).Build().Run();
        }
    }
}
