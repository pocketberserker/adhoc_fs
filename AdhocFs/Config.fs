[<AutoOpen>]
module Config

open System
open System.IO
open Argu
open Basis.Core

type CLIArguments =
  | [<AltCommandLine("-t")>]
    Timeout of int
  | [<AltCommandLine("-n")>]
    Overwrite_Times of int
  | [<AltCommandLine("-f")>]
    Force
  | [<AltCommandLine("-i"); Rest>]
    Input of string
with
  interface IArgParserTemplate with
    member this.Usage =
      match this with
      | Timeout           _ -> "set timeout before erasing."
      | Overwrite_Times   _ -> "process multiple times to destroy file content."
      | Force               -> "ignore any errors (by default). set default timeout to 0."
      | Input             _ -> "paths to what you want to erase."

let clParser =
  ArgumentParser.Create<CLIArguments>()

type Config = {
  OverwriteTimes    : int
  Timeout           : int
  Force             : bool
  InputPaths        : string list
}
with
  static member Default =
    {
      OverwriteTimes    = 3
      Timeout           = 1000
      Force             = false
      InputPaths        = []
    }

  member def.fromCommandLine(argv) =
    let results = clParser.ParseCommandLine(argv, raiseOnUsage = false)
    if results.IsUsageRequested then exit 0

    let is_forced = results.Contains <@ Force @>
    {
      OverwriteTimes =
        results.GetResult(<@ Overwrite_Times @>, def.OverwriteTimes)
      Timeout =
        results.GetResult(<@ Timeout @>
          , (if is_forced then 0 else def.Timeout)
          )
      Force = is_forced
      InputPaths =
        results.GetResults(<@ Input @>)
    }

  member this.fromReader(r: TextReader) =
    let input =
      r.ReadToEnd()
        .Split([|Environment.NewLine|], StringSplitOptions.RemoveEmptyEntries)
      |> Array.toList
    { this with InputPaths = input @ this.InputPaths }

  static member Create(argv) =
    let config = Config.Default.fromCommandLine(argv)
    if config.InputPaths |> List.isEmpty |> not
    then config
    else config.fromReader(Console.In)
