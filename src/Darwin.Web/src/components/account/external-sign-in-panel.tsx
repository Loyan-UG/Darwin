"use client";

import Script from "next/script";
import { useCallback, useEffect, useRef, useState } from "react";

type GoogleCredentialResponse = {
  credential?: string;
};

type GoogleAccounts = {
  id: {
    initialize(input: {
      client_id: string;
      callback: (response: GoogleCredentialResponse) => void;
      auto_select?: boolean;
      cancel_on_tap_outside?: boolean;
      use_fedcm_for_prompt?: boolean;
    }): void;
    renderButton(
      parent: HTMLElement,
      options: {
        theme: "outline" | "filled_blue" | "filled_black";
        size: "large" | "medium" | "small";
        shape: "pill" | "rectangular" | "circle" | "square";
        text: "signin_with" | "signup_with" | "continue_with" | "signin";
        width?: number;
      },
    ): void;
  };
};

declare global {
  interface Window {
    google?: {
      accounts?: GoogleAccounts;
    };
  }
}

type ExternalSignInPanelProps = {
  googleClientId: string | null;
  googleEnabled: boolean;
  microsoftEnabled: boolean;
  allowAccountCreation?: boolean;
  googleButtonText?: "signin_with" | "signup_with" | "continue_with" | "signin";
  returnPath?: string;
  labels: {
    title: string;
    divider: string;
    googleUnavailable: string;
    googleFailed: string;
    microsoftComingSoon: string;
  };
};

export function ExternalSignInPanel({
  googleClientId,
  googleEnabled,
  microsoftEnabled,
  allowAccountCreation = false,
  googleButtonText = "continue_with",
  returnPath,
  labels,
}: ExternalSignInPanelProps) {
  const googleButtonRef = useRef<HTMLDivElement | null>(null);
  const [scriptReady, setScriptReady] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleCredential = useCallback(
    async (response: GoogleCredentialResponse) => {
      if (!response.credential || isSubmitting) {
        setError(labels.googleFailed);
        return;
      }

      setIsSubmitting(true);
      setError(null);

      try {
        const loginResponse = await fetch("/account/external-login/google", {
          method: "POST",
          headers: {
            Accept: "application/json",
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            credential: response.credential,
            allowAccountCreation,
            returnPath: returnPath || "/account",
          }),
        });

        const result = (await loginResponse.json()) as {
          ok?: boolean;
          redirectTo?: string;
          message?: string;
        };

        if (!loginResponse.ok || !result.ok || !result.redirectTo) {
          setError(result.message || labels.googleFailed);
          return;
        }

        window.location.assign(result.redirectTo);
      } catch {
        setError(labels.googleFailed);
      } finally {
        setIsSubmitting(false);
      }
    },
    [allowAccountCreation, isSubmitting, labels.googleFailed, returnPath],
  );

  useEffect(() => {
    if (!googleEnabled || !googleClientId || !scriptReady || !googleButtonRef.current) {
      return;
    }

    const google = window.google?.accounts?.id;
    if (!google) {
      setError(labels.googleUnavailable);
      return;
    }

    googleButtonRef.current.replaceChildren();
    google.initialize({
      client_id: googleClientId,
      callback: handleCredential,
      auto_select: false,
      cancel_on_tap_outside: true,
      use_fedcm_for_prompt: true,
    });
    google.renderButton(googleButtonRef.current, {
      theme: "outline",
      size: "large",
      shape: "pill",
      text: googleButtonText,
      width: 260,
    });
  }, [
    googleClientId,
    googleEnabled,
    googleButtonText,
    handleCredential,
    labels.googleUnavailable,
    scriptReady,
  ]);

  return (
    <div className="mt-8 border-t border-[var(--color-border-soft)] pt-6">
      {googleEnabled && googleClientId ? (
        <Script
          src="https://accounts.google.com/gsi/client"
          strategy="afterInteractive"
          onLoad={() => setScriptReady(true)}
          onError={() => setError(labels.googleUnavailable)}
        />
      ) : null}

      <div className="flex items-center gap-3 text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-text-muted)]">
        <span className="h-px flex-1 bg-[var(--color-border-soft)]" />
        <span>{labels.divider}</span>
        <span className="h-px flex-1 bg-[var(--color-border-soft)]" />
      </div>

      <div className="mt-5 grid gap-3">
        <div className="min-h-11">
          {googleEnabled && googleClientId ? (
            <div
              ref={googleButtonRef}
              aria-label={labels.title}
              className={isSubmitting ? "pointer-events-none opacity-70" : undefined}
            />
          ) : (
            <button
              type="button"
              disabled
              className="inline-flex w-full cursor-not-allowed items-center justify-center rounded-full border border-[var(--color-border-soft)] px-4 py-3 text-sm font-semibold text-[var(--color-text-muted)] opacity-75"
            >
              {labels.googleUnavailable}
            </button>
          )}
        </div>

        <button
          type="button"
          disabled={!microsoftEnabled}
          className="inline-flex w-full cursor-not-allowed items-center justify-center rounded-full border border-[var(--color-border-soft)] px-4 py-3 text-sm font-semibold text-[var(--color-text-muted)] opacity-75"
        >
          {labels.microsoftComingSoon}
        </button>
      </div>

      {error ? (
        <p className="mt-3 text-sm font-medium text-[var(--color-accent)]">{error}</p>
      ) : null}
    </div>
  );
}
