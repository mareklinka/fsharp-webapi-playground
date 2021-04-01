namespace SeedProject.Host

open System.Text.Json
open System.Text.Json.Serialization

open Giraffe

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.EntityFrameworkCore
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Identity.Web

open SeedProject.Persistence.Model
open Giraffe.EndpointRouting
open Microsoft.AspNetCore.Authentication.JwtBearer

type Startup(configuration: IConfiguration) =
    let corsPolicyName = "CorsPolicy"
    let authenticationPolicyName = "ClientAuthPolicy"

    let configureCommonServices (services: IServiceCollection) =
        services.AddAuthorization() |> ignore
        services.AddGiraffe() |> ignore

        let jsonOptions = JsonSerializerOptions()
        jsonOptions.Converters.Add(JsonFSharpConverter())
        services.AddSingleton(jsonOptions) |> ignore
        services.AddHealthChecks() |> ignore

        services.AddSingleton<Json.ISerializer, SystemTextJson.Serializer>() |> ignore

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(configuration.GetSection("AzureAd")) |> ignore

        services.AddDbContext<DatabaseContext>
            (fun options ->
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
                |> ignore)
        |> ignore
        ()

    member _.Configuration = configuration

    // This method gets called by the runtime. Use this method to add services to the container.
    member __.ConfigureDevelopmentServices(services: IServiceCollection) =
        services |> configureCommonServices
        services.AddCors(
            fun options ->
                options.AddPolicy(
                    corsPolicyName,
                        (fun builder ->
                                builder
                                    .AllowAnyOrigin()
                                    .AllowAnyMethod()
                                    .WithHeaders("authorization", "content-type")
                                    |> ignore
                                ()
                            )
                )
        ) |> ignore
        ()

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    member _.Configure(app: IApplicationBuilder, env: IWebHostEnvironment) =
        if (env.IsDevelopment()) then
            app.UseDeveloperExceptionPage() |> ignore

        app
            .UseHttpsRedirection()
            .UseRouting()
            .UseAuthentication()
            .UseAuthorization()
            .UseCors(corsPolicyName)
            .UseEndpoints(fun endpoints -> endpoints.MapHealthChecks("/health") |> ignore)
            .UseGiraffeErrorHandler(Middleware.errorHandler)
            .UseGiraffe(Routing.routes)
        |> ignore
