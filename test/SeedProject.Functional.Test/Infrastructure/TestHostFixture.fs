namespace SeedProject.Functional.Infrastructure

open FSharp.Control.Tasks

open SeedProject.Functional.TestHost

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