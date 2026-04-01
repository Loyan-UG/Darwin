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

export type HeroPagePart = {
  id: string;
  kind: "hero";
  eyebrow: string;
  title: string;
  description: string;
  actions: WebPageAction[];
  highlights: string[];
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

export type BlankStatePagePart = {
  id: string;
  kind: "blank-state";
  eyebrow: string;
  title: string;
  description: string;
  actions: WebPageAction[];
};

export type WebPagePart = HeroPagePart | CardGridPagePart | BlankStatePagePart;
