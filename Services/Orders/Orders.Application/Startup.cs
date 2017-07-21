﻿using System;
using System.Reflection;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using EventBus;
using EventBus.Abstractions;
using FluentValidation;
using FluentValidation.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orders.Application.Commands;
using Orders.Application.ExceptionHandling;
using Orders.Application.IntegrationEvents;
using Orders.Application.IntegrationEvents.Events;
using Orders.Application.Validation;
using Orders.Domain.AggregatesModel.OrderAggregate;
using Orders.Infrastructure;
using Orders.Infrastructure.Repositories;

namespace Orders.Application
{
    public partial class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }
        
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Take connection string from environment varible by default
            var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");

            // Otherwise take from the local configuration (service testing)
            if (string.IsNullOrEmpty(connectionString))
                connectionString = Configuration.GetConnectionString("DefaultConnection");
            
            services.AddDbContext<OrdersContext>(options =>
            {
                options.UseMySql(
                    connectionString,
                    opts =>
                    {
                        opts.MigrationsAssembly(typeof(Startup).GetTypeInfo().Assembly.GetName().Name);
                    });
            });
            
            // Setup MVC 
            services.AddMvc( options =>
            {
                options.Filters.Add(typeof(HttpGlobalExceptionHandlingFilter));
            })
                .AddFluentValidation(); /* Using Fluent validation */

            // Adding services to DI container
            services.AddTransient<IOrderRepository, OrderRepository>();    /* Orders respository */
            services.AddTransient<ISubscriptionsManager, SuscriptionManager>(); /* Subscription manager used by the EventBus */
            services.AddSingleton<IEventBus, EventBusAwsSns.EventBus>(); /* Adding EventBus as a singletone service */
           
            // Amazon services setup
            services.AddDefaultAWSOptions(Configuration.GetAWSOptions()); /* Setup credentails and others options */
            services.AddAWSService<IAmazonSimpleNotificationService>(); /* Amazon SNS */
            services.AddAWSService<IAmazonSQS>(); /* Amazon SQS */
            
            services.AddSingleton<IConfiguration>(Configuration); /* Make project configuration available */
            
            // Register all integration event handlers for this microservice
            RegisterIntegrationEventHandlers(services);
            
            services.AddOptions();

            // MediatR config
            services.AddMediatR(typeof(Startup).GetTypeInfo().Assembly);
            
            // Command validation:
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidatorPipeline<,>)); /* Validation pipline (MediatR) */
            services.AddTransient<IValidator<CreateOrderCommand>, CreateOrderCommandValidator>(); /* Adding fluent validator to DI container */
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();
            
            var options = new RewriteOptions()
                .AddRedirectToHttps();
            app.UseRewriter(options);
            
            ConfigureAuth(app);
            ConfigureEventBus(app);

            app.UseMvc();
        }

        private void ConfigureEventBus(IApplicationBuilder app)
        {
            var eventBus = app.ApplicationServices.GetRequiredService<IEventBus>();

            // TODO: Move to another microservice (here for testing)
            eventBus.Subscribe<OrderStartedIntegrationEvent, OrderStartedIntegrationEventHandlerTest>(() =>
                app.ApplicationServices.GetRequiredService<OrderStartedIntegrationEventHandlerTest>());
        }

        // TODO: Move to another microservice (here for testing)
        private void RegisterIntegrationEventHandlers(IServiceCollection services)
        {
            services.AddTransient<OrderStartedIntegrationEventHandlerTest>();
        }
    }
}