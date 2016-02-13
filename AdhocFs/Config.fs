module Config

open System

type Config =
  {
    Threshold   : TimeSpan
    Timeout     : TimeSpan
  }

let config =
  {
    Threshold   = TimeSpan(0, 10, 0) // 10 min
    Timeout     = TimeSpan(0, 0, 10) // 10 sec
  }
