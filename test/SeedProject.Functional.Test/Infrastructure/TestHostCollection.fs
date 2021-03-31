namespace SeedProject.Functional.Infrastructure

open Xunit

[<CollectionDefinition("Test Host Collection")>]
type TestHostCollection(fixture: TestHostFixture) =
    let f = fixture

    interface ICollectionFixture<TestHostFixture>