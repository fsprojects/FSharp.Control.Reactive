namespace FSharp.Control.Reactive.Testing

open System.Reactive
open Microsoft.Reactive.Testing

/// Represents a recorded notification list to an observer.
type TestNotification<'a> = Recorded<Notification<'a>>
type TestNotifications<'a> = TestNotifications of TestNotification<'a> list

/// The Reactive module provides operators for working with TestObserver<_> in F#.
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestNotification =     
    open System

    /// Active pattern on the different kind of 'TestNotification''s (a.k.a. 'Recorded<Notification<'a>>').
    let (|Next|Error|Completed|) (x : TestNotification<'a>) =
        match x.Value.Kind with
        | NotificationKind.OnNext -> Next x.Value.Value
        | NotificationKind.OnError -> Error x.Value.Exception
        | NotificationKind.OnCompleted -> Completed
        | _ -> Error (new ArgumentOutOfRangeException ("NotificationKind") :> exn)

    /// Factory function for an OnNext notification record at a given time with a given value.
    let onNext t (x : 'a) = ReactiveTest.OnNext (t, x)

    /// Factory function for an OnError notification record at a given time with a given error.
    let onError t (ex : exn) = ReactiveTest.OnError (t, ex)

    /// Factory function for an OnCompleted notification record at a given time.
    let onCompleted t = ReactiveTest.OnCompleted t

    /// Filters the OnNexts recorded notifiations of the specified list.
    let nexts (TestNotifications xs) = 
        List.choose (function | Next x -> Some x | _ -> None) xs

    /// Maps the OnNext recorded notifications of the specified list to other values.
    let mapNexts f = (nexts >> List.map f)

    /// Filters the OnError recorded notifications of the specified list.
    let errors (TestNotifications xs) =
        List.choose (function | Error ex -> Some ex | _ -> None) xs

    /// Maps the OnError recorded notifications of the specified list to other values.
    let mapErrors f = (errors >> List.map f)

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

