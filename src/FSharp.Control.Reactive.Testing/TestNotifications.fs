namespace FSharp.Control.Reactive.Testing

open System.Reactive
open Microsoft.Reactive.Testing

/// Represents a recorded notification list to an observer.
type TestNotifications<'a> = TestNotifications of Recorded<Notification<'a>> list

/// The Reactive module provides operators for working with TestObserver<_> in F#.
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestNotification =     
    open System.Reactive
    open Microsoft.Reactive.Testing

    /// Factory function for an OnNext notification record at a given time with a given value.
    let onNext t (x : 'a) = ReactiveTest.OnNext (t, x)

    /// Factory function for an OnError notification record at a given time with a given error.
    let onError t (ex : exn) = ReactiveTest.OnError (t, ex)

    /// Factory function for an OnCompleted notification record at a given time.
    let onCompleted t = ReactiveTest.OnCompleted t

    /// Filters the OnNexts recorded notifiations of the specified list.
    let nexts (xs : Recorded<Notification<'a>> list) = 
        xs 
        |> List.filter (fun x -> x.Value.HasValue)
        |> List.map (fun x -> x.Value.Value)

    /// Maps the OnNext recorded notifications of the specified list to other values.
    let mapNexts f (TestNotifications ms : TestNotifications<'a>) = (nexts >> List.map f) ms

    /// Filters the OnError recorded notifications of the specified list.
    let errors (xs : Recorded<Notification<'a>> list) =
        xs
        |> List.filter (fun x -> x.Value.Kind = NotificationKind.OnError)
        |> List.map (fun x -> x.Value.Exception)

    /// Maps the OnError recorded notifications of the specified list to other values.
    let mapErrors f (TestNotifications ms : TestNotifications<'a>) = (errors >> List.map f) ms

open FsCheck

open TestNotification

type GenTestNotification =
    static member GenNotifications<'a> () =
        let nexts_errors = 
            Gen.frequency [ 
                (1, Gen.constant (fun t _ -> onError t (new System.Exception ())))
                (2, Gen.constant onNext) ] 
            |> Gen.listOf
            
        let emits =
            Gen.constant (fun h t -> h :: t)
            <*> Gen.constant (fun t _ -> onCompleted t)
            <*> nexts_errors
            |> Gen.map List.rev

        let realisticMs = 100L
        let growingNumbers l = 
            Arb.generate<int64 * 'a>
            |> Gen.map (fun (x, y) -> (abs x) + realisticMs, y)
            |> Gen.listOfLength l
            |> Gen.map (List.sortBy fst)

        let zipGrowingNumbers es = 
            growingNumbers (List.length es)
            |> Gen.map (List.zip es)
            
        emits 
        >>= zipGrowingNumbers
        |> Gen.map (List.map (fun (f, (x, y)) -> f x y) >> TestNotifications)
        |> Arb.fromGen

