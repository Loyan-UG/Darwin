import { PlaceholderPage } from "@/components/placeholders/placeholder-page";

export const metadata = {
  title: "Account",
};

export default function AccountPage() {
  return (
    <PlaceholderPage
      eyebrow="Auth and self-service"
      title="Account self-service will sit behind the same shell and route system."
      description="Profile, preferences, addresses, activation, and reset flows already exist in Darwin.WebApi. Browser-session strategy remains an explicit dependency before full member UX lands."
      bullets={[
        "JWT-first auth endpoints already exist for login, register, confirmation, and reset.",
        "Profile, preferences, and address-book contracts are already member-scoped.",
        "Communication flows must keep using the current backend activation/reset lifecycle.",
      ]}
      primaryAction={{ label: "Back to Home", href: "/" }}
    />
  );
}
