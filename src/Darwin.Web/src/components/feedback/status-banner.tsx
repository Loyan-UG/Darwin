type StatusBannerProps = {
  tone?: "warning" | "neutral";
  title: string;
  message: string;
};

export function StatusBanner({
  tone = "neutral",
  title,
  message,
}: StatusBannerProps) {
  const toneClasses =
    tone === "warning"
      ? "border-[rgba(217,111,50,0.22)] bg-[rgba(217,111,50,0.1)] text-[var(--color-text-primary)]"
      : "border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] text-[var(--color-text-secondary)]";

  return (
    <div className={`rounded-[1rem] border px-4 py-4 ${toneClasses}`}>
      <p className="text-sm font-semibold">{title}</p>
      <p className="mt-1 text-sm leading-7">{message}</p>
    </div>
  );
}
