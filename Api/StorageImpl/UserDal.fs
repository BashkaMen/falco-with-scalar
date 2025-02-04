namespace Api.StorageImpl

open System
open Api.Core

type AppUserDal = {
    id: Guid
    email: string
    credits: int
    roles: string[]
    created_at: DateTimeOffset
}

module UserRoleDalMapper =
    let toDal = function
        | Admin -> "admin"
        | RegularUser -> "regular_user"
    
    let toDomain = function
        | "admin" -> Admin
        | "regular_user" -> RegularUser
        | _ -> failwith "Unknown role"
    
module AppUserDalMapper =
    let toDomain (x: AppUserDal) : AppUser =
        { id = x.id
          email = Email.fromString x.email
          credits = x.credits
          roles = x.roles |> Seq.map UserRoleDalMapper.toDomain |> Set.ofSeq 
          created_at = x.created_at }
        
    let toDal (x: AppUser) : AppUserDal =
        { id = x.id
          email = Email.value x.email
          credits = x.credits
          roles = x.roles |> Seq.map UserRoleDalMapper.toDal |> Array.ofSeq
          created_at = x.created_at }
