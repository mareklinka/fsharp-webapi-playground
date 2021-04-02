open System.IO
open System.Reflection
open System.Collections.Generic

open Structurizr
open Structurizr.Analysis
open Structurizr.IO.C4PlantUML
open Structurizr.IO.C4PlantUML.ModelExtensions

open SeedProject.Architecture.Common
open SeedProject.Architecture.StructurizrExtensions
open SeedProject.Host

let exportDiagram() =
    let workspace = new Workspace(Constants.WorkspaceName, "This is a model of my software system.")
    let model = workspace.Model
    model.Enterprise <- new Enterprise(Constants.EnterpriseName)

    let user = model.AddPerson(Constants.MainUserName, "A user of my software system.")
    let softwareSystem = model.AddSoftwareSystem(Constants.SystemName, "My software system.")
    user.Uses(softwareSystem, "Uses") |> ignore

    let webApplication = softwareSystem.AddContainer(Constants.ApplicationName, "Provides users with information.", "ASP.NET Core 5/Giraffe/F#");
    let relationalDatabase = softwareSystem.AddContainer(Constants.DatabaseName, "Stores data", "MS SQL Server");
    relationalDatabase.SetIsDatabase(true)
    webApplication.Uses(relationalDatabase, "Stores information in") |> ignore

    let componentFinder = new ComponentFinder(webApplication, "SeedProject", new FunctionalComponentFinderStrategy())
    componentFinder.FindComponents() |> ignore
    model.AddImplicitRelationships() |> ignore

    let views = workspace.Views;
    let contextView = views.CreateSystemContextView(softwareSystem, "SystemContext", "An example of a System Context diagram.");
    contextView.AddAllElements();

    let containerView = views.CreateContainerView(softwareSystem, "Containers", "The container diagram from my software system.");
    containerView.AddAllElements();

    let componentView = views.CreateComponentView(webApplication, "Components", "The component diagram for the web application.");
    componentView.AddAllElements();

    use stringWriter = new StringWriter();
    let plantUMLWriter = new C4PlantUmlWriter();
    plantUMLWriter.Write(workspace, stringWriter);

    File.WriteAllText("architecture.puml", stringWriter.ToString())
    ()

let rec loadAssemblyTree (assembliesToScan: Assembly list) (accumulator: HashSet<string>) =
    match assembliesToScan with
    | [] -> ()
    | head::tail ->
        let referenced =
            head.GetReferencedAssemblies()
            |> Seq.filter (fun a -> a.Name.StartsWith "SeedProject")
            |> List.ofSeq

        // adds new items to the accumulator and uses the return value to filter out new assemblies to scan
        let newAssemblies = referenced |> List.filter (fun a -> accumulator.Add a.Name) |> List.map (fun a -> Assembly.Load(a.FullName))

        loadAssemblyTree (tail @ newAssemblies) accumulator
        ()

[<EntryPoint>]
let main argv =
    let _ = typeof<Startup> // to force loading of the root application assembly
    loadAssemblyTree [Assembly.GetEntryAssembly()] (new HashSet<string>()) // to recursively force-load dependent assemblies

    exportDiagram()
    0