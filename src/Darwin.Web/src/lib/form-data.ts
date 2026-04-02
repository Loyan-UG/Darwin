export function readTrimmedFormText(
  formData: FormData,
  key: string,
  maxLength?: number,
) {
  const value = formData.get(key);
  const trimmed = String(value ?? "").trim();

  return typeof maxLength === "number"
    ? trimmed.slice(0, Math.max(0, maxLength))
    : trimmed;
}

export function readNormalizedEmail(formData: FormData, key = "email") {
  return readTrimmedFormText(formData, key, 320).toLowerCase();
}
