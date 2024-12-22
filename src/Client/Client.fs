module Client

open Elmish
open Fable.Remoting.Client
open Feliz
open Feliz.Bulma
open Shared
open Elmish.React
open Browser.Dom
open Fable.Core.JsInterop

let energyApi =
    Remoting.createApi ()
    |> Remoting.withBaseUrl "http://localhost:8080" 
    |> Remoting.withRouteBuilder (sprintf "/api/%s/%s")
    |> Remoting.buildProxy<IEnergyApi>

type Model = {
    EnergyData: EnergyData option
    Loading: bool
    Error: string option
}

type Msg =
    | LoadEnergyData
    | DataLoaded of EnergyData
    | LoadingError of string
    | RefreshData

let init() : Model * Cmd<Msg> =
    let model = {
        EnergyData = None
        Loading = false
        Error = None
    }

    model, Cmd.ofMsg LoadEnergyData

let update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
    match msg with
    | LoadEnergyData ->
        { model with Loading = true },
        Cmd.OfAsync.either
            energyApi.getLatestEnergyData
            ()
            DataLoaded
            (fun ex -> LoadingError ex.Message)

    | DataLoaded data ->
        {
            model with
                EnergyData = Some data
                Loading = false
        },
        Cmd.none

    | LoadingError error ->
        {
            model with
                Error = Some error
                Loading = false
        },
        Cmd.none

    | RefreshData -> model, Cmd.ofMsg LoadEnergyData

let renderPriceData(data: EnergyData) =
    let getLatestPriceForCountry countryCode =
        data.Prices
        |> Array.filter (fun p -> p.Area = countryCode)
        |> Array.tryHead
        |> Option.map (fun p -> $"€{p.Price}/MWh")
        |> Option.defaultValue "N/A"

    Bulma.box [
        Bulma.title.h3 [ title.is4; prop.text "Electricity Prices" ]
        Bulma.columns [
            Bulma.column [
                column.is3
                prop.children [
                    Bulma.title.h4 "Estonia (EE)"
                    Html.div [
                        Html.p "Latest Price: "
                        Html.strong (getLatestPriceForCountry "EE")
                    ]
                ]
            ]
            Bulma.column [
                column.is3
                prop.children [
                    Bulma.title.h4 "Finland (FI)"
                    Html.div [
                        Html.p "Latest Price: "
                        Html.strong (getLatestPriceForCountry "FI")
                    ]
                ]
            ]
            Bulma.column [
                column.is3
                prop.children [
                    Bulma.title.h4 "Latvia (LV)"
                    Html.div [
                        Html.p "Latest Price: "
                        Html.strong (getLatestPriceForCountry "LV")
                    ]
                ]
            ]
            Bulma.column [
                column.is3
                prop.children [
                    Bulma.title.h4 "Lithuania (LT)"
                    Html.div [
                        Html.p "Latest Price: "
                        Html.strong (getLatestPriceForCountry "LT")
                    ]
                ]
            ]
        ]
    ]

let renderGasData(data: EnergyData) =
    Bulma.box [
        Bulma.title.h3 [
            title.is4
            prop.text "Total domestic gas flow from transmission network (Last 24 Hours)"
        ]
        match data.GasFlow with
        | [||] -> Html.p "Gas flow data not available"
        | gasData ->
            Bulma.table [
                table.isFullWidth
                table.isHoverable
                table.isStriped
                prop.children [
                    Html.thead [
                        Html.tr [
                            Html.th "Timestamp (UTC)"
                            Html.th [
                                prop.style [ style.textAlign.right ]
                                prop.text "Flow Rate (m³/h)"
                            ]
                        ]
                    ]
                    Html.tbody [
                        for gasPoint in gasData do
                            Html.tr [
                                Html.td [
                                    prop.text (gasPoint.Timestamp.ToString("yyyy-MM-dd HH:mm"))
                                ]
                                Html.td [
                                    prop.style [ style.textAlign.right ]
                                    prop.text (sprintf "%.2f" gasPoint.Value)
                                ]
                            ]
                    ]
                ]
            ]

            // Summary statistics
            Bulma.level [
                Bulma.levelItem [
                    prop.children [
                        Html.div [
                            prop.style [ style.textAlign.center ]
                            prop.children [
                                Html.p [ prop.className "heading"; prop.text "Maximum Flow" ]
                                Html.p [
                                    prop.className "title"
                                    prop.text (
                                        sprintf
                                            "%.2f m³/h"
                                            (gasData |> Array.map (fun g -> g.Value) |> Array.max)
                                    )
                                ]
                            ]
                        ]
                    ]
                ]
                Bulma.levelItem [
                    prop.children [
                        Html.div [
                            prop.style [ style.textAlign.center ]
                            prop.children [
                                Html.p [ prop.className "heading"; prop.text "Minimum Flow" ]
                                Html.p [
                                    prop.className "title"
                                    prop.text (
                                        sprintf
                                            "%.2f m³/h"
                                            (gasData |> Array.map (fun g -> g.Value) |> Array.min)
                                    )
                                ]
                            ]
                        ]
                    ]
                ]
                Bulma.levelItem [
                    prop.children [
                        Html.div [
                            prop.style [ style.textAlign.center ]
                            prop.children [
                                Html.p [ prop.className "heading"; prop.text "Average Flow" ]
                                Html.p [
                                    prop.className "title"
                                    prop.text (
                                        sprintf
                                            "%.2f m³/h"
                                            (gasData |> Array.averageBy (fun g -> float g.Value))
                                    )
                                ]
                            ]
                        ]
                    ]
                ]
            ]
    ]

