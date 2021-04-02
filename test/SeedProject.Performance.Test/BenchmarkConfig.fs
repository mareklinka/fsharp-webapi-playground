namespace SeedProject.Performance.Test

open BenchmarkDotNet.Jobs
open BenchmarkDotNet.Diagnosers
open BenchmarkDotNet.Exporters
open BenchmarkDotNet.Exporters.Json
open BenchmarkDotNet.Loggers
open BenchmarkDotNet.Columns
open BenchmarkDotNet.Engines

type BenchmarkConfig() as this =
    inherit BenchmarkDotNet.Configs.ManualConfig()

    let job =
        Job
            .Default
            .WithId("MacroBenchmark")
            .WithGcServer(true)
            .WithStrategy(RunStrategy.Monitoring)
            .WithWarmupCount(1)
            .WithLaunchCount(1)
            .WithIterationCount(20)

    do
        this
            .AddJob(job)
            .AddDiagnoser(MemoryDiagnoser.Default)
            .AddExporter(JsonExporter.Brief)
            .AddExporter(HtmlExporter.Default)
            .AddLogger(ConsoleLogger.Unicode)
            .AddColumnProvider(DefaultColumnProviders.Instance)
            .WithArtifactsPath(".\\benchmark")
        |> ignore
