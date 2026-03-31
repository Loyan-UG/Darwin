import { PlaceholderPage } from "@/components/placeholders/placeholder-page";

export const metadata = {
  title: "Invoices",
};

export default function InvoicesPage() {
  return (
    <PlaceholderPage
      eyebrow="Invoices and billing"
      title="Invoice self-service stays aligned with billing and compliance realities."
      description="Invoice history, invoice detail, retry-payment, and document download already exist. Deeper compliance and archive behavior remain explicit dependencies and should not be faked in the web layer."
      bullets={[
        "Invoice routes and action metadata are already member-scoped.",
        "UI should stay aware of B2B/B2C tax and invoice-readiness signals.",
        "Deeper e-invoice and compliance flows remain later-phase work.",
      ]}
      primaryAction={{ label: "Back to Home", href: "/" }}
    />
  );
}
