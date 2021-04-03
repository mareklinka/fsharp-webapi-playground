namespace SeedProject.Performance.Test

open FSharp.Data
open System.IO

[<RequireQualifiedAccess>]
module BenchmarkComparison =
    type private ResultJson =
        JsonProvider<"sample.json", EmbeddedResource="SeedProject.Performance.Test, sample.json", InferTypesFromValues=false>

    let private isOverThreshhold timeThreshold memoryThreshold (old: ResultJson.Benchmark) (current: ResultJson.Benchmark) =
        let compare o c t = c >= o * t

        compare old.Statistics.Mean current.Statistics.Mean timeThreshold
        || compare old.Statistics.Median current.Statistics.Median timeThreshold
        || compare old.Memory.BytesAllocatedPerOperation current.Memory.BytesAllocatedPerOperation memoryThreshold

    let private toFileSet =
        Map.toSeq
        >> Seq.map (fun (k, _) -> k)
        >> Set.ofSeq

    let private toNameSet =
        Seq.map (fun (b: ResultJson.Benchmark) -> b.FullName)
        >> Set.ofSeq

    let private printDegradation
        timeThreshold
        memoryThreshold
        benchmarkName
        (oldBenchmark: ResultJson.Benchmark)
        (currentBenchmark: ResultJson.Benchmark)
        =
        printfn ""
        printfn "~~~ Degradation in %s ~~~" benchmarkName

        printfn
            "Original: %M / %M / %M"
            oldBenchmark.Statistics.Mean
            oldBenchmark.Statistics.Median
            oldBenchmark.Memory.BytesAllocatedPerOperation

        printfn
            "Current: %M / %M / %M"
            currentBenchmark.Statistics.Mean
            currentBenchmark.Statistics.Median
            currentBenchmark.Memory.BytesAllocatedPerOperation

        printfn
            "Ratios: %.3f (limit %.3f) / %.3f (limit %.3f) / %.3f (limit %.3f)"
            (currentBenchmark.Statistics.Mean
             / oldBenchmark.Statistics.Mean)
            timeThreshold
            (currentBenchmark.Statistics.Median
             / oldBenchmark.Statistics.Median)
            timeThreshold
            (currentBenchmark.Memory.BytesAllocatedPerOperation
             / oldBenchmark.Memory.BytesAllocatedPerOperation)
            memoryThreshold

    let private compareBenchmarkFile file original current =
        let timeThreshold = 1.2M
        let memoryThreshold = 1.2M

        let intersection =
            Set.intersect (original |> toNameSet) (current |> toNameSet)

        let degradationsInFile =
            intersection
            |> Seq.fold
                (fun state name ->
                    let oldBenchmark =
                        original
                        |> Array.find (fun b -> b.FullName = name)

                    let currentBenchmark =
                        current |> Array.find (fun b -> b.FullName = name)

                    match isOverThreshhold timeThreshold memoryThreshold oldBenchmark currentBenchmark with
                    | false -> state
                    | true ->
                        printDegradation timeThreshold memoryThreshold name oldBenchmark currentBenchmark
                        state + 1)
                0

        let isSetDifferent =
            match (intersection |> Seq.length, original.Length, current.Length) with
            | (i, o, n) when i = o && o = n -> false
            | _ ->
                printfn "%s: Set of baseline benchmarks has changes, you might need to create a new baseline file" file
                true

        degradationsInFile, isSetDifferent

    let private compareBenchmarkFiles originalMap currentMap =
        let fileIntersection =
            Set.intersect (originalMap |> toFileSet) (currentMap |> toFileSet)

        let (totalDegradations, benchmarkSetDifference) =
            fileIntersection
            |> Seq.fold
                (fun (degradations, setDifference) file ->
                    let original = originalMap |> Map.find file
                    let current = currentMap |> Map.find file

                    let (degradationsInFile, isSetDifferent) =
                        compareBenchmarkFile file original current

                    (degradations + degradationsInFile, setDifference || isSetDifferent))
                (0, false)

        let isFileSetDifferent =
            match (fileIntersection |> Seq.length, originalMap.Count, currentMap.Count) with
            | (i, o, n) when i = o && o = n -> false
            | _ ->
                printfn "Set of baseline benchmark files has changes, you might need to create a new baseline file"
                true

        totalDegradations, isFileSetDifferent || benchmarkSetDifference

    let private loadBenchmarkResults directory =
        match Directory.Exists(directory) with
        | false -> Map.empty<string, ResultJson.Benchmark array>
        | true ->
            Directory.GetFiles(directory, "*.json")
            |> Array.map
                (fun f ->
                    printfn "Loading file %s" f
                    let original = ResultJson.Load(f)
                    f, original.Benchmarks)
            |> Map.ofSeq

    let runBenchmarkWithComparison benchmarkRunner =
        let directory =
            Path.Combine(Directory.GetCurrentDirectory(), "benchmark\\results")

        let originalMap = loadBenchmarkResults directory

        Directory.GetFiles(directory)
        |> Array.iter (fun f -> File.Delete f)

        benchmarkRunner ()

        let currentMap = loadBenchmarkResults directory

        let (totalDegradations, setDifference) =
            compareBenchmarkFiles originalMap currentMap

        printfn "Degradations: %i" totalDegradations
        (totalDegradations, setDifference)
