using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using MimeKit;
using servicedesk.Common.Events;
using servicedesk.Common.Services;
using servicedesk.Services.Tickets.Shared.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace servicedesk.Services.Mailing.Handlers
{
    public class TicketCreatedHandler : IEventHandler<TicketCreated>
    {
        private readonly IHandler handler;
        private readonly ILogger logger;

        public TicketCreatedHandler(IHandler handler, ILogger<TicketCreatedHandler> logger)
        {
            this.handler = handler;
            this.logger = logger;
        }

        public async Task HandleAsync(TicketCreated @event)
        {
            logger.LogDebug($"Call TicketCreatedHandler: ${Newtonsoft.Json.JsonConvert.SerializeObject(@event)}");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("help@atomigy.com", "help@atomigy.com"));
            message.To.Add(new MailboxAddress("leirbythe@gmail.com", "leirbythe@gmail.com"));
            message.Subject = "How you doin'?";

            message.Body = new TextPart("plain")
            {
                Text = @"TEST Тест"
            };

            using (var client = new SmtpClient(new MailKit.ProtocolLogger("smtp.log")))
            {
                // For demo-purposes, accept all SSL certificates (in case the server supports STARTTLS)
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                // Note: since we don't have an OAuth2 token, disable
                // the XOAUTH2 authentication mechanism.
                client.AuthenticationMechanisms.Remove("XOAUTH2");

                await client.ConnectAsync("atomigy.com", 587, MailKit.Security.SecureSocketOptions.None); //22
                await client.AuthenticateAsync("help@atomigy.com", "!12345Aa");
                await client.SendAsync(message);

                await client.DisconnectAsync(true);
            }
        }
    }
}
