import * as api from "$lib/cryptotrackerApi";

/**
 * An automatic integration counts as stale after three missed sync intervals
 * (with a 3h floor so short intervals don't flap). Manual integrations and
 * integrations that never synced are never stale.
 */
export function isStale(
	integration: api.IntegrationDto,
	intervalMinutes: number,
	now = Date.now()
): boolean {
	if (integration.isManual || !integration.lastSyncedAtUtc) return false;
	const ageMinutes = (now - new Date(integration.lastSyncedAtUtc).getTime()) / 60_000;
	return ageMinutes > Math.max(3 * intervalMinutes, 180);
}
