/**
 * cryptotracker.webapi
 * 1.0
 * DO NOT MODIFY - This file has been generated using oazapfts.
 * See https://www.npmjs.com/package/oazapfts
 */
import * as Oazapfts from "@oazapfts/runtime";
export const defaults: Oazapfts.Defaults<Oazapfts.CustomHeaders> = {
    headers: {},
    baseUrl: "/",
};
const oazapfts = Oazapfts.runtime(defaults);
export const servers = {};
export type AssetMeasuringDto = {
    assetId?: string | null;
    assetName?: string | null;
    standingValue?: number;
};
export function getMeasuringsByDay(days: number, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: {
            [key: string]: AssetMeasuringDto[];
        };
    }>(`/api/CryptoTracker/${encodeURIComponent(days)}`, {
        ...opts
    });
}
export function getLatestMeasurings(opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: AssetMeasuringDto[];
    }>("/api/CryptoTracker", {
        ...opts
    });
}
