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
export type AssetType = "Fiat" | "Crypto" | "Stock" | "ETF" | "Commodity" | "RealEstate";
export type Asset = {
    "symbol": string | null;
    externalId?: string | null;
    name?: string | null;
    image?: string | null;
    assetType: AssetType;
    isHidden: boolean;
};
export type AddAssetDto = {
    "symbol"?: string | null;
    assetType?: AssetType;
    externalId?: string | null;
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
export type MeResponse = {
    userName?: string | null;
    email?: string | null;
    displayName?: string | null;
};
export type LoginRequest = {
    username?: string | null;
    password?: string | null;
};
export type RegisterRequest = {
    username?: string | null;
    email?: string | null;
    password?: string | null;
};
export type AssetDto = {
    "symbol": string | null;
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
export type MessungDto = {
    asset: AssetDto;
    price: number;
    totalAmount: number;
    totalValue: number;
    integrationValues: IntegrationShit[] | null;
};
export type AddIntegrationDto = {
    name?: string | null;
    description?: string | null;
};
export type IntegrationDetails = {
    integration: IntegrationDto;
    measurings: MessungDto[] | null;
};
export type AssetMeasuringDto = {
    id?: string;
    "symbol": string | null;
    integrationId: string;
    timestamp?: string;
    amount?: number;
};
export type AddMeasuringDto = {
    "symbol"?: string | null;
    date?: string;
    amount?: number;
};
export function getAssets(opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: Asset[];
    }>("/api/Asset", {
        ...opts
    });
}
export function addAsset(addAssetDto?: AddAssetDto, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: boolean;
    }>("/api/Asset", oazapfts.json({
        ...opts,
        method: "POST",
        body: addAssetDto
    }));
}
export function getAsset($symbol: string, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: AssetData;
    }>(`/api/Asset/${encodeURIComponent($symbol)}`, {
        ...opts
    });
}
export function deleteAsset($symbol: string, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: boolean;
    }>(`/api/Asset/${encodeURIComponent($symbol)}`, {
        ...opts,
        method: "DELETE"
    });
}
export function getCoins(opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: Coin[];
    }>("/api/Asset/coin", {
        ...opts
    });
}
export function findCoinsBySymbol($symbol: string, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: Coin[];
    }>(`/api/Asset/${encodeURIComponent($symbol)}/coin`, {
        ...opts
    });
}
export function getFiats(opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: Fiat[];
    }>("/api/Asset/fiat", {
        ...opts
    });
}
export function findFiatBySymbol($symbol: string, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: Fiat[];
    }>(`/api/Asset/${encodeURIComponent($symbol)}/fiat`, {
        ...opts
    });
}
export function setExternalIdForSymbol($symbol: string, body?: string, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: AssetData;
    }>(`/api/Asset/${encodeURIComponent($symbol)}/ExternalId`, oazapfts.json({
        ...opts,
        method: "POST",
        body
    }));
}
export function setVisibilityForSymbol($symbol: string, body?: boolean, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: boolean;
    }>(`/api/Asset/${encodeURIComponent($symbol)}/Visibility`, oazapfts.json({
        ...opts,
        method: "POST",
        body
    }));
}
export function setAssetTypeForSymbol($symbol: string, assetType?: AssetType, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: boolean;
    }>(`/api/Asset/${encodeURIComponent($symbol)}/AssetType`, oazapfts.json({
        ...opts,
        method: "POST",
        body: assetType
    }));
}
export function resetAsset(body?: string, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: boolean;
    }>("/api/Asset/Reset", oazapfts.json({
        ...opts,
        method: "POST",
        body
    }));
}
export function getMe(opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: MeResponse;
    }>("/api/Auth/me", {
        ...opts
    });
}
export function oidcLogin({ returnUrl }: {
    returnUrl?: string;
} = {}, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchText(`/api/Auth/oidc-login${QS.query(QS.explode({
        returnUrl
    }))}`, {
        ...opts
    });
}
export function login(loginRequest?: LoginRequest, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchText("/api/Auth/login", oazapfts.json({
        ...opts,
        method: "POST",
        body: loginRequest
    }));
}
export function register(registerRequest?: RegisterRequest, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchText("/api/Auth/register", oazapfts.json({
        ...opts,
        method: "POST",
        body: registerRequest
    }));
}
export function logout(opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchText("/api/Auth/logout", {
        ...opts,
        method: "POST"
    });
}
export function getMeasuringsByDate(date: string, { $symbol }: {
    $symbol?: string;
} = {}, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: MessungDto[];
    }>(`/api/CryptoTracker/measuring/date/${encodeURIComponent(date)}${QS.query(QS.explode({
        "symbol": $symbol
    }))}`, {
        ...opts
    });
}
export function getMeasuringsByDays(days: number, { $symbol }: {
    $symbol?: string;
} = {}, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: {
            [key: string]: MessungDto[];
        };
    }>(`/api/CryptoTracker/measuring/days/${encodeURIComponent(days)}${QS.query(QS.explode({
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
    }>(`/api/CryptoTracker/standing/days/${encodeURIComponent(days)}`, {
        ...opts
    });
}
export function getLatestMeasurings(opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: MessungDto[];
    }>("/api/CryptoTracker/measuring", {
        ...opts
    });
}
export function getLatestStanding(opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: number;
    }>("/api/CryptoTracker/standing", {
        ...opts
    });
}
export function getIntegrations(opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: IntegrationDto[];
    }>("/api/Integration", {
        ...opts
    });
}
export function addIntegration(addIntegrationDto?: AddIntegrationDto, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: boolean;
    }>("/api/Integration", oazapfts.json({
        ...opts,
        method: "POST",
        body: addIntegrationDto
    }));
}
export function getIntegrationDetails(id: string, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: IntegrationDetails;
    }>(`/api/Integration/${encodeURIComponent(id)}/detail`, {
        ...opts
    });
}
export function getMeasuringsByIntegration(id: string, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: AssetMeasuringDto[];
    }>(`/api/Measuring/${encodeURIComponent(id)}`, {
        ...opts
    });
}
export function deleteMeasuringById(id: string, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: boolean;
    }>(`/api/Measuring/${encodeURIComponent(id)}`, {
        ...opts,
        method: "DELETE"
    });
}
export function addIntegrationMeasuring(id: string, addMeasuringDto?: AddMeasuringDto, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: boolean;
    }>(`/api/Measuring${QS.query(QS.explode({
        id
    }))}`, oazapfts.json({
        ...opts,
        method: "POST",
        body: addMeasuringDto
    }));
}
