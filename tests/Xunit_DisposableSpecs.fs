module FSharp.Reactive.Tests.Xunit_DisposableSpecs

open Xunit
open FsCheck
open FSharp.Control.Reactive
open FSharp.Control.Reactive.Testing
open System.Reactive.Disposables
    
[<Fact>]
let ``CompositeDispose disposes correctly inner disposables`` () =    
        let mutlist = new ResizeArray<_>()
        let add n = fun () -> mutlist.Add n
        let contents () = mutlist |> List.ofSeq

        let compDisp = new CompositeDisposable()
        let disp1 = Disposable.create (add 1)
        let disp2 = Disposable.create (add 2)
        let disp3 = Disposable.create (add 3)

        disp1 |> Disposable.disposeWith compDisp
        disp2 |> Disposable.disposeWith compDisp
        disp3 |> Disposable.disposeWith compDisp

        compDisp.Dispose()
        
        let result = contents ()
        let expected = [1; 2; 3;]
        Assert.Equal<int list>(result, expected)