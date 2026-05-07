type SurfaceSectionNavItem = {
  href: string;
  label: string;
};

type SurfaceSectionNavProps = {
  items: SurfaceSectionNavItem[];
};

export function SurfaceSectionNav({ items }: SurfaceSectionNavProps) {
  return (
    <div className="sticky top-24 z-10 -mt-2">
      <div className="overflow-x-auto rounded-[1rem] border border-[#dce6cf] bg-[color:color-mix(in_srgb,white_84%,#eff7e9_16%)] px-3 py-3 shadow-[0_24px_54px_-36px_rgba(58,92,35,0.32)] backdrop-blur">
        <div className="flex min-w-max flex-wrap gap-2">
          {items.map((item) => (
            <a
              key={item.href}
              href={item.href}
              className="inline-flex rounded-full border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
            >
              {item.label}
            </a>
          ))}
        </div>
      </div>
    </div>
  );
}
