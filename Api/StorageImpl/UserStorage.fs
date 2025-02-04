namespace Api.StorageImpl

open Microsoft.EntityFrameworkCore
open System
open System.Linq
open Api.Core
open Api.Storage


type UserStorage(db:DbContext) =
    inherit Storage<AppUser, AppUserDal>(db)
    
    override this.ToDal(x) = Task.up AppUserDalMapper.toDal x
    override this.ToDomain(x) = Task.up AppUserDalMapper.toDomain x
    
    interface IUserStorage with
        member this.FindByEmail(Email email) =
            db.Set<AppUserDal>().FirstOrDefaultAsync(fun x -> x.email = email)
            |> TaskOption.ofTaskObj
            |> TaskOption.map AppUserDalMapper.toDomain



