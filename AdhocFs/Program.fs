module Program

open System
open System.Collections.Generic
open System.Diagnostics
open System.IO
open System.Windows

open Config
open Util

module Git =
  let exec =
    Diagnostics.execCmdline (config.Timeout.TotalMilliseconds |> int) "git"

  let execIgnore = exec >> ignore

  let workOnNewBranch f =

    let prevBranchName =
        exec "rev-parse --abbrev-ref HEAD"
        |> Option.get
        |> tap (if' ((=) "HEAD") (fun () ->
            failwith "HEAD must be a branch."
            ))

    // generate unique branch name
    let tempBranchName =
        Path.GetRandomFileName().Replace(".", "")

    execIgnore (sprintf "checkout -b %s" tempBranchName)

    let checkoutOrigBranch () =
        execIgnore (sprintf "checkout %s" prevBranchName)

    let doMerge () =
        sprintf "merge %s --no-ff -m \"Merge into %s: Add untracked files\""
          tempBranchName
          tempBranchName
        |> execIgnore

    let eraseTempBranch () =
        execIgnore (sprintf "branch -d %s" tempBranchName)

    try
      try
        f ()
      finally
        checkoutOrigBranch ()

      doMerge ()
    finally
      eraseTempBranch ()

let enumUntrackedFiles () =
  Git.exec "status --porcelain --untracked-files=all"
  |> Option.get // or raise
  |> Str.splitWith (Environment.NewLine)
  |> Array.choose (fun line ->
      if (line |> String.length) > 3 && line.StartsWith("??")
      then Some (line.Substring(3))
      else None
      )

let chunkByLWT paths =
  paths
  |> Array.map (fun path -> (File.GetLastWriteTimeUtc(path), path))
  |> Array.sort
  |> Seq.chunk (fun chunk (lwt, path) ->
      if chunk |> Seq.isEmpty
      then true
      else
        let (lwtPrev, _) = chunk |> Seq.last
        (lwt - lwtPrev) <= (config.Threshold)
      )

let commitMessageFromChunk paths =
  if paths |> Seq.length = 1
  then sprintf "Add %s" (paths |> Seq.exactlyOne |> Path.GetFileName)
  else "Add untracked files"

let dateStrFromChunk dates =
  let date =
      DateTime.middle
        (dates |> Seq.head)
        (dates |> Seq.last)

  // RFC 2822
  date.ToString("R")

let commitChunk (dates, (paths: #seq<string>)) =
  String.Join("\" \"", paths)
  |> sprintf "add \"%s\""
  |> Git.exec
  |> ignore

  sprintf "commit -m \"%s\" --date=\"%s\""
    (paths |> commitMessageFromChunk)
    (dates |> dateStrFromChunk)
  |> Git.exec
  |> ignore

let main_impl () =
  let paths =
      enumUntrackedFiles ()
      |> Array.filter (File.isHiddenOrSystem >> not)
      |> tap (if' (Array.isEmpty) (fun () -> failwith "No target files."))

  Git.workOnNewBranch (fun () ->
      paths
      |> chunkByLWT
      |> List.map (Seq.unzip)
      |> List.iter commitChunk
      )

[<EntryPoint>]
let main _ =
  try
    main_impl ()
  with
  | e ->
      eprintfn "%s" (e.Message)
      exit 1

  //exit code
  0
