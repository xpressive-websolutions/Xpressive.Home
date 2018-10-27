using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz.Spi;
using Serilog;
using Serilog.Events;
using Xpressive.Home.Contracts;
using Xpressive.Home.Contracts.Automation;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;
using Xpressive.Home.Contracts.Rooms;
using Xpressive.Home.Contracts.Services;
using Xpressive.Home.Contracts.Variables;
using Xpressive.Home.DatabaseModel;
using Xpressive.Home.Services;
using Xpressive.Home.Services.Automation;
using Xpressive.Home.Services.Messaging;
using Xpressive.Home.Services.Variables;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace Xpressive.Home
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<XpressiveHomeContext>(o => o.UseSqlite("Data Source=XpressiveHome.db"));

            LoadPlugins(services);

            services.AddSingleton<IMessageQueue, MessageQueue>();
            services.AddSingleton<IVariablePersistingService, VariablePersistingService>();
            services.AddTransient<IJobFactory, RecurrentScriptJobFactory>();
            services.AddTransient<IScheduledScriptRepository, ScheduledScriptRepository>();
            services.AddTransient<IScriptRepository, ScriptRepository>();
            services.AddTransient<IScriptObjectProvider, VariableScriptObjectProvider>();
            services.AddTransient<IScriptObjectProvider, DefaultScriptObjectProvider>();
            services.AddTransient<IScriptObjectProvider, SchedulerScriptObjectProvider>();
            services.AddTransient<IScriptTriggerService, ScriptTriggerService>();
            services.AddSingleton<IScriptEngine, ScriptEngine>();
            services.AddHostedService<MessageQueueScriptTriggerListener>();
            services.AddHostedService<MessageQueueLogListener>();
            services.AddHostedService<RenameDeviceListener>();
            services.AddSingleton<VariableRepository>();
            services.AddSingleton<IVariableRepository>(s => s.GetService<VariableRepository>());
            services.AddSingleton<IHostedService>(s => s.GetService<VariableRepository>());
            services.AddSingleton<IVariableHistoryService, VariableHistoryService>();
            services.AddSingleton<CronService>();
            services.AddSingleton<IHostedService>(s => s.GetService<CronService>());
            services.AddSingleton<ICronService>(s => s.GetService<CronService>());
            services.AddHostedService<LowBatteryDeviceObserver>();
            services.AddTransient<IIpAddressService, IpAddressService>();
            services.AddTransient<ITuneInRadioStationService, TuneInRadioStationService>();
            services.AddTransient<IDevicePersistingService, DevicePersistingService>();
            services.AddTransient<IRoomRepository, RoomRepository>();
            services.AddTransient<IRoomScriptGroupRepository, RoomScriptGroupRepository>();
            services.AddTransient<IRoomScriptRepository, RoomScriptRepository>();
            services.AddTransient<IFavoriteRadioStationService, FavoriteRadioStationService>();
            services.AddTransient<IRoomDeviceService, RoomDeviceService>();
            services.AddTransient<IDeviceConfigurationBackupService, DeviceConfigurationBackupService>();
            services.AddTransient<IWebHookService, WebHookService>();
            services.AddTransient<IBase62Converter, Base62Converter>();
            services.AddSingleton<IHttpClientProvider, HttpClientProvider>();
            services.AddSingleton<SoftwareUpdateDownloadService>();
            services.AddSingleton<ISoftwareUpdateDownloadService>(s => s.GetService<SoftwareUpdateDownloadService>());
            services.AddSingleton<IHostedService>(s => s.GetService<SoftwareUpdateDownloadService>());
            services.AddHostedService<UpnpDeviceDiscoveringService>();
            services.AddSingleton<IContextFactory, ContextFactory>();

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .WriteTo.Console()
                .CreateLogger();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                //app.UseHsts();
            }

            // Run DB Migrations
            using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetService<XpressiveHomeContext>())
                {
                    context.Database.Migrate();
                }
            }

            // Register Queue Listeners

            // Start Gateways

            // Start NetworkScanners

            //app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseMvc();
        }

        private void LoadPlugins(IServiceCollection services)
        {
            var directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "Publish", "Plugins");
            var plugins = Directory.GetFiles(directory, "Xpressive.Home.Plugins.*.dll", SearchOption.AllDirectories);

            foreach (var plugin in plugins)
            {
                var pluginFileName = Path.GetFileName(plugin);

                if (string.IsNullOrEmpty(pluginFileName))
                {
                    continue;
                }

                var assembly = Assembly.LoadFrom(plugin);
                var pluginTypes = assembly.GetTypes().Where(t => typeof(IPlugin).IsAssignableFrom(t)).ToList();

                foreach (var pluginType in pluginTypes)
                {
                    var pluginInstance = (IPlugin)Activator.CreateInstance(pluginType);
                    pluginInstance.ConfigureServices(services);
                }
            }
        }
    }
}
