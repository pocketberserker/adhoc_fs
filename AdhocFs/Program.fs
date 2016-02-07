open System

let program paths =
  let eraser = EraseFile.EraseFile()
  for path in paths do
    eraser.erase(path)
    |> Array.iter (Console.Error.WriteLine)

[<EntryPoint>]
let main argv =

  program [| "file.txt" |]
  0
