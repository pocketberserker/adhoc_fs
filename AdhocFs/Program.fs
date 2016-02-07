open System

let program argv =
  let config = Config.Create(argv)
  let eraser = EraseFile.EraseFile(config)
  let paths = config.InputPaths
  for path in paths do
    eraser.erase(path)
    |> Array.iter (Console.Error.WriteLine)

[<EntryPoint>]
let main argv =
  program [|"-i"; "D:/trash/sub"; "C:/a/b"|]//argv
  0
