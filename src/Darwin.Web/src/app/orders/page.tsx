import { PlaceholderPage } from "@/components/placeholders/placeholder-page";

export const metadata = {
  title: "Orders",
};

export default function OrdersPage() {
  return (
    <PlaceholderPage
      eyebrow="Member commerce"
      title="Order history and payment retry can build on the same storefront system."
      description="Member order history, detail, payment-intent retry, and order documents already exist in Darwin.WebApi. The next commerce slice can wire those contracts without reshaping the shell."
      bullets={[
        "Order history and detail are already member-scoped APIs.",
        "Hosted payment retry already reuses the storefront payment-intent path.",
        "Shipping and payment support now have real operator workflows in WebAdmin.",
      ]}
      primaryAction={{ label: "Back to Home", href: "/" }}
    />
  );
}
