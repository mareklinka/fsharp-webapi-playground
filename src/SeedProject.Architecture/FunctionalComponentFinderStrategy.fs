namespace SeedProject.Architecture.StructurizrExtensions

open System
open System.Collections.Generic
open System.Linq
open System.Reflection

open Structurizr
open Structurizr.Analysis
open Structurizr.Annotations

type FunctionalComponentFinderStrategy() =
    let mutable typeRepository : ITypeRepository = null
    let mutable componentFinder : ComponentFinder = null
    let componentsFound = new HashSet<Component>()

    let addUsesComponentRelationship (sourceType: Type) (destinationType: Type) description technology =
        let source =
            componentFinder.Container.GetComponentOfType(sourceType.AssemblyQualifiedName)

        let destination =
            componentFinder.Container.GetComponentOfType(destinationType.AssemblyQualifiedName)

        match destination with
        | null -> ()
        | _ ->
            let relationships =
                source
                    .Relationships
                    .Where(fun r -> r.Destination.Equals(destination))
                    .ToList()

            match relationships.Count with
            | 0 ->
                source.Uses(destination, description, technology)
                |> ignore
            | _ ->
                relationships
                |> Seq.iter
                    (fun r ->
                        match r.Description with
                        | null
                        | "" -> source.Model.ModifyRelationship(r, description, technology)
                        | _ -> ())

                ()

    let findUsesComponentAnnotations (ce: CodeElement) =
        let t = typeRepository.GetType(ce.Type)

        match t with
        | null -> ()
        | _ ->
            t.GetRuntimeFields()
            |> Seq.iter
                (fun f ->
                    let annotation =
                        f.GetCustomAttribute<UsesComponentAttribute>()

                    match annotation with
                    | null -> ()
                    | _ -> addUsesComponentRelationship t f.FieldType annotation.Description annotation.Technology)

            t.GetRuntimeProperties()
            |> Seq.iter
                (fun p ->
                    let annotation =
                        p.GetCustomAttribute<UsesComponentAttribute>()

                    match annotation with
                    | null -> ()
                    | _ -> addUsesComponentRelationship t p.PropertyType annotation.Description annotation.Technology)

            t.GetRuntimeMethods()
            |> Seq.collect (fun m -> m.GetParameters())
            |> Seq.iter
                (fun p ->
                    let annotation =
                        p.GetCustomAttribute<UsesComponentAttribute>()

                    match annotation with
                    | null -> ()
                    | _ -> addUsesComponentRelationship t p.ParameterType annotation.Description annotation.Technology)

    let findUsesComponentExAnnotations (ce: CodeElement) =
        let sourceType = typeRepository.GetType(ce.Type)

        match sourceType with
        | null -> ()
        | _ ->
            let typeAttributes =
                sourceType.GetCustomAttributes<UsesComponentExAttribute>()

            let methodAttributes =
                sourceType.GetRuntimeMethods()
                |> Seq.collect (fun p -> p.GetCustomAttributes<UsesComponentExAttribute>())
                |> Seq.filter (fun a -> a <> null)

            typeAttributes
            |> Seq.append methodAttributes
            |> Seq.iter
                (fun attribute ->
                    let target =
                        componentsFound.SingleOrDefault(fun c -> c.Name = attribute.ComponentName)

                    let destinationType = typeRepository.GetType(target.Type)
                    addUsesComponentRelationship sourceType destinationType attribute.Description attribute.Technology)

    let findContainerByNameOrCanonicalNameOrId (c: Component) (name: string) =
        let container =
            c.Container.SoftwareSystem.GetContainerWithName(name)

        match container with
        | null ->
            let element =
                c.Model.GetElementWithCanonicalName(name)

            match element with
            | :? Container as e when e <> null -> e
            | _ -> c.Model.GetElement(name) :?> Container
        | _ -> container


    let findUsesContainerAnnotations (ce: CodeElement) =
        let t = typeRepository.GetType(ce.Type)

        match t with
        | null -> ()
        | _ ->
            let c =
                componentFinder.Container.GetComponentOfType(t.AssemblyQualifiedName)

            let attributes =
                t.GetCustomAttributes<UsesContainerAttribute>()

            attributes
            |> Seq.iter
                (fun attribute ->
                    let container =
                        findContainerByNameOrCanonicalNameOrId c attribute.ContainerName

                    match container with
                    | null -> ()
                    | _ ->
                        c.Uses(container, attribute.Description, attribute.Technology)
                        |> ignore)

    let findUsedByPersonAnnotations (ce: CodeElement) =
        let t = typeRepository.GetType(ce.Type)

        match t with
        | null -> ()
        | _ ->
            let c =
                componentFinder.Container.GetComponentOfType(t.AssemblyQualifiedName)

            let attributes =
                t.GetCustomAttributes<UsedByPersonAttribute>()

            attributes
            |> Seq.iter
                (fun attribute ->
                    let person =
                        c.Model.GetPersonWithName(attribute.PersonName)

                    person.Uses(c, attribute.Description, attribute.Technology)
                    |> ignore)

    interface ComponentFinderStrategy with
        override __.AfterFindComponents() : unit =
            componentsFound
            |> Seq.collect (fun c -> c.CodeElements)
            |> Seq.iter
                (fun ce ->
                    ce.Visibility <- typeRepository.FindVisibility(ce.Type)
                    ce.Category <- typeRepository.FindCategory(ce.Type)
                    findUsesComponentAnnotations ce
                    findUsesComponentExAnnotations ce
                    findUsesContainerAnnotations ce
                    findUsedByPersonAnnotations ce)

        override __.BeforeFindComponents() : unit =
            typeRepository <- new ReflectionTypeRepository(componentFinder.Namespace, componentFinder.Exclusions)

        member __.ComponentFinder
            with set (v: ComponentFinder): unit = componentFinder <- v

        override __.FindComponents() : IEnumerable<Structurizr.Component> =
            let types = typeRepository.GetAllTypes().ToList()

            types
            |> Seq.iter
                (fun t ->
                    let componentAttribute =
                        t.GetCustomAttribute<ComponentAttribute>()

                    match componentAttribute with
                    | null -> ()
                    | _ ->
                        componentFinder.Container.AddComponent(
                            t.Name,
                            t,
                            componentAttribute.Description,
                            componentAttribute.Technology
                        )
                        |> componentsFound.Add
                        |> ignore

                        ())

            componentsFound.AsEnumerable()
