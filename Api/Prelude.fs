[<AutoOpen>]
module Prelude

open System.Collections.Generic
open System.Threading.Tasks
open Thoth.Json.Net
open System

let inline (^) f x = f x

let konst x = fun () -> x

let memoize f =
    let cache = Dictionary<_,_>()
    fun arg -> 
        match cache.TryGetValue arg with
        | true, res -> res
        | _, _ ->
            let res = f arg
            cache.Add(arg, res)
            res
    

module Encode =
    let seqOf encoder = Seq.map encoder >> Encode.seq


module Option =
    
    let mapAsync aMap opt = task {
        match opt with
        | None -> return None
        | Some x ->
            let! mapped = aMap x
            return Some mapped
    }
    

module Task =
    let ignore tsk = task {
        let! res = tsk
        return ()
    }
    
    let map f tsk = task {
        let! r = tsk
        return f r
    }
    
    let result x = Task.FromResult x
    
    let bind f tsk = task {
        let! r = tsk
        return! f r
    }
    
    let up f x = f x |> result

module TaskOption =
    let ofTaskObj tsk = Task.map Option.ofObj tsk
    
    let ofObj x = Option.ofObj x |> Task.result
    
    let map f tsk = tsk |> Task.map ^ Option.map f
    
module String =
    let toLower (x: string) = x.ToLowerInvariant()
    let eq_without_case (x: string) (y: string) =
        String.Equals(x, y, StringComparison.OrdinalIgnoreCase)