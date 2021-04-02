namespace SeedProject.Functional.TestHost

open System.Net.Http
open System.Text.Json
open System.Text.Json.Serialization
open System.Threading.Tasks

open FSharp.Control.Tasks

open SeedProject.Host

module Api =
    let private jsonOptions = JsonSerializerOptions()
    jsonOptions.Converters.Add(JsonFSharpConverter())

    let private serialize value =
        JsonSerializer.Serialize(value, jsonOptions)

    let private deserialize<'a> (responseTask: Task<HttpResponseMessage>) : Task<'a> =
        task {
            let! response = responseTask
            let! content = response.Content.ReadAsStringAsync()

            return JsonSerializer.Deserialize<'a>(content, jsonOptions)
        }

    let getAbsenceRequest (client: HttpClient) (id: int) =
        client.GetAsync($"/api/absencerequest/{id}")

    let getAllAbsenceRequests (client: HttpClient) = client.GetAsync($"/api/absencerequest")

    let createAbsenceRequest (client: HttpClient) (model: Handlers.AbsenceRequests.Types.CreateRequestInputModel) =
        client.PutAsync($"/api/absencerequest", new StringContent(model |> serialize))

    let deleteAbsenceRequest (client: HttpClient) (id: int) =
        client.DeleteAsync($"/api/absencerequest/{id}")