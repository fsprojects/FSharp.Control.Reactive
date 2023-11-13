/// The Reactive module provides operators for working with TestObserver<_> in F#.
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module FSharp.Control.Reactive.Testing.TestObserver

open System
open Microsoft.Reactive.Testing

/// Gets recorded timestamped notification messages received by the observer.
[<Obsolete("Use 'messages' to get all the recored timestamped notifications")>]
let all (o : ITestableObserver<'a>) =
    o.Messages |> Seq.toList |> TestNotifications

/// Gets recorded timestamped notification messages received by the observer.
let messages (o : ITestableObserver<'a>) =
    o.Messages |> Seq.toList |> TestNotifications

/// Gets recorded timestamped "OnNext" notification messages received by the observer.
let nexts (o : ITestableObserver<'a>) =
    o.Messages |> Seq.toList |> TestNotifications |> TestNotification.nexts

/// Gets recorded timestamped "OnError" notification messages received by the observer.
let errors (o : ITestableObserver<'a>) =
    o.Messages |> Seq.toList |> TestNotifications |> TestNotification.errors

