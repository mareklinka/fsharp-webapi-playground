namespace SeedProject.Functional.TestHost

open System.Net.Http
open System.Threading.Tasks
open System.Threading

open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.TestHost
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Configuration
open Microsoft.EntityFrameworkCore

open FSharp.Control.Tasks

open SeedProject.Host
open SeedProject.Persistence.Model
open SeedProject.Persistence
open System.Text.Json
open System.Text.Json.Serialization

module TestServer =
    let private testHostBuilder =
        Host
            .CreateDefaultBuilder()
            .ConfigureAppConfiguration(fun i -> i.AddUserSecrets<Startup>() |> ignore)
            .ConfigureWebHostDefaults(fun builder ->
                builder.UseTestServer().UseStartup<Startup>()
                |> ignore)
            .UseEnvironment("FunctionalTests")

    let start () =
        task {
            let! host = testHostBuilder.StartAsync()
            return (host, host.GetTestClient())
        }

    let stop (host: IHost) = unitTask { do! host.StopAsync() }

    let migrateDb (host: IHost) =
        unitTask {
            let scopeFactory =
                host.Services.GetRequiredService<IServiceScopeFactory>()

            use scope = scopeFactory.CreateScope()

            do!
                scope.ServiceProvider.GetRequiredService<DatabaseContext>()
                |> Db.migrateDatabase CancellationToken.None
        }

    let clearDb (host: IHost) =
        unitTask {
            let scopeFactory =
                host.Services.GetRequiredService<IServiceScopeFactory>()

            use scope = scopeFactory.CreateScope()

            let db =
                scope.ServiceProvider.GetRequiredService<DatabaseContext>()

            let entityModel =
                db.Model.GetEntityTypes()
                |> Seq.find (fun entityType -> entityType.ClrType = typeof<AbsenceRequest>)

            do! db.Database.ExecuteSqlRawAsync($"DELETE FROM {entityModel.GetTableName()}") :> Task
        }