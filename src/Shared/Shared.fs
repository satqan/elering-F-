namespace Shared

open System

type EleringPrice = { price: decimal; timestamp: int64 }

type EleringPriceData = {
    ee: EleringPrice[]
    fi: EleringPrice[]
    lt: EleringPrice[]
    lv: EleringPrice[]
}

type EleringGasData = { timestamp: int64; value: decimal }

type EleringGasResponse = { data: EleringGasData[]; success: bool }

type EleringPriceResponse = { data: EleringPriceData; success: bool }

type EnergyPrice = {
    Timestamp: DateTime
    Price: decimal
    Area: string
}

type GasFlow = { Timestamp: DateTime; Value: decimal }

type CrossBorderPoint = {
    timestamp: int64
    volume: decimal
    direction: string
    pressure_in: decimal
    pressure_out: decimal
    wobbe_index: decimal
    gross_calorific_value: decimal
}

type CrossBorderData = {
    bc: CrossBorderPoint[]
    karksi: CrossBorderPoint[]
    misso: CrossBorderPoint[]
    narva: CrossBorderPoint[]
    varska: CrossBorderPoint[]
}

type CrossBorderResponse = { data: CrossBorderData; success: bool }

type GasCrossBorderFlow = {
    Timestamp: DateTime
    Location: string
    Volume: decimal
    Direction: string
    PressureIn: decimal
    PressureOut: decimal
}

type EnergyData = {
    Prices: EnergyPrice[]
    GasFlow: GasFlow[]
    CrossBorderFlow: GasCrossBorderFlow[]
}

type IEnergyApi = {
    getLatestEnergyData: unit -> Async<EnergyData>
    getCachedData: unit -> Async<EnergyData option>
    updateCache: EnergyData -> Async<unit>
}
