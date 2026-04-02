export function parseUtcTimestamp(value: string | null | undefined) {
  if (typeof value !== "string" || value.trim().length === 0) {
    return null;
  }

  const parsed = new Date(value).getTime();
  return Number.isFinite(parsed) ? parsed : null;
}

export function isValidUtcTimestamp(value: string | null | undefined) {
  return parseUtcTimestamp(value) !== null;
}

export function isExpiredUtcTimestamp(
  value: string | null | undefined,
  now = Date.now(),
) {
  const parsed = parseUtcTimestamp(value);
  return parsed === null || parsed <= now;
}
