namespace SeedProject.Host

open System.Threading

open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection

open SeedProject.Persistence.Model
open SeedProject.Persistence

module Program =
    let exitCode = 0

    let private migrateDb (scopeFactory: IServiceScopeFactory) =
        async {
            use scope = scopeFactory.CreateScope()
            do!
                scope.ServiceProvider.GetRequiredService<DatabaseContext>()
                |> Db.migrateDatabase CancellationToken.None
        }

    let CreateHostBuilder args =
        Host
            .CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(fun webBuilder -> webBuilder.UseStartup<Startup>() |> ignore)

    [<EntryPoint>]
    let main args =
        let host = CreateHostBuilder(args).Build()

        async {
            let scopeFactory =
                host.Services.GetRequiredService<IServiceScopeFactory>()

            do! migrateDb scopeFactory

            do! host.RunAsync() |> Async.AwaitTask
        }
        |> Async.RunSynchronously

        exitCode
