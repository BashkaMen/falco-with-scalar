namespace Api.Storage

open System
open System.Threading.Tasks
open Api.Core

type IUserStorage =
    inherit IStorage<AppUser>
    abstract FindByEmail : email:Email -> Task<AppUser option>
