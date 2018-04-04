#I "../../bin/v4.5"

#r "FSharp.Control.Reactive"
#r "System.Reactive.Linq"
#r "System.Reactive.Interfaces"

open System
open System.Windows.Forms
open FSharp.Control.Reactive

let form = new Form ()

(** 
# Introduction to Observables

## Prerequisits

This page contains some basic examples of how to use `Observable`'s. 
This is by no means a complete stable example and should only be used for demonstration purposes.

## Subscribing on Observable

The following example show how the Enabling/Disabling of a button can be controled by looking at the input of another input field (TextBox).

![Enable Disable Button](../files/img/enable-disable.gif)

*)

let btn = new Button()
let txb = new TextBox()

txb.TextChanged
|> Observable.startWith [EventArgs.Empty]
|> Observable.subscribe (fun _ -> btn.Enabled <- not (String.IsNullOrEmpty txb.Text))

btn.Text <- "OK"
btn.Top <- 20

form.Controls.Add txb
form.Controls.Add btn
form.Show ()

(** 
## Transforming Observable

Incoming emits can be transformed using the mapping functionality.
Following example show how the input of a textbox can be transformed into the reversed value.

![Reversed](../files/img/reversed.gif)
*)

module String = 
    let rev (s : string) = new String (s.ToCharArray () |> Array.rev)

let input = new TextBox()
let reversed = new Label()

input.TextChanged
|> Observable.map (fun _ -> input.Text |> String.rev)
|> Observable.subscribe (fun x -> reversed.Text <- x)

reversed.Top <- 20

form.Controls.Add input
form.Controls.Add reversed
form.Show ()

(**
## Merging Two emits into one
Merging multiple events into one can be done with the `Observable.merge` primitive.
Following example shows how we change the value of a label with the buttons ("Red" and "Green").

![Red-Green](../files/img/red-green.gif)

*)
let redBtn = new Button()
let greenBtn = new Button()
let result = new Label()

let red = redBtn.Click |> Observable.mapTo "Red"
let green = greenBtn.Click |> Observable.mapTo "Green"

Observable.merge red green
|> Observable.subscribe (fun x -> result.Text <- x)

redBtn.Text <- "Red"
greenBtn.Text <- "Green"
greenBtn.Top <- 20
result.Left <- 100

form.Controls.Add redBtn
form.Controls.Add greenBtn
form.Controls.Add result
form.Show ()