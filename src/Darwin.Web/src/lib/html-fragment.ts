function stripDangerousBlocks(value: string) {
  return value.replace(
    /<(script|iframe|object|embed|link|meta|base|form)\b[\s\S]*?<\/\1>/gi,
    "",
  );
}

function stripDangerousStandaloneTags(value: string) {
  return value.replace(/<(script|iframe|object|embed|link|meta|base|form)\b[^>]*\/?>/gi, "");
}

function stripInlineEventHandlers(value: string) {
  return value
    .replace(/\son[a-z]+\s*=\s*(['"]).*?\1/gi, "")
    .replace(/\son[a-z]+\s*=\s*[^\s>]+/gi, "");
}

function stripDangerousUrlAttributes(value: string) {
  return value
    .replace(
      /\s(href|src|xlink:href|formaction)\s*=\s*(['"])\s*(javascript:|vbscript:|data:\s*text\/html)[\s\S]*?\2/gi,
      "",
    )
    .replace(
      /\s(href|src|xlink:href|formaction)\s*=\s*(javascript:|vbscript:|data:\s*text\/html)[^\s>]+/gi,
      "",
    );
}

function stripDangerousStandaloneAttributes(value: string) {
  return value
    .replace(/\s(srcdoc)\s*=\s*(['"])[\s\S]*?\2/gi, "")
    .replace(/\s(srcdoc)\s*=\s*[^\s>]+/gi, "");
}

export function sanitizeHtmlFragment(value: string) {
  if (!value) {
    return "";
  }

  return stripDangerousUrlAttributes(
    stripDangerousStandaloneAttributes(
      stripInlineEventHandlers(
        stripDangerousStandaloneTags(
          stripDangerousBlocks(value),
        ),
      ),
    ),
  );
}

export function resolveRelativeHtmlMediaUrls(
  value: string,
  resolveUrl: (path: string) => string,
) {
  if (!value) {
    return "";
  }

  return value.replace(
    /\s(src|href)\s*=\s*(["'])(\/uploads\/[^"']*)\2/gi,
    (match, attribute: string, quote: string, path: string) => {
      const resolvedUrl = resolveUrl(path);
      return resolvedUrl ? ` ${attribute}=${quote}${resolvedUrl}${quote}` : match;
    },
  );
}