let renderCrossBorderData(data: EnergyData) =
    Bulma.box [
        Bulma.title.h3 [ title.is4; prop.text "Gas Cross-Border Flow (Last 24 Hours)" ]
        match data.CrossBorderFlow with
        | [||] -> Html.p "Cross-border flow data not available"
        | flows ->
            Bulma.table [
                table.isFullWidth
                table.isHoverable
                table.isStriped
                prop.children [
                    Html.thead [
                        Html.tr [
                            Html.th "Timestamp (UTC)"
                            Html.th "Location"
                            Html.th [
                                prop.style [ style.textAlign.right ]
                                prop.text "Volume (m³/h)"
                            ]
                            Html.th "Direction"
                            Html.th [
                                prop.style [ style.textAlign.right ]
                                prop.text "Pressure In (bar)"
                            ]
                            Html.th [
                                prop.style [ style.textAlign.right ]
                                prop.text "Pressure Out (bar)"
                            ]
                        ]
                    ]
                    Html.tbody [
                        for flow in flows do
                            Html.tr [
                                Html.td [ prop.text (flow.Timestamp.ToString("yyyy-MM-dd HH:mm")) ]
                                Html.td [ prop.text flow.Location ]
                                Html.td [
                                    prop.style [
                                        style.textAlign.right
                                        if flow.Direction = "export" then
                                            style.color.red
                                        else
                                            style.color.green
                                    ]
                                    prop.text (sprintf "%.2f" (abs flow.Volume))
                                ]
                                Html.td [
                                    prop.style [
                                        if flow.Direction = "export" then
                                            style.color.red
                                        else
                                            style.color.green
                                    ]
                                    prop.text (
                                        if flow.Direction = "export" then "Export ↑" else "Import ↓"
                                    )
                                ]
                                Html.td [
                                    prop.style [ style.textAlign.right ]
                                    prop.text (sprintf "%.3f" flow.PressureIn)
                                ]
                                Html.td [
                                    prop.style [ style.textAlign.right ]
                                    prop.text (sprintf "%.3f" flow.PressureOut)
                                ]
                            ]
                    ]
                ]
            ]
    ]

let renderEnergyData(data: EnergyData) =
    Html.div [
        prop.children [ renderPriceData data; renderCrossBorderData data; renderGasData data ]
    ]

let view (model: Model) (dispatch: Msg -> unit) =
    Bulma.hero [
        hero.isFullHeight
        prop.style [ style.backgroundImage "linear-gradient(to right, #000428, #004e92)" ]
        prop.children [
            Bulma.heroBody [
                Bulma.container [
                    Bulma.title [
                        title.is2
                        color.hasTextWhite
                        prop.text "Elering Energy Dashboard"
                    ]

                    match model.Loading, model.Error, model.EnergyData with
                    | true, _, _ -> Bulma.progress [ color.isInfo; prop.value 75; prop.max 100 ]
                    | _, Some error, _ -> Bulma.notification [ color.isDanger; prop.text error ]
                    | _, _, Some data -> renderEnergyData data
                    | _ -> Html.none

                    Bulma.button.a [
                        color.isInfo
                        prop.className "is-outlined"
                        color.isLight
                        prop.onClick (fun _ -> dispatch RefreshData)
                        prop.text "Refresh Data"
                    ]
                ]
            ]
        ]
    ]

Program.mkProgram init update view |> Program.withReactBatched "root" |> Program.run
