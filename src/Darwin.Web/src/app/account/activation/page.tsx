import { ActivationPage } from "@/components/account/activation-page";

export const metadata = {
  title: "Activation",
};

type ActivationRouteProps = {
  searchParams?: Promise<Record<string, string | string[] | undefined>>;
};

function readSearchParam(
  value: string | string[] | undefined,
) {
  return Array.isArray(value) ? value[0] : value;
}

export default async function ActivationRoute({
  searchParams,
}: ActivationRouteProps) {
  const resolvedSearchParams = searchParams ? await searchParams : undefined;

  return (
    <ActivationPage
      email={readSearchParam(resolvedSearchParams?.email)}
      token={readSearchParam(resolvedSearchParams?.token)}
      activationStatus={readSearchParam(resolvedSearchParams?.activationStatus)}
      activationError={readSearchParam(resolvedSearchParams?.activationError)}
    />
  );
}
