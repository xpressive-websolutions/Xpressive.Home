using System;
using System.Reflection;
using System.Web.Http;
using Autofac;
using Autofac.Integration.WebApi;
using Microsoft.Owin.Hosting;
using Owin;
using Module = Autofac.Module;

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
            _webApp = WebApp.Start("http://localhost:8080", app =>
            {
                var config = new HttpConfiguration();
                config.MapHttpAttributeRoutes();
                config.DependencyResolver = new AutofacWebApiDependencyResolver(_container);

                app.UseAutofacMiddleware(_container);
                app.UseAutofacWebApi(config);
                app.UseWebApi(config);
            });
        }

        public void Dispose()
        {
            _webApp.Dispose();
        }
    }

    public class WebApiModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<WebApiStartable>().As<IStartable>().SingleInstance();
            var executingAssembly = Assembly.GetExecutingAssembly();
            builder.RegisterApiControllers(executingAssembly);

            base.Load(builder);
        }
    }

    [RoutePrefix("api/v1/gateway")]
    public class GatewayController : ApiController
    {
        public GatewayController()
        {
            Console.WriteLine("Created GatewayController");
        }

        [HttpPost, Route("test")]
        public void Test()
        {
            Console.WriteLine("Test");
        }

        [HttpGet, Route("test")]
        public IHttpActionResult X()
        {
            return Ok("Hello World!");
        }
    }
}
