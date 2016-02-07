open System

let program argv =
  let config = Config.Create(argv)
  let eraser = EraseFile.EraseFile(config)
  let paths = config.InputPaths
  
  let confirm () =
    if config.Force
    then true
    else
      printfn "Would you like erase these files? (NOT undoable) (Y/n)"
      paths |> List.iter (printfn "%s")
      Console.ReadLine() = "Y"

  let perform () =
    for path in paths do
      eraser.erase(path)
      |> Array.iter (Console.Error.WriteLine)

  if confirm ()
  then perform ()

[<EntryPoint>]
let main argv =
  program [|"-i"; "D:/trash/sub"; "C:/a/b"|]//argv
  0
