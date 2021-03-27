namespace SeedProject.App

open SeedProject.Domain
open SeedProject.Domain.Common
open Microsoft.EntityFrameworkCore
open SeedProject.Persistence.Model

module DependencyInjection =
    let CreateContextOptions =
        let builder =
            new DbContextOptionsBuilder<DatabaseContext>()

        builder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Integrated Security=true;Database=FSharpSeedDatabase")
        |> ignore

        fun () -> builder.Options

    let CreateContext (options: DbContextOptions<DatabaseContext>) = new DatabaseContext(options)

    let LifetimeScope<'a when 'a: not struct> (factory: unit -> 'a) =
        let instance = factory ()

        match box instance with
        | :? System.IDisposable as disposable -> (fun () -> instance), disposable
        | _ ->
            (fun () -> instance),
            { new System.IDisposable with
                member __.Dispose() = () }

    type DbContextLifetimeScope = unit -> DatabaseContext

module RequestPipeline =
    open DependencyInjection
    open Microsoft.EntityFrameworkCore.Storage
    open SeedProject.Persistence
    open System.Threading

    let private operation =
        OperationResult.OperationResultBuilder.Instance

    type RequestContext<'a, 'b when 'a : not struct> =
        {
            Database: DatabaseContext;
            Input: 'a;
            Transaction: IDbContextTransaction;
            CancellationToken: CancellationToken;
            Result: Option<OperationResult.OperationResult<'b>>
        }

    let respond result =
        async {
            let! r = result

            match r with
            | Some opResult ->
                match opResult with
                | OperationResult.Success _ -> printf "Absence request updated"
                | OperationResult.ValidationError (code, ValidationMessage message) ->
                    printf "Validation error %A: %s" code message
                | OperationResult.OperationError (code, OperationMessage message) ->
                    printf "Operation error %A: %s" code message
            | None ->
                printf "Operation termianted without a result"
            ()
        }

    let cleanup operationResultTask =
        async {
            let! operationResult = operationResultTask
            printf "Cleaning up..."

            let actualResult =
                match operationResult with
                | OperationResult.Success context ->
                    context.Transaction.Dispose()
                    context.Database.Dispose()
                    context.Result
                | _ -> None

            printf "Cleanup done..."
            return actualResult
        }

    let buildRequestContext<'a, 'b when 'a : not struct and 'b : not struct> requestReader =
        async {
            let database = (CreateContextOptions >> CreateContext)()
            let cts = new CancellationTokenSource();
            let! t = Db.beginTransaction cts.Token database
            let! readerResult = requestReader();

            return operation {
                let! requestReaderValue = readerResult
                let context: RequestContext<'a, 'b>  = { RequestContext.Database = database; Input = requestReaderValue; Transaction = t; CancellationToken = cts.Token; Result = None }
                return context
            }
        }

    let saveAndCommit (context: RequestContext<_, _>) r =
        async {
            do!
                context.Database
                |> Db.saveChanges CancellationToken.None

            do! context.Transaction |> Db.commit CancellationToken.None

            return operation { return r }
        }

