namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("FSharp.Control.Reactive")>]
[<assembly: AssemblyProductAttribute("FSharp.Control.Reactive")>]
[<assembly: AssemblyDescriptionAttribute("A F#-friendly wrapper for the Reactive Extensions.")>]
[<assembly: AssemblyVersionAttribute("2.3.0")>]
[<assembly: AssemblyFileVersionAttribute("2.3.0")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "2.3.0"
