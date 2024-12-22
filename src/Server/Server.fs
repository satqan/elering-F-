module Server

open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open Saturn
open Shared
open System
open System.Net.Http
open System.Text.Json

module Storage =
    let mutable cachedData: EnergyData option = None
    let mutable lastFetchTime: DateTime option = None
    let CACHE_DURATION = TimeSpan.FromMinutes(15.0)

    let httpClient = new HttpClient()
    let ELERING_BASE_URL = "https://dashboard.elering.ee/api"

    let formatDateString(date: DateTime) = date.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")

    let getDateRangeParams() =
        let endTime = DateTime.UtcNow
        let startTime = endTime.AddHours(-24.0)

        $"?start={formatDateString startTime}&end={formatDateString endTime}"

    let convertPrices countryCode (prices: EleringPrice[]) =
        prices
        |> Array.map (fun p -> {
            Timestamp = DateTimeOffset.FromUnixTimeSeconds(p.timestamp).UtcDateTime
            Price = p.price
            Area = countryCode
        })

    let fetchGasData() = async {
        try
            let url = $"{ELERING_BASE_URL}/gas-system{getDateRangeParams ()}"
            printfn "Fetching gas data from URL: %s" url

            let! response = httpClient.GetStringAsync(url) |> Async.AwaitTask
            let gasData = JsonSerializer.Deserialize<EleringGasResponse>(response)

            let gasFlow =
                gasData.data
                |> Array.map (fun g -> {
                    Timestamp = DateTimeOffset.FromUnixTimeSeconds(g.timestamp).UtcDateTime
                    Value = g.value
                })
                |> Array.sortByDescending (fun g -> g.Timestamp)

            return Some gasFlow
        with ex ->
            eprintfn $"Error fetching gas data: {ex.Message}"
            return None
    }

    let fetchPriceData() = async {
        try
            let url = $"{ELERING_BASE_URL}/nps/price{getDateRangeParams ()}"
            let! response = httpClient.GetStringAsync(url) |> Async.AwaitTask
            let priceData = JsonSerializer.Deserialize<EleringPriceResponse>(response)

            let prices =
                [|
                    yield! convertPrices "EE" priceData.data.ee
                    yield! convertPrices "FI" priceData.data.fi
                    yield! convertPrices "LV" priceData.data.lv
                    yield! convertPrices "LT" priceData.data.lt
                |]
                |> Array.sortByDescending (fun p -> p.Timestamp)

            return Some prices
        with ex ->
            eprintfn $"Error fetching price data: {ex.Message}"
            return None
    }

    let fetchCrossBorderData() = async {
        try
            let url = $"{ELERING_BASE_URL}/gas-transmission/cross-border{getDateRangeParams ()}"
            printfn "Fetching cross-border data from URL: %s" url

            let! response = httpClient.GetStringAsync(url) |> Async.AwaitTask
            let crossBorderData = JsonSerializer.Deserialize<CrossBorderResponse>(response)

            let convertPoints location (points: CrossBorderPoint[]) =
                points
                |> Array.map (fun p -> {
                    Timestamp = DateTimeOffset.FromUnixTimeSeconds(p.timestamp).UtcDateTime
                    Location = location
                    Volume = p.volume
                    Direction = p.direction
                    PressureIn = p.pressure_in
                    PressureOut = p.pressure_out
                })

            let crossBorderFlows =
                [|
                    yield! convertPoints "Balticconnector" crossBorderData.data.bc
                    yield! convertPoints "Karksi" crossBorderData.data.karksi
                    yield! convertPoints "Misso" crossBorderData.data.misso
                    yield! convertPoints "Narva" crossBorderData.data.narva
                    yield! convertPoints "Värska" crossBorderData.data.varska
                |]
                |> Array.filter (fun p -> p.Volume <> 0m) 
                |> Array.sortByDescending (fun p -> p.Timestamp)

            return Some crossBorderFlows
        with ex ->
            eprintfn $"Error fetching cross-border data: {ex.Message}"
            return None
    }

    let fetchAllData() = async {
        let! pricesResult = fetchPriceData ()
        let! gasResult = fetchGasData ()
        let! crossBorderResult = fetchCrossBorderData ()

        return
            match pricesResult, gasResult, crossBorderResult with
            | Some prices, Some gas, Some crossBorder ->
                Some {
                    Prices = prices
                    GasFlow = gas
                    CrossBorderFlow = crossBorder
                }
            | _ -> None
    }

    let getLatestData() = async {
        match lastFetchTime with
        | Some time when DateTime.UtcNow - time < CACHE_DURATION -> return cachedData
        | _ ->
            let! newData = fetchAllData ()

            match newData with
            | Some data ->
                cachedData <- Some data
                lastFetchTime <- Some DateTime.UtcNow
                return Some data
            | None -> return cachedData
    }

let energyApi = {
    getLatestEnergyData =
        fun () -> async {
            let! data = Storage.getLatestData ()

            return
                Option.defaultValue
                    {
                        Prices = [||]
                        GasFlow = [||]
                        CrossBorderFlow = [||]
                    }
                    data
        }
    getCachedData = fun () -> async { return Storage.cachedData }
    updateCache =
        fun data -> async {
            Storage.cachedData <- Some data
            Storage.lastFetchTime <- Some DateTime.UtcNow
        }
}

let webApp =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder (sprintf "/api/%s/%s")
    |> Remoting.fromValue energyApi
    |> Remoting.buildHttpHandler

let app = application {
    url "http://0.0.0.0:8080"
    use_router webApp
    memory_cache

    use_cors
        "anything"
        (fun builder -> builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod() |> ignore)

    use_static "public"
    use_gzip
}

run app
