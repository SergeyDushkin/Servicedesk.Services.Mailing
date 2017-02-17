using Microsoft.AspNetCore.Hosting;
using servicedesk.Common.Host;
using servicedesk.Services.Tickets.Shared.Events;
using System.IO;

namespace servicedesk.Services.Mailing
{
    public class Program
    {
        public static void Main(string[] args)
        {
            /*
            var host = new WebHostBuilder()
                        .UseKestrel()
                        .UseContentRoot(Directory.GetCurrentDirectory())
                        .UseStartup<Startup>()
                        .Build();

            host.Run();
            */
            
            WebServiceHost
                .Create<Startup>(port: 10030)
                .UseAutofac(Startup.LifetimeScope)
                .UseRabbitMq()
                .SubscribeToEvent<TicketCreated>(exchangeName: "servicedesk.Services.Tickets", routingKey: "ticket.created")
                .Build()
                .Run();
        }
    }
}
