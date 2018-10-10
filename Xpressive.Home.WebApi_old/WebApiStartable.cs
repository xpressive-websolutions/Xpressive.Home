using System;
using System.IO;
using System.Web.Http;
using Autofac;
using Autofac.Integration.WebApi;
using Microsoft.Owin;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.Hosting;
using Microsoft.Owin.StaticFiles;
using Microsoft.Owin.StaticFiles.ContentTypes;
using Newtonsoft.Json.Serialization;
using Owin;

namespace Xpressive.Home.WebApi
{
    public class WebApiStartable : IStartable, IDisposable
    {
        private readonly IContainer _container;
        private IDisposable _webApp;

        public WebApiStartable(IContainer container)
        {
            _container = container;
        }

        public void Start()
        {
            var root = AppDomain.CurrentDomain.BaseDirectory;
            var webDirectory = Path.Combine(root, "Web");

            _webApp = WebApp.Start("http://+:8080", app =>
            {
                var config = new HttpConfiguration();
                config.MapHttpAttributeRoutes();
                config.DependencyResolver = new AutofacWebApiDependencyResolver(_container);
                config.EnsureInitialized();

                var json = config.Formatters.JsonFormatter;
                json.SerializerSettings.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc;
                json.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();

                var fileServerOptions = new FileServerOptions
                {
                    EnableDefaultFiles = true,
                    EnableDirectoryBrowsing = false,
                    RequestPath = new PathString(""),
                    FileSystem = new PhysicalFileSystem(webDirectory)
                };
                fileServerOptions.StaticFileOptions.ContentTypeProvider = new CustomContentTypeProvider();
                app.UseFileServer(fileServerOptions);

                app.UseAutofacMiddleware(_container);
                app.UseAutofacWebApi(config);
                app.UseWebApi(config);
                app.MapSignalR();
            });
        }

        public void Dispose()
        {
            _webApp.Dispose();
        }
    }

    public class CustomContentTypeProvider : FileExtensionContentTypeProvider
    {
        public CustomContentTypeProvider()
        {
            Mappings.Add(".json", "application/json");
        }
    }
}
