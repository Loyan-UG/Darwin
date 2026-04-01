import { SignInPage } from "@/components/account/sign-in-page";

export const metadata = {
  title: "Sign in",
};

type SignInRouteProps = {
  searchParams?: Promise<Record<string, string | string[] | undefined>>;
};

function readSearchParam(value: string | string[] | undefined) {
  return Array.isArray(value) ? value[0] : value;
}

export default async function SignInRoute({ searchParams }: SignInRouteProps) {
  const resolvedSearchParams = searchParams ? await searchParams : undefined;

  return (
    <SignInPage
      email={readSearchParam(resolvedSearchParams?.email)}
      signInError={readSearchParam(resolvedSearchParams?.signInError)}
      returnPath={readSearchParam(resolvedSearchParams?.returnPath)}
    />
  );
}
