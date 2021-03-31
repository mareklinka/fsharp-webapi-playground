namespace SeedProject.Functional

open System.Threading.Tasks
open FSharp.Control.Tasks

open Xunit
open Xunit.Abstractions

open SeedProject.Functional.Serialization
open SeedProject.Functional.Infrastructure
open SeedProject.Functional.HttpResponse
open SeedProject.Host.Handlers.AbsenceRequests.Types
open FsUnit

open FsCheck
open FsCheck.Xunit
open Generators

[<Collection("Test Host Collection")>]
type BasicTests(fixture: TestHostFixture, output: ITestOutputHelper) =
    let client = fixture.Client
    let host = fixture.Host
    let logger = output.WriteLine

    [<Property(Arbitrary = [| typeof<ValidAbsenceRequestCreationModel>|])>]
    member __.CreationTest model =
        (task {
            do! TestServer.clearDb host

            do! model
                |> (Api.createAbsenceRequest client)
                |> assertOk
                :> Task

            let! allRequests =
                client
                |> Api.getAllAbsenceRequests
                |> assertOk
                |> deserialize<AbsenceRequestModel list>

            allRequests.Length |> should equal 1

            let created = allRequests.Head

            return
                created.Description = model.Description
                && created.StartDate = model.StartDate.Value
                && created.EndDate = model.EndDate
                && created.HalfDayStart = model.HalfDayStart
                && created.HalfDayEnd = model.HalfDayEnd
                && created.Type = RequestType.Holiday
        }).Result