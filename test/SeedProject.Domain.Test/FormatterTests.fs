namespace SeedProject.Domain.Tests

module FormatterTests =
    open FsCheck.Xunit
    open SeedProject.Domain
    open SeedProject.Domain.AbsenceRequests
    open System.Text.RegularExpressions

    [<Property>]
    let ``Formatting a request has 1 table row`` request =
        let result = request |> Formatter.formatAbsenceRequest
        result.StartsWith("<tr>") && result.EndsWith("</tr>")

    [<Property>]
    let ``Formatting a request has 5 table cells`` request =
        let result = request |> Formatter.formatAbsenceRequest
        let m = Regex.Matches(result, "<td>.*?</td>", RegexOptions.Singleline) |> Seq.cast<Match>
        m |> Seq.length = 5

