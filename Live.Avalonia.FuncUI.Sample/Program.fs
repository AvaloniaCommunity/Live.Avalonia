module Live.Avalonia.FuncUI.Sample.Program

open System
open System.Text.Json
open System.Text.Json.Serialization
open Elmish
open Avalonia
open Avalonia.Controls
open Avalonia.Controls.ApplicationLifetimes
open Live.Avalonia
open Avalonia.FuncUI
open Avalonia.FuncUI.Elmish
open Avalonia.FuncUI.Components.Hosts

let transferState<'t> oldState =
    let jsonOptions = JsonSerializerOptions()
    jsonOptions.Converters.Add(JsonFSharpConverter())
    
    try
        let json = JsonSerializer.Serialize(oldState, jsonOptions)
        let state = JsonSerializer.Deserialize<'t>(json, jsonOptions)        
        match box state with
        | null -> None
        | _ -> Some state
    with ex ->
        Console.Write $"Error restoring state: {ex}"
        None
    
type MainControl(window: Window) as this =
    inherit HostControl()
    do
        // Instead of just creating default init state, try to recover state from window.DataContext
        let hotInit () = 
            match transferState<Counter.State> window.DataContext with
            | Some newState ->
                Console.WriteLine $"Restored state %O{newState}"
                newState
            | None -> Counter.init
        
        Elmish.Program.mkSimple hotInit Counter.update Counter.view
        |> Program.withHost this
        // Every time state changes, save state to window.DataContext
        |> Program.withTrace (fun _ state -> window.DataContext <- state)
        |> Program.run

        
type App() =
    inherit Application()
    
    interface ILiveView with
        member _.CreateView(window: Window) =
            MainControl(window) :> obj

    override this.Initialize() =
        this.Styles.Load "avares://Avalonia.Themes.Default/DefaultTheme.xaml"
        this.Styles.Load "avares://Avalonia.Themes.Default/Accents/BaseDark.xaml"

    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktopLifetime ->
            let window = new LiveViewHost(this, fun msg -> printfn $"%s{msg}")
            window.StartWatchingSourceFilesForHotReloading()
            window.Show()
            base.OnFrameworkInitializationCompleted()
        | _ -> ()

[<EntryPoint>]
let main (args: string array) =
    AppBuilder
        .Configure<App>()
        .UsePlatformDetect()
        .UseSkia()
        .StartWithClassicDesktopLifetime(args)