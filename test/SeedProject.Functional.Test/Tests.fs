namespace SeedProject.Functional

open FSharp.Control.Tasks

open Xunit
open Xunit.Abstractions

open SeedProject.Functional.Serialization
open SeedProject.Functional.Infrastructure
open SeedProject.Functional.HttpResponse
open SeedProject.Host.Handlers.AbsenceRequests.Types

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

            let! created =
                model
                |> Api.createAbsenceRequest client
                |> assertOk
                |> deserialize<int>
                |- Api.getAbsenceRequest client
                |> assertOk
                |> deserialize<AbsenceRequestModel>

            return
                created.Description = model.Description
                && created.StartDate = model.StartDate.Value
                && created.EndDate = model.EndDate
                && created.HalfDayStart = model.HalfDayStart
                && created.HalfDayEnd = model.HalfDayEnd
                && created.Type = RequestType.Holiday
        }).Result