using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Serilog;
using Polly;
using servicedesk.Services.Mailing.Extensions;
using servicedesk.Common.Services;
using servicedesk.Common.Events;
using servicedesk.Common.Commands;
using RawRabbit.Configuration;
using RawRabbit.vNext;
using RawRabbit;
using RabbitMQ.Client.Exceptions;
using RawRabbit.Common;
using RawRabbit.Attributes;
using RawRabbit.vNext.Logging;

namespace servicedesk.Services.Mailing
{
    public class Startup
    {
        public string EnvironmentName { get; set; }
        public IConfiguration Configuration { get; set; }
        public static ILifetimeScope LifetimeScope { get; private set; }

        public Startup(IHostingEnvironment env)
        {
            EnvironmentName = env.EnvironmentName.ToLowerInvariant();
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables()
                .SetBasePath(env.ContentRootPath);

            Configuration = builder.Build();
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            var rmqRetryPolicy = Policy
                .Handle<ConnectFailureException>()
                .Or<BrokerUnreachableException>()
                .Or<IOException>()
                .WaitAndRetry(5, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, retryCount, context) => {
                        //logger.LogError(new EventId(10001, "RabbitMQ Connect Error"), exception, $"Cannot connect to RabbitMQ. retryCount:{retryCount}, duration:{timeSpan}");
                    }
                );

            var builder = new ContainerBuilder();

            builder.Populate(services);

            var rawRabbitConfiguration = Configuration.GetSettings<RawRabbitConfiguration>();
            builder.RegisterInstance(rawRabbitConfiguration).SingleInstance();
            rmqRetryPolicy.Execute(() => builder
                    .RegisterInstance(BusClientFactory.CreateDefault(rawRabbitConfiguration))
                    .As<IBusClient>()
            );

            var assembly = typeof(Startup).GetTypeInfo().Assembly;
            builder.RegisterAssemblyTypes(assembly).AsClosedTypesOf(typeof(IEventHandler<>));
            builder.RegisterAssemblyTypes(assembly).AsClosedTypesOf(typeof(ICommandHandler<>));
            builder.RegisterType<Handler>().As<IHandler>();

            LifetimeScope = builder.Build().BeginLifetimeScope();

            return new AutofacServiceProvider(LifetimeScope);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IApplicationLifetime appLifeTime)
        {
            var serilogLogger = new LoggerConfiguration()
                .Enrich.WithProperty("Application", "ServiceDesk.Services.Mailing")
                .ReadFrom.Configuration(Configuration)
                .CreateLogger();

            loggerFactory.AddSerilog(serilogLogger);
            loggerFactory.AddConsole();

            //appLifeTime.ApplicationStopped.Register(() => LifetimeScope.Dispose());
        }
    }
}
