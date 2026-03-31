export type WebPageAction = {
  label: string;
  href: string;
  variant?: "primary" | "secondary";
};

export type BlankStatePagePart = {
  id: string;
  kind: "blank-state";
  eyebrow: string;
  title: string;
  description: string;
  actions: WebPageAction[];
};

export type WebPagePart = BlankStatePagePart;
