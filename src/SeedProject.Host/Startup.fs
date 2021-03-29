namespace SeedProject.Host

open System
open System.Threading.Tasks

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.EntityFrameworkCore
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting

open SeedProject.Persistence.Model
open SeedProject.Infrastructure.Common
open SeedProject.Infrastructure

type Startup(configuration: IConfiguration) =
    let runHandler handler =
        fun (c: HttpContext) ->
            async {
                let lf = c |> Context.loggerFactory
                let! result = handler c lf
                match result with
                | OperationResult.Success _ -> ()
                | OperationResult.ValidationError (code, ValidationMessage message) ->
                    do! (Context.ResponseBody.badRequest c {| Code = code.ToString(); Message = message |} |> Async.Ignore)
                | OperationResult.OperationError (code, OperationMessage message) ->
                    do! (Context.ResponseBody.badRequest c {| Code = code.ToString(); Message = message |} |> Async.Ignore)
            } |> Async.StartAsTask :> Task

    member _.Configuration = configuration

    // This method gets called by the runtime. Use this method to add services to the container.
    member this.ConfigureServices(services: IServiceCollection) =
        services.AddAuthorization() |> ignore

        services.AddMvcCore().AddJsonOptions(fun o -> o.JsonSerializerOptions.Converters.Add(Context.optionConverter)) |> ignore

        services.AddDbContext<DatabaseContext>
            (fun options ->
                options.UseSqlServer(this.Configuration.GetConnectionString("DefaultConnection"))
                |> ignore)
        |> ignore
        ()

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    member _.Configure(app: IApplicationBuilder, env: IWebHostEnvironment) =
        if (env.IsDevelopment()) then
            app.UseDeveloperExceptionPage() |> ignore

        app.Use(Func<_, _>(Middleware.exceptionHandler))
        |> ignore

        app
            .UseHttpsRedirection()
            .UseRouting()
            .UseAuthorization()
            .UseEndpoints(fun endpoints ->
                Routing.routes
                |> List.iter
                    (fun (pattern, methods, handler) ->
                        endpoints.MapMethods(pattern, methods, RequestDelegate(runHandler handler)) |> ignore))
        |> ignore
