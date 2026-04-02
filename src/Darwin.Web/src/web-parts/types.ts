export type WebPageAction = {
  label: string;
  href: string;
  variant?: "primary" | "secondary";
};

export type WebPageCard = {
  id: string;
  eyebrow?: string;
  title: string;
  description: string;
  href: string;
  ctaLabel?: string;
  meta?: string;
};

export type WebPageLinkItem = {
  id: string;
  title: string;
  description: string;
  href: string;
  ctaLabel: string;
  meta?: string;
};

export type WebPageStatusItem = {
  id: string;
  label: string;
  title: string;
  description: string;
  href: string;
  ctaLabel: string;
  tone?: "ok" | "warning";
  meta?: string;
};

export type WebPageStageItem = {
  id: string;
  step: string;
  title: string;
  description: string;
  href: string;
  ctaLabel: string;
  meta?: string;
};

export type WebPagePairPanel = {
  id: string;
  eyebrow: string;
  title: string;
  description: string;
  href: string;
  ctaLabel: string;
  meta?: string;
};

export type WebPageAgendaColumn = {
  id: string;
  label: string;
  title: string;
  description: string;
  href: string;
  ctaLabel: string;
  bullets: string[];
  meta?: string;
};

export type WebPageRouteMapItem = {
  id: string;
  label: string;
  title: string;
  description: string;
  primaryHref: string;
  primaryCtaLabel: string;
  secondaryHref?: string;
  secondaryCtaLabel?: string;
  meta?: string;
};

export type WebPageMetric = {
  id: string;
  label: string;
  value: string;
  note: string;
};

export type HeroPagePart = {
  id: string;
  kind: "hero";
  eyebrow: string;
  title: string;
  description: string;
  actions: WebPageAction[];
  highlights: string[];
  panelTitle?: string;
};

export type CardGridPagePart = {
  id: string;
  kind: "card-grid";
  eyebrow: string;
  title: string;
  description: string;
  cards: WebPageCard[];
  emptyMessage: string;
};

export type LinkListPagePart = {
  id: string;
  kind: "link-list";
  eyebrow: string;
  title: string;
  description: string;
  items: WebPageLinkItem[];
  emptyMessage: string;
};

export type StatGridPagePart = {
  id: string;
  kind: "stat-grid";
  eyebrow: string;
  title: string;
  description: string;
  metrics: WebPageMetric[];
};

export type StatusListPagePart = {
  id: string;
  kind: "status-list";
  eyebrow: string;
  title: string;
  description: string;
  items: WebPageStatusItem[];
  emptyMessage: string;
};

export type StageFlowPagePart = {
  id: string;
  kind: "stage-flow";
  eyebrow: string;
  title: string;
  description: string;
  items: WebPageStageItem[];
  emptyMessage: string;
};

export type PairPanelPagePart = {
  id: string;
  kind: "pair-panel";
  eyebrow: string;
  title: string;
  description: string;
  leading: WebPagePairPanel;
  trailing: WebPagePairPanel;
};

export type AgendaColumnsPagePart = {
  id: string;
  kind: "agenda-columns";
  eyebrow: string;
  title: string;
  description: string;
  columns: WebPageAgendaColumn[];
  emptyMessage: string;
};

export type RouteMapPagePart = {
  id: string;
  kind: "route-map";
  eyebrow: string;
  title: string;
  description: string;
  items: WebPageRouteMapItem[];
  emptyMessage: string;
};

export type BlankStatePagePart = {
  id: string;
  kind: "blank-state";
  eyebrow: string;
  title: string;
  description: string;
  actions: WebPageAction[];
};

export type WebPagePart =
  | HeroPagePart
  | CardGridPagePart
  | LinkListPagePart
  | StatGridPagePart
  | StatusListPagePart
  | StageFlowPagePart
  | PairPanelPagePart
  | AgendaColumnsPagePart
  | RouteMapPagePart
  | BlankStatePagePart;
