import assert from "node:assert/strict";
import test from "node:test";
import React from "react";
import { renderToStaticMarkup } from "react-dom/server";
import { SurfaceSectionNav } from "@/components/layout/surface-section-nav";

test("SurfaceSectionNav renders the shared sticky grocery shell", () => {
  const html = renderToStaticMarkup(
    React.createElement(SurfaceSectionNav, {
      items: [
        { href: "#overview", label: "Overview" },
        { href: "#delivery", label: "Delivery" },
        { href: "#actions", label: "Actions" },
      ],
    }),
  );

  assert.match(html, /sticky top-24 z-10 -mt-2/);
  assert.match(html, /rounded-\[2rem\]/);
  assert.match(html, /white_84%,#eff7e9_16%/);
  assert.ok(html.includes('href="#overview"'));
  assert.ok(html.includes('href="#delivery"'));
  assert.ok(html.includes('href="#actions"'));
});
