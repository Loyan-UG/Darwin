import { RegisterPage } from "@/components/account/register-page";

export const metadata = {
  title: "Register",
};

type RegisterRouteProps = {
  searchParams?: Promise<Record<string, string | string[] | undefined>>;
};

function readSearchParam(
  value: string | string[] | undefined,
) {
  return Array.isArray(value) ? value[0] : value;
}

export default async function RegisterRoute({ searchParams }: RegisterRouteProps) {
  const resolvedSearchParams = searchParams ? await searchParams : undefined;

  return (
    <RegisterPage
      email={readSearchParam(resolvedSearchParams?.email)}
      registerStatus={readSearchParam(resolvedSearchParams?.registerStatus)}
      registerError={readSearchParam(resolvedSearchParams?.registerError)}
    />
  );
}
