
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Load Ocelot configuration
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// Add Ocelot services
builder.Services.AddOcelot();

//  Add Swagger configuration for API Gateway
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "API Gateway",
        Version = "v1",
        Description = "API Gateway using Ocelot"
    });

    //  Add Downstream Services' Swagger endpoints
    c.AddServer(new OpenApiServer { Url = "http://localhost:5280", Description = "TaskService" });
    c.AddServer(new OpenApiServer { Url = "http://localhost:5058", Description = "AuthService" });
});

var app = builder.Build();

// Enable Swagger Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Gateway V1");

        //  Add Swagger Endpoints for Downstream Services
        c.SwaggerEndpoint("http://localhost:5280/swagger/v1/swagger.json", "TaskService API");
        c.SwaggerEndpoint("http://localhost:5058/swagger/v1/swagger.json", "AuthService API");
    });
}

app.UseRouting();
app.UseOcelot().Wait(); // Apply Ocelot middleware

app.Run();



