/**
 * cryptotracker.webapi
 * 1.0
 * DO NOT MODIFY - This file has been generated using oazapfts.
 * See https://www.npmjs.com/package/oazapfts
 */
import * as Oazapfts from "@oazapfts/runtime";
import * as QS from "@oazapfts/runtime/query";
export const defaults: Oazapfts.Defaults<Oazapfts.CustomHeaders> = {
    headers: {},
    baseUrl: "/",
};
const oazapfts = Oazapfts.runtime(defaults);
export const servers = {};
export type Asset = {
    "symbol": string | null;
    externalId?: string | null;
    name?: string | null;
    image?: string | null;
    isFiat: boolean;
    isHidden: boolean;
};
export type AssetData = {
    asset: Asset;
    price: number;
};
export type Coin = {
    id?: string | null;
    "symbol"?: string | null;
    name?: string | null;
};
export type Fiat = {
    "symbol"?: string | null;
    name?: string | null;
};
export type AddAssetDto = {
    "symbol"?: string | null;
    isFiat?: boolean;
    externalId?: string | null;
};
export type AssetDto = {
    id: string | null;
    name?: string | null;
    image?: string | null;
    isHidden?: boolean;
};
export type IntegrationDto = {
    id: string;
    name: string | null;
    description?: string | null;
    isHidden?: boolean;
    isManual?: boolean;
};
export type IntegrationShit = {
    integration: IntegrationDto;
    amount: number;
};
export type AssetMeasuringDto = {
    asset: AssetDto;
    price: number;
    totalAmount: number;
    totalValue: number;
    integrationValues: IntegrationShit[] | null;
};
export type IntegrationDetails = {
    integration: IntegrationDto;
    measurings: AssetMeasuringDto[] | null;
};
export type AddIntegrationDto = {
    name?: string | null;
    description?: string | null;
};
export type AddMeasurementDto = {
    "symbol"?: string | null;
    date?: string;
    amount?: number;
};
export type AssetMeasurementDto = {
    id?: string;
    "symbol": string | null;
    integrationId: string;
    timestamp?: string;
    amount?: number;
};
export function getAssets(opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: Asset[];
    }>("/api/Asset/GetAssets", {
        ...opts
    });
}
export function getAsset($symbol: string, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: AssetData;
    }>(`/api/Asset/GetAsset${QS.query(QS.explode({
        "symbol": $symbol
    }))}`, {
        ...opts
    });
}
export function getCoins(opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: Coin[];
    }>("/api/Asset/GetCoins", {
        ...opts
    });
}
export function findCoinsBySymbol($symbol: string, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: Coin[];
    }>(`/api/Asset/FindCoinsBySymbol${QS.query(QS.explode({
        "symbol": $symbol
    }))}`, {
        ...opts
    });
}
export function getFiats(opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: Fiat[];
    }>("/api/Asset/GetFiats", {
        ...opts
    });
}
export function findFiatBySymbol($symbol: string, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: Fiat[];
    }>(`/api/Asset/FindFiatBySymbol${QS.query(QS.explode({
        "symbol": $symbol
    }))}`, {
        ...opts
    });
}
export function setExternalIdForSymbol($symbol: string, body?: string, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: AssetData;
    }>(`/api/Asset/SetExternalIdForSymbol${QS.query(QS.explode({
        "symbol": $symbol
    }))}`, oazapfts.json({
        ...opts,
        method: "POST",
        body
    }));
}
export function setVisibilityForSymbol($symbol: string, body?: boolean, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: boolean;
    }>(`/api/Asset/SetVisibilityForSymbol${QS.query(QS.explode({
        "symbol": $symbol
    }))}`, oazapfts.json({
        ...opts,
        method: "POST",
        body
    }));
}
export function setFiatForSymbol($symbol: string, body?: boolean, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: boolean;
    }>(`/api/Asset/SetFiatForSymbol${QS.query(QS.explode({
        "symbol": $symbol
    }))}`, oazapfts.json({
        ...opts,
        method: "POST",
        body
    }));
}
export function addAsset(addAssetDto?: AddAssetDto, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: boolean;
    }>("/api/Asset/AddAsset", oazapfts.json({
        ...opts,
        method: "POST",
        body: addAssetDto
    }));
}
export function resetAsset(body?: string, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: boolean;
    }>("/api/Asset/ResetAsset", oazapfts.json({
        ...opts,
        method: "POST",
        body
    }));
}
export function getIntegrationMeasuringByDay({ $symbol }: {
    $symbol?: string;
} = {}, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchText(`/api/CryptoTracker/GetIntegrationMeasuringByDay${QS.query(QS.explode({
        "symbol": $symbol
    }))}`, {
        ...opts
    });
}
export function getMeasuringsByDay(days: number, { $symbol }: {
    $symbol?: string;
} = {}, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: {
            [key: string]: AssetMeasuringDto[];
        };
    }>(`/api/CryptoTracker/GetMeasuringsByDay${QS.query(QS.explode({
        days,
        "symbol": $symbol
    }))}`, {
        ...opts
    });
}
export function getStandingsByDay(days: number, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: {
            [key: string]: number;
        };
    }>(`/api/CryptoTracker/GetStandingByDay${QS.query(QS.explode({
        days
    }))}`, {
        ...opts
    });
}
export function getLatestMeasurings(opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: AssetMeasuringDto[];
    }>("/api/CryptoTracker/GetLatestMeasurings", {
        ...opts
    });
}
export function getLatestStanding(opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: number;
    }>("/api/CryptoTracker/GetLatestStanding", {
        ...opts
    });
}
export function getIntegrations(opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: IntegrationDto[];
    }>("/api/Integration/GetIntegrations", {
        ...opts
    });
}
export function getIntegrationDetails(id: string, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: IntegrationDetails;
    }>(`/api/Integration/GetIntegrationDetails${QS.query(QS.explode({
        id
    }))}`, {
        ...opts
    });
}
export function addIntegration(addIntegrationDto?: AddIntegrationDto, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: boolean;
    }>("/api/Integration/AddIntegration", oazapfts.json({
        ...opts,
        method: "POST",
        body: addIntegrationDto
    }));
}
export function addIntegrationMeasurement(id: string, addMeasurementDto?: AddMeasurementDto, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: boolean;
    }>(`/api/Integration/AddIntegrationMeasurement${QS.query(QS.explode({
        id
    }))}`, oazapfts.json({
        ...opts,
        method: "POST",
        body: addMeasurementDto
    }));
}
export function getMeasuringsByIntegration(id: string, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: AssetMeasurementDto[];
    }>(`/api/Measuring/GetMeasuringsByIntegration${QS.query(QS.explode({
        id
    }))}`, {
        ...opts
    });
}
export function deleteMeasuringById(body?: string, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: boolean;
    }>("/api/Measuring/DeleteMeasuringById", oazapfts.json({
        ...opts,
        method: "POST",
        body
    }));
}
