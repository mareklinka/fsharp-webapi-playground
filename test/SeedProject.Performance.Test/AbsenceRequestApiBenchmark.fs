namespace SeedProject.Performance.Test

open System
open System.Net.Http
open System.Threading.Tasks

open Microsoft.Extensions.Hosting

open FSharp.Control.Tasks

open BenchmarkDotNet.Attributes

open SeedProject.Host.Handlers.AbsenceRequests.Types
open SeedProject.Functional.TestHost

type AbsenceRequestApiBenchmark() as this =
    let createModel : CreateRequestInputModel =
        { StartDate = Some(new DateTime(2021, 1, 1))
          EndDate = Some(new DateTime(2021, 2, 1))
          HalfDayStart = Some true
          HalfDayEnd = Some false
          Description = Some "My description"
          Duration = None
          PersonalDayType = None
          Type = RequestType.Holiday }

    [<DefaultValue>]
    val mutable host: IHost

    [<DefaultValue>]
    val mutable client: HttpClient

    [<Benchmark>]
    member __.GetEmptyRequestList() =
        task { do! Api.getAllAbsenceRequests this.client :> Task }

    [<Benchmark>]
    member __.CreateNewRequest() =
        task { do! Api.createAbsenceRequest this.client createModel :> Task }

    [<GlobalSetup>]
    member __.StartServer() =
        task {
            let! (host, client) = TestServer.start ()
            this.host <- host
            this.client <- client
        }
        :> Task

    [<GlobalCleanup>]
    member __.StopServer() =
        task {
            do! this.host.StopAsync()
            this.host.Dispose()
        }
        :> Task

    [<IterationSetup(Targets = [| "GetEmptyRequestList" |])>]
    member __.ClearDatabase() =
        (task {
            printfn "Clearing DB"
            do! TestServer.clearDb this.host
         })
            .Wait()
