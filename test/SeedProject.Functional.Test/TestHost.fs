namespace SeedProject.Functional

open System.Net
open System.Net.Http
open System.Threading.Tasks
open System.Text.Json
open System.Text.Json.Serialization
open System.Threading

open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.TestHost
open Microsoft.Extensions.DependencyInjection
open Microsoft.EntityFrameworkCore

open FSharp.Control.Tasks
open FsUnit

open SeedProject.Host
open SeedProject.Persistence.Model
open SeedProject.Persistence

module TestServer =
    let private testHostBuilder =
        Host.CreateDefaultBuilder().ConfigureWebHostDefaults(fun h ->
            h.UseTestServer()
             .UseStartup<Startup>()
             .UseEnvironment("FunctionalTests") |> ignore)

    let start() =
        task {
            let! host = testHostBuilder.StartAsync()
            return (host, host.GetTestClient())
        }

    let stop (host: IHost) =
        unitTask {
            do! host.StopAsync()
        }

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

            let db = scope.ServiceProvider.GetRequiredService<DatabaseContext>()
            let entityModel = db.Model.GetEntityTypes() |> Seq.find (fun entityType -> entityType.ClrType = typeof<AbsenceRequest>)
            do! db.Database.ExecuteSqlRawAsync($"DELETE FROM {entityModel.GetTableName()}") :> Task
        }

module Serialization =
    let jsonOptions = JsonSerializerOptions()
    jsonOptions.Converters.Add(JsonFSharpConverter())

    let serialize value = JsonSerializer.Serialize(value, jsonOptions)

    let deserialize<'a> (responseTask: Task<HttpResponseMessage>) : Task<'a> =
        task {
            let! response = responseTask
            let! content = response.Content.ReadAsStringAsync()

            return JsonSerializer.Deserialize<'a>(content, jsonOptions)
        }

module HttpResponse =
    let assertOk (responseTask: Task<HttpResponseMessage>) =
        task {
            let! response = responseTask

            response.StatusCode |> should equal HttpStatusCode.OK

            return response
        }

    let assertStatus (expectedCode: HttpStatusCode) (responseTask: Task<HttpResponseMessage>) =
        task {
            let! response = responseTask
            response.StatusCode |> should equal expectedCode

            return response
        }

module Api =
    let getAbsenceRequest (client: HttpClient) (id: int) =
        client.GetAsync($"/api/absencerequest/{id}")

    let getAllAbsenceRequests (client: HttpClient) =
        client.GetAsync($"/api/absencerequest")

    let createAbsenceRequest (client: HttpClient) (model: Handlers.AbsenceRequests.Types.CreateRequestInputModel) =
        client.PutAsync($"/api/absencerequest", new StringContent(model |> Serialization.serialize))