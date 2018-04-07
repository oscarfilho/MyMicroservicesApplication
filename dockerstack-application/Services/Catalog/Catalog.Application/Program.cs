﻿using System;
using System.IO;
using System.Net;
using Catalog.Application.Infrastructure;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Catalog.Application
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).MigrateDbContext<CatalogContext>((context, services) =>
            {
                var loggerFactory = services.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("BuildWebHost");

                try
                {
                    new CatalogContextSeed().SeedAsync(context, loggerFactory).Wait();
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "An error occurred seeding the DB.");
                }
            }).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
            .UseKestrel(options =>
            {
                options.Listen(IPAddress.Any, 80);
            })
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseIISIntegration()
            .UseStartup<Startup>()
            .Build();
    }
}