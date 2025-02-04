namespace Api.Storage

open System
open System.Threading.Tasks
open Api.Core
open Microsoft.EntityFrameworkCore
open System.Linq

type IStorage<'domain> =
    abstract Get : id:Guid -> Task<'domain>
    abstract Find : id:Guid -> Task<'domain option>
    abstract Save : aggregate:'domain -> Task<unit>
    abstract Commit : unit -> Task<unit>
    

[<AbstractClass>]
type Storage<'domain, 'dal when 'dal : not struct and 'domain : not struct>
    (db: DbContext) =
    
    // не инжекчу потому что хочу явно не провтыкать мапперы
    abstract ToDomain : x:'dal -> Task<'domain>
    abstract ToDal : x:'domain -> Task<'dal>
    
    interface IStorage<'domain> with
        member this.Get(id) = task {
            let! dal = db.Set<'dal>().FindAsync(id)
            let! domain = this.ToDomain(dal)
            return domain
        }
            
        member this.Save(aggregate) = task {
            let! dal = this.ToDal(aggregate)
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
                let! domain = this.ToDomain(dal)
                return Some domain
        }

        member this.Commit() = task {
            let! _ = db.SaveChangesAsync()
            let! _ = db.Database.CommitTransactionAsync()
            return ()
        }
