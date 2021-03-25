namespace SeedProject.Domain

open SeedProject.Domain.Common

module Person =
    type FirstName = FirstName of string
    type LastName = LastName of string
    type Name =
        {
            FirstName: FirstName;
            LastName: LastName;
        }
    type Person =
        {
            Id: DatabaseId;
            Name: Name;
        }

    open SeedProject.Persistence.Model

    let ConstructPerson (p: PersonEntity) =
        { Id = Id p.Id
          Name =
              { FirstName = FirstName p.FirstName
                LastName = LastName p.LastName } }
