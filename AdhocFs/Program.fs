open System
open FSharpPlus

// From <https://github.com/gmpl/FSharpPlus/blob/d5723e5181b72770b3fd6461a693213aed57aa09/FSharpPlus/Identity.fs>

type Identity<'t> = Identity of 't with
    static member Return x = Identity x                                             : Identity<'T>
    static member Bind  (Identity x, f :'T -> Identity<'U>) = f x                   : Identity<'U>
    static member (<*>) (Identity (f : 'T->'U), Identity (x : 'T)) = Identity (f x) : Identity<'U>
    static member Map   (Identity x, f : 'T->'U) = Identity (f x)                   : Identity<'U>

[<EntryPoint>]
let main argv =

  let m =
    monad {
      let! x = Identity 1  // error
      return x + 1
    }

  printfn "%A" m

  // exit code
  0
