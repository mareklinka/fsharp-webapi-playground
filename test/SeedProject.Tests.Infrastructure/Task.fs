namespace SeedProject.Tests.Infrastructure
open System.Threading.Tasks

module Task =
    let result (task: Task<_>) = task.Result
