module Util

open System
open System.Collections.Generic
open System.Diagnostics
open System.IO
open System.Text

[<AutoOpen>]
module Misc =
  let tap f x = f x; x

  let if' pred f x =
    if pred x then f x

module Seq =
  let unzip self =
    self |> List.ofSeq |> List.unzip

  let chunk joins self = 
    let newList () = List<'a>()

    let (acc, lastChunk) =
        self
        |> Seq.fold (fun state x ->
            let (acc, (cur: List<_>)) = state
            if cur.Count = 0 || (x |> joins cur)
            then
              cur.Add(x)
              state
            else
              (cur :: acc, newList ())
            ) ([], newList ())
    let acc =
        if lastChunk.Count = 0
        then acc
        else lastChunk :: acc
    acc |> List.rev

module DateTime =
  let middle (l: DateTime) (r: DateTime) =
    l.AddMilliseconds((r - l).Milliseconds / 2 |> float)
    
module File =
  let isHiddenOrSystem path =
    File
      .GetAttributes(path)
      .HasFlag(FileAttributes.Hidden ||| FileAttributes.System)

module Str =
  let splitWith (s: string) (self: string) =
    self.Split([| s |], StringSplitOptions.RemoveEmptyEntries)

module TextReader =
  let readLineAll (self: TextReader) =
    Seq.unfold (fun () ->
      match self.ReadLine() with
      | null -> None
      | line -> Some (line, ())
      ) ()

module Diagnostics =
  let execCmdline timeout verb arg =
    let psi =
      ProcessStartInfo
        ( FileName = verb
        , Arguments = arg
        , CreateNoWindow = true
        , UseShellExecute = false
        , RedirectStandardOutput = true
        , StandardOutputEncoding = Encoding.UTF8
        )
    use p = Process.Start(psi)
    if p.WaitForExit(timeout)
    then Some (p.StandardOutput |> TextReader.readLineAll)
    else None
