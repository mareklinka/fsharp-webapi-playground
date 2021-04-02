namespace SeedProject.Functional

open FSharp.Control.Tasks

open Xunit
open Xunit.Abstractions

open SeedProject.Functional.Infrastructure
open SeedProject.Host.Handlers.AbsenceRequests.Types
open SeedProject.Functional.TestHost

open FsCheck.Xunit
open Generators

[<Collection("Test Host Collection")>]
type BasicTests(fixture: TestHostFixture, output: ITestOutputHelper) =
    let client = fixture.Client
    let host = fixture.Host
    let logger = output.WriteLine

    [<Property(Arbitrary = [| typeof<ValidAbsenceRequestCreationModel>|])>]
    member __.``Creating an absence request persists the data correctly`` model =
        (task {
            do! TestServer.clearDb host

            let! created =
                model
                |> Api.createAbsenceRequest client
                |> Test.assertOk logger
                |> Test.deserialize<int>
                |- Api.getAbsenceRequest client
                |> Test.assertOk logger
                |> Test.deserialize<AbsenceRequestModel>

            return
                created.Description = model.Description
                && created.StartDate = model.StartDate.Value
                && created.EndDate = model.EndDate
                && created.HalfDayStart = model.HalfDayStart
                && created.HalfDayEnd = model.HalfDayEnd
                && created.Type = RequestType.Holiday
        }).Result

    [<Property(Arbitrary = [| typeof<ValidAbsenceRequestCreationModel>|])>]
    member __.``Deletion removes the deleted request`` model =
        (task {
            do! TestServer.clearDb host

            let! count =
                model
                |> Api.createAbsenceRequest client
                |> Test.assertOk logger
                |> Test.deserialize<int>
                |- Api.deleteAbsenceRequest client
                |- (fun _ -> Api.getAllAbsenceRequests client)
                |> Test.assertOk logger
                |> Test.deserialize<AbsenceRequestModel list>
                |= List.length

            return count = 0
        }).Result