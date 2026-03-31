import { PlaceholderPage } from "@/components/placeholders/placeholder-page";

export const metadata = {
  title: "Loyalty",
};

export default function LoyaltyPage() {
  return (
    <PlaceholderPage
      eyebrow="Member loyalty"
      title="Loyalty pages can now be built against real member contracts."
      description="Overview, dashboard, reward browsing, join, and history endpoints already exist. The web portal can adopt those canonical contracts without forking mobile-oriented loyalty concepts."
      bullets={[
        "Loyalty overview and business dashboard endpoints are already available.",
        "WebAdmin now has real reward-tier, campaign, account, and redemption support surfaces.",
        "This area still depends on the member auth/browser-session decision.",
      ]}
      primaryAction={{ label: "Back to Home", href: "/" }}
    />
  );
}
