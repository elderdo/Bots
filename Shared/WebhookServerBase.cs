﻿using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bots
{
    public abstract class WebhookServerBase
    {
        private string pfxPath, pfxPassword;

        public WebhookServerBase(int port, LogLevel logLevel) : this(port, null, null, logLevel)
        {
        }

        public WebhookServerBase(int port, string pfxPath, string pfxPassword, LogLevel logLevel)
        {
            LogLevel = logLevel;
            Port = port;
            LoggerFactory factory = new LoggerFactory();
            factory.AddConsole().AddDebug();
            Logger = factory.CreateLogger(this.GetType().ToString() + $"[WITHOUT_WEBHOOK]");
            this.pfxPath = pfxPath;
            this.pfxPassword = pfxPassword;
            CreateWebhookHost();
        }

        public abstract string WebhookPath { get; }

        public virtual string StatusPath => "status";

        public ILogger Logger { get; protected set; }

        public int Port { get; protected set; }

        protected IWebHost Host { get; set; }

        protected LogLevel LogLevel { get; set; }

        protected int EventId { get; set; } = 1;


        public virtual async void StartReceivingAsync()
        {
            await Task.Run(() =>
            {
                #region Unused
                //var builder = WebHost.CreateDefaultBuilder();

                //builder.ConfigureServices(cfg =>
                //{
                //    cfg.AddRouting();
                //});

                //builder.ConfigureLogging(cfg =>
                //{
                //    //cfg.ClearProviders();
                //    //cfg.AddConsole();
                //    //cfg.AddDebug();
                //});

                //builder.UseKestrel(options =>
                //{
                //    options.Listen(IPAddress.Any, Port);
                //});

                //builder.Configure(cfg =>
                //{
                //    cfg.UseRouter(r =>
                //    {
                //        r.MapGet(WebhookTestPath, Test);
                //        r.MapGet(WebhookPath, Get);
                //        r.MapPost(WebhookPath, Post);
                //    });
                //});

                //host = builder.Build();
                //logger = host.Services.GetService<ILoggerFactory>().CreateLogger(this.GetType().ToString() + $"[{IPAddress.Any}:{Port}]");
                #endregion

                Host.Run();
            });
        }

        public void WaitForShutdown()
        {
            while (Host == null)
            {
                Thread.Sleep(100);
            }

            Host.WaitForShutdown();
        }


        public delegate void WebhookHandler(WebhookEventArgs e);

        public event WebhookHandler GetReceived;

        public event WebhookHandler PostReceived;
        

        private void CreateWebhookHost()
        {
            var builder = WebHost.CreateDefaultBuilder();

            builder.ConfigureServices(cfg =>
            {
                cfg.AddRouting();
            });

            builder.ConfigureLogging(cfg =>
            {
                cfg.SetMinimumLevel(LogLevel);
                //cfg.ClearProviders();
                //cfg.AddConsole();
                //cfg.AddDebug();
            });

            builder.UseKestrel(options =>
            {
                if (pfxPath == null) options.Listen(IPAddress.Any, Port);
                else options.Listen(IPAddress.Any, Port, op=>
                {
                    op.UseHttps(pfxPath, pfxPassword);
                });
            });

            builder.Configure(cfg =>
            {
                cfg.UseRouter(r =>
                {
                    r.MapGet(StatusPath, Status);
                    r.MapGet(WebhookPath, Get);
                    r.MapPost(WebhookPath, Post);
                });
            });

            Host = builder.Build();
            Logger = Host.Services.GetService<ILoggerFactory>().CreateLogger(this.GetType().ToString() + $"[{IPAddress.Any}:{Port}]");
        }

        private async Task Status(HttpRequest request, HttpResponse response, RouteData route)
        {
            try
            {
                response.ContentType = ContentTypes.TextHtml;
                await response.WriteAsync($"<p>Webhook status: OK</p>IP: {request.HttpContext.Connection.RemoteIpAddress}");
            }
            catch (Exception e)
            {
                Logger.LogError(e, e.Message);
            }
        }

        private async Task Get(HttpRequest request, HttpResponse response, RouteData route)
        {
            var eventId = new EventId(EventId++);

            try
            {
                if (GetReceived != null)
                {
                    await Task.Run(() =>
                    {
                        GetReceived.Invoke(new WebhookEventArgs()
                        {
                            Request = request,
                            Response = response
                        });
                    });
                }
            }
            catch (Exception e)
            {
                Logger.LogError(eventId, e, e.Message);
            }
        }

        private async Task Post(HttpRequest request, HttpResponse response, RouteData route)
        {
            var eventId = new EventId(EventId++);
            
            try
            {
                byte[] bodyRaw = new byte[request.ContentLength.Value];
                await request.Body.ReadAsync(bodyRaw, 0, bodyRaw.Length);

                string body = Encoding.UTF8.GetString(bodyRaw);

                if (PostReceived != null)
                {
                    await Task.Run(() =>
                    {
                        PostReceived.Invoke(new WebhookEventArgs()
                        {
                            Request = request,
                            BodyRaw = bodyRaw,
                            Body = body,
                            Response = response
                        });
                    });
                }
            }
            catch (Exception e)
            {
                Logger.LogError(eventId, e, e.Message);
            }
        }
    }
}
