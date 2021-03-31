namespace SeedProject.Functional

open System
open System.Threading.Tasks
open FSharp.Control.Tasks

open Xunit
open Xunit.Abstractions

open SeedProject.Functional.Serialization
open SeedProject.Functional.HttpResponse
open SeedProject.Host.Handlers.AbsenceRequests
open SeedProject.Host.Handlers.AbsenceRequests.CreateRequest.Types
open FsUnit

type TestHostFixture() as this =
    member val Host = null with get, set
    member val Client = null with get, set

    interface Xunit.IAsyncLifetime with
        member __.InitializeAsync() =
            unitTask {
                let! (testHost, client) = TestServer.start ()
                do! testHost |> TestServer.migrateDb
                this.Host <- testHost
                this.Client <- client
            }

        member __.DisposeAsync() =
            unitTask {
                do! this.Host |> TestServer.stop
                do this.Host.Dispose()
            }

[<CollectionDefinition("Test Host Collection")>]
type TestHostCollection(fixture: TestHostFixture) =
    let f = fixture

    interface ICollectionFixture<TestHostFixture>



[<Collection("Test Host Collection")>]
type BasicTests(fixture: TestHostFixture, output: ITestOutputHelper) =
    let client = fixture.Client
    let logger = output.WriteLine

    [<Fact>]
    member __.CreationTest() =
        task {
            let model =
                { AddRequestInputModel.Type = RequestType.Holiday
                  Description = "AAA"
                  HalfDayEnd = Some false
                  HalfDayStart = Some true
                  Duration = None
                  StartDate = Some DateTime.Now
                  EndDate = Some DateTime.Now
                  PersonalDayType = None }

            let createRequest = client |> Api.createAbsenceRequest
            let getRequest = client |> Api.getAbsenceRequest

            do! model
                |> createRequest
                |> assertOk
                :> Task

            let! response =
                1
                |> getRequest
                |> assertOk
                |> deserialize<GetRequest.Private.AbsenceRequestModel>

            response.Description |> should equal model.Description
        }
