import { PasswordPage } from "@/components/account/password-page";

export const metadata = {
  title: "Password recovery",
};

type PasswordRouteProps = {
  searchParams?: Promise<Record<string, string | string[] | undefined>>;
};

function readSearchParam(
  value: string | string[] | undefined,
) {
  return Array.isArray(value) ? value[0] : value;
}

export default async function PasswordRoute({ searchParams }: PasswordRouteProps) {
  const resolvedSearchParams = searchParams ? await searchParams : undefined;

  return (
    <PasswordPage
      email={readSearchParam(resolvedSearchParams?.email)}
      token={readSearchParam(resolvedSearchParams?.token)}
      passwordStatus={readSearchParam(resolvedSearchParams?.passwordStatus)}
      passwordError={readSearchParam(resolvedSearchParams?.passwordError)}
    />
  );
}
