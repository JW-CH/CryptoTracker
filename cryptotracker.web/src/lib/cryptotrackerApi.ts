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
    externalId: string | null;
    name: string | null;
    image: string | null;
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
export type AssetMeasuringDto = {
    assetId?: string | null;
    assetName?: string | null;
    assetAmount?: number;
    assetPrice?: number;
    fiatValue?: number;
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
    }>(`/api/Asset/GetAsset/${encodeURIComponent($symbol)}`, {
        ...opts
    });
}
export function getPossibleAssets($symbol: string, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: Coin[];
    }>(`/api/Asset/GetPossibleAssets/${encodeURIComponent($symbol)}`, {
        ...opts
    });
}
export function setAssetForSymbol($symbol: string, body?: string, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: boolean;
    }>(`/api/Asset/SetAssetForSymbol/${encodeURIComponent($symbol)}`, oazapfts.json({
        ...opts,
        method: "POST",
        body
    }));
}
export function getMeasuringsByDay(days: number, { $symbol }: {
    $symbol?: string;
} = {}, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: {
            [key: string]: AssetMeasuringDto[];
        };
    }>(`/api/CryptoTracker/GetMeasuringsByDay/${encodeURIComponent(days)}${QS.query(QS.explode({
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
    }>(`/api/CryptoTracker/GetStandingByDay/${encodeURIComponent(days)}`, {
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
