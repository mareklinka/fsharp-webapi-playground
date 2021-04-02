namespace SeedProject.Architecture.StructurizrExtensions

open System

[<AllowNullLiteral>]
[<AttributeUsage(AttributeTargets.Class, AllowMultiple = true)>]
type TypeUsesComponentAttribute(t: string) =
    inherit Attribute()

    member val ComponentName : string = t with get, set
    member val Description : string = null with get, set
    member val Technology : string = null with get, set