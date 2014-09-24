namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("FSharp.Control.Reactive")>]
[<assembly: AssemblyProductAttribute("FSharp.Control.Reactive")>]
[<assembly: AssemblyDescriptionAttribute("A F#-friendly wrapper for the Reactive Extensions.")>]
[<assembly: AssemblyVersionAttribute("3.0.0")>]
[<assembly: AssemblyFileVersionAttribute("3.0.0")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "3.0.0"
