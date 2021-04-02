namespace SeedProject.Architecture.StructurizrExtensions

open System

[<AllowNullLiteral>]
[<AttributeUsage(AttributeTargets.Class ||| AttributeTargets.Method, AllowMultiple = true)>]
type UsesComponentExAttribute(t: string) =
    inherit Attribute()

    member val ComponentName : string = t with get, set
    member val Description : string = null with get, set
    member val Technology : string = null with get, set