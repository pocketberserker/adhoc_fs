﻿module EraseFile

open System
open System.IO
open System.Text
open System.Threading
open Basis.Core

[<AutoOpen>]
module Misc =
  let tap f x = f x; x

  let (|File|Dir|NotExist|) path =
    if   File.Exists(path)      then File (FileInfo(path))
    elif Directory.Exists(path) then Dir  (DirectoryInfo(path))
    else NotExist

module Random =
  let rng = Random()

  let nextDateTime () =
    DateTime.FromFileTimeUtc (rng.Next() |> int64)

    // Win32 File Time として有効でない値になることがある
    (*
    let ran = DateTime.MaxValue - DateTime.MinValue
    let ms = ran.TotalMilliseconds * rng.NextDouble()
    DateTime.MinValue.AddMilliseconds(ms)
    //*)

  let writeBytesToFile(fi: FileInfo) =
    let len = fi.Length
    use writer = fi.OpenWrite()

    // ファイルサイズは一般に巨大なので、ブロック単位で書き込む
    // 乱数列は使い回す
    let blockSize = min 0x1000L len
    let buf = Array.zeroCreate (blockSize |> int)
    rng.NextBytes(buf)

    let rec loop i =
      let curSize = (int64 i) * blockSize
      if curSize + blockSize >= len
      then
        writer.Write(buf, 0, (len - curSize) |> int)
      else
        writer.Write(buf, 0, buf.Length)
        loop (i + 1)
    loop 0

type EraseFile(config_: Config) =
  member private this.timeout() =
    Thread.Sleep(config_.Timeout)

  member private this.moveToTrash(fi: FileInfo) =
    let name = Path.GetRandomFileName()
    fi.MoveTo(Path.Combine(config_.TrashDir, name))

  member private this.randomizeInfo(fi: FileSystemInfo) =
    let t = Random.nextDateTime ()
    fi.CreationTimeUtc   <- t
    fi.LastAccessTimeUtc <- t
    fi.LastWriteTimeUtc  <- t

  member private this.randomizeContent(fi) =
    for i in 0..(config_.OverwriteTimes - 1) do
      Random.writeBytesToFile fi

  member this.eraseFile(fi: FileInfo) =
    assert (fi.Exists)
    try
      this.timeout()
      this.moveToTrash(fi)
      this.randomizeContent(fi)
      this.randomizeInfo(fi)
      fi.Delete()
      Success ()
    with
    | e -> Failure (sprintf "%s: %s" e.Message fi.FullName)

  // FileInfo 版と重複している
  // FileSystemInfo.MoveTo がないせい
  member private this.moveToTrash(di: DirectoryInfo) =
    let name = Path.GetRandomFileName()
    di.MoveTo(Path.Combine(config_.TrashDir, name))

  member private this.randomizeContent(di: DirectoryInfo) =
    let errors =
      di.GetFiles()
      |> Array.choose (fun fi ->
        match this.eraseFile fi with
        | Success () -> None
        | Failure e -> Some e
        )

    let errors' =
      di.GetDirectories()
      |> Array.collect (this.eraseDir)

    Array.append errors errors'

  member this.eraseDir(di: DirectoryInfo) =
    assert (di.Exists)
    try
      this.timeout()
      this.moveToTrash(di)
      let errors = this.randomizeContent(di)
      if errors |> Array.isEmpty
      then
        this.randomizeInfo(di)
        di.Delete()
        [||]
      else
        errors
    with
    | e -> [| e.Message |]

  member this.erase(path: string) =
    match path with
    | File fi ->
        match this.eraseFile(fi) with
        | Success () -> [||]
        | Failure e -> [|e|]
    | Dir di ->
        this.eraseDir(di)
    | NotExist ->
        [| "NotExist: " + path |]
