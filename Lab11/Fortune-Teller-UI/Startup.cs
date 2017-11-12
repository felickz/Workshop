﻿
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;


using Fortune_Teller_UI.Services;

// Lab07 Start
using Pivotal.Discovery.Client;
// Lab07 End

// Lab08 Start
using Steeltoe.CloudFoundry.Connector.Redis;
using Steeltoe.Security.DataProtection;
// Lab08 End

// Lab09 Start
using Steeltoe.Security.Authentication.CloudFoundry;
// Lab09 End

// Lab10 Start
using Steeltoe.CircuitBreaker.Hystrix;
// Lab10 End

// Lab11 Start
using Steeltoe.Management.CloudFoundry;
// Lab11 End

namespace Fortune_Teller_UI
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            Configuration = configuration;
            Environment = env;
        }

        public IConfiguration Configuration { get; }
        public IHostingEnvironment Environment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();

            // Lab08 Start
            if (!Environment.IsDevelopment())
            {
                // Use Redis cache on CloudFoundry to DataProtection Keys
                services.AddRedisConnectionMultiplexer(Configuration);
                services.AddDataProtection()
                    .PersistKeysToRedis()
                    .SetApplicationName("fortuneui");
            }
            // Lab08 End

            // Lab05 Start
            services.AddScoped<IFortuneService, FortuneServiceClient>();
            // Lab05 End

            // Lab05 Start
            services.Configure<FortuneServiceConfig>(Configuration.GetSection("fortuneService"));
            // Lab05 End

            // Lab07 Start
            services.AddDiscoveryClient(Configuration);
            // Lab07 End

            // Lab08 Start
            if (Environment.IsDevelopment())
            {
                services.AddDistributedMemoryCache();
            }
            else
            {
                // Use Redis cache on CloudFoundry to store session data
                services.AddDistributedRedisCache(Configuration);
            }
            // Lab08 End

            // Lab09 Start
            services.AddAuthentication((options) =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CloudFoundryDefaults.AuthenticationScheme;

            })
            .AddCookie((options) =>
            {
                options.AccessDeniedPath = new PathString("/Fortunes/AccessDenied");

            })
            .AddCloudFoundryOAuth(Configuration);

            services.AddAuthorization(options =>
            {
                options.AddPolicy("read.fortunes", policy => policy.RequireClaim("scope", "read.fortunes"));

            });
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            // Lab09 End

            // Lab10 Start
            services.AddHystrixCommand<FortuneServiceCommand>("FortuneService", Configuration);
            services.AddHystrixMetricsStream(Configuration);
            // Lab10 End

            services.AddSession();

            // Lab11 Start
            services.AddCloudFoundryActuators(Configuration);
            // Lab11 End

            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Fortunes/Error");
            }

            app.UseStaticFiles();

            // Lab11 Start
            app.UseCloudFoundryActuators();
            // Lab11 End

            // Lab10 Start
            app.UseHystrixRequestContext();
            // Lab10 End

            app.UseSession();

            // Lab09 Start
            app.UseAuthentication();
            // Lab09 End

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Fortunes}/{action=Index}/{id?}");
            });

            // Lab07 Start
            app.UseDiscoveryClient();
            // Lab07 End

            // Lab10 Start
            if (!Environment.IsDevelopment())
            {
                app.UseHystrixMetricsStream();
            }
            // Lab10 End
        }
    }
}
