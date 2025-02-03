namespace Api.Storage

open System
open System.Threading.Tasks
open Microsoft.EntityFrameworkCore
open System.Linq

type IStorage<'aggregate> =
    abstract Get : id:Guid -> Task<'aggregate>
    abstract Find : id:Guid -> Task<'aggregate option>
    abstract Save : aggregate:'aggregate -> Task<unit>
    abstract Commit : unit -> Task<unit>


type IDalMapper<'domain, 'dal> =
    abstract ToDomain : 'dal -> Task<'domain>
    abstract ToDal : 'domain -> Task<'dal>

type Storage<'domain, 'dal when 'dal : not struct and 'domain : not struct>
    (db: DbContext, mapper: IDalMapper<'domain, 'dal>) =
    
    interface IStorage<'domain> with
        member this.Get(id) = task {
            let! dal = db.Set<'dal>().FindAsync(id)
            let! domain = mapper.ToDomain(dal)
            return domain
        }
            
        member this.Save(aggregate) = task {
            let! dal = mapper.ToDal(aggregate)
            let! existEntry = db.Set<'dal>().FindAsync(Guid.NewGuid())
            
            if Unchecked.equals existEntry null then
                let! _ = db.Set<'dal>().AddAsync(dal)
                ()
            else db.Update(dal) |> ignore
            
            return ()
        }

        member this.Find(id) = task {
            let! dal = db.Set<'dal>().FindAsync(id)
            match dal with
            | null -> return None
            | dal ->
                let! domain = mapper.ToDomain(dal)
                return Some domain
        }

        member this.Commit() = task {
            let! _ = db.SaveChangesAsync()
            let! _ = db.Database.CommitTransactionAsync()
            return ()
        }

type UserDal = {
    id: Guid
    email: string
    credits: int
    created_at: DateTimeOffset
}

type UserDalMapper() =
    interface IDalMapper<UserDal, UserDal> with
        member this.ToDal(state) = Task.FromResult state
        member this.ToDomain(state) = Task.FromResult state


type IUserStorage =
    inherit IStorage<UserDal>
    abstract FindByEmail : email:string -> Task<UserDal option>

type UserStorage(db, mapper) =
    inherit Storage<UserDal, UserDal>(db, mapper)
    
    interface IUserStorage with
        member this.FindByEmail(email) = task {
            let! user = db.Set<UserDal>().FirstOrDefaultAsync(fun x -> x.email = email)
            return! Option.ofObj user |> Option.mapAsync mapper.ToDomain
        }



