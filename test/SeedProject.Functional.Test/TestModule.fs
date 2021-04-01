namespace SeedProject.Functional

open System.Threading.Tasks
open System.Net
open System.Net.Http
open System.Text.Json
open System.Text.Json.Serialization

open FSharp.Control.Tasks

open FsUnit

module Test =
    let assertOk outputWriter (responseTask: Task<HttpResponseMessage>) =
        task {
            let! response = responseTask

            match response.StatusCode with
            | HttpStatusCode.OK -> ()
            | _ ->
                let! content = response.Content.ReadAsStringAsync()
                content |> outputWriter
                response.StatusCode |> should equal HttpStatusCode.OK

            return response
        }

    let assertStatus (expectedCode: HttpStatusCode) (responseTask: Task<HttpResponseMessage>) =
        task {
            let! response = responseTask
            response.StatusCode |> should equal expectedCode

            return response
        }

    let jsonOptions = JsonSerializerOptions()
    jsonOptions.Converters.Add(JsonFSharpConverter())

    let serialize value =
        JsonSerializer.Serialize(value, jsonOptions)

    let deserialize<'a> (responseTask: Task<HttpResponseMessage>) : Task<'a> =
        task {
            let! response = responseTask
            let! content = response.Content.ReadAsStringAsync()

            return JsonSerializer.Deserialize<'a>(content, jsonOptions)
        }