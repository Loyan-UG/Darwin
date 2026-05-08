# Darwin.Web

`Darwin.Web` is the public customer-facing storefront and member portal for Darwin.
It is separate from `Darwin.WebAdmin`, which remains the operational back-office.

Current stack, from `package.json`:

- Next.js 16.2.1
- React 19.2.4
- React DOM 19.2.4
- TypeScript 5
- Tailwind CSS 4

## Development

Install dependencies:

```bash
npm install
```

Run the development server:

```bash
npm run dev
```

Run the production build locally:

```bash
npm run build
npm run start
```

Run lint and tests:

```bash
npm run lint
npm run test
```

Open [http://localhost:3000](http://localhost:3000).

For local storefront work, run `Darwin.WebApi` on its HTTP profile at `http://localhost:5134` and keep the Web runtime pointed at that URL.

## Purpose

Darwin.Web should present a clean public storefront and a focused member self-service area.
It must not expose internal review, readiness, route-health, API-status, diagnostic, or back-office concepts to shoppers.

Customer-facing failures should use concise copy, for example:

- "Products are temporarily unavailable."
- "We could not load your cart."
- "Please try again later."

Operational diagnostics, loader health, and observability belong on the server side and in logs/tests, not in rendered customer UI.

## Navigation

Primary navigation is customer-facing:

- Home: `/`
- Shop: `/catalog`
- Loyalty: `/loyalty`
- Help: `/help`
- Contact: `/page/kontakt`

Utility navigation:

- Account: `/account`
- Cart: `/cart`

Do not place checkout, orders, invoices, profile, preferences, addresses, security, CMS tooling, or admin-like pages in the primary public navigation.
Checkout is reachable from the cart or a direct checkout continuation only.
Orders, invoices, profile, preferences, addresses, security, and member loyalty details belong inside the member area.

Footer groups:

- Shop: Catalog, Loyalty, Cart
- Account: Sign in, Register, My account, Orders, Invoices
- Support: Help, Contact, Shipping, Returns / Cancellation
- Legal: Legal notice / Impressum, Privacy, Terms, Cancellation policy

If a CMS-backed menu is published, it should match this structure. Fallback navigation is kept clean and customer-facing.

## Customer-Facing Page Architecture

Public pages:

- `/`: concise storefront landing page with hero CTAs, featured categories, featured products/offers, loyalty teaser, help teaser, and optional trust strip.
- `/catalog`: product listing with title, search, category filter, optional sort/filter controls, product grid, pagination, and empty state.
- `/catalog/[slug]`: product detail with breadcrumb, gallery, name, price, description, variant/options where supported, quantity, add-to-cart, shipping note, details, and up to four related products.
- `/help`: customer help/information listing. The visible label is Help, Information, Pages, or Guides, never CMS.
- `/page/[slug]`: customer content detail page with breadcrumb, title, published content, and optional related pages.
- `/loyalty`: loyalty program overview for anonymous visitors and member loyalty summary/discovery for signed-in users.
- `/cart`: basket review with items, quantity update, remove, coupon, summary, continue shopping, checkout CTA, empty state, and limited recommendations.
- `/checkout`: checkout flow with customer/contact data, shipping address, shipping method, payment handoff, summary, and place-order action.
- `/checkout/orders/[orderId]/confirmation`: post-checkout result with order number, payment status, shipping summary, and next actions.

Member pages:

- `/account`: anonymous sign-in/register/recovery entry or signed-in member dashboard.
- `/account/profile`: profile edit form and verification actions.
- `/account/preferences`: marketing and privacy preferences.
- `/account/addresses`: address management and default billing/shipping actions.
- `/account/security`: password and security actions.
- `/orders`: member order list.
- `/orders/[id]`: member order detail.
- `/invoices`: member invoice list.
- `/invoices/[id]`: member invoice detail.
- `/loyalty/[businessId]`: member/business loyalty detail where supported.

The public UI must not render:

- route maps
- route summaries
- readiness panels
- review windows or review queues
- composition windows
- route-health messages
- API status labels
- loaded-count/server-total summaries
- image/offer/metadata coverage diagnostics
- internal CMS wording as a customer label
- back-office or admin-style "needs attention" panels

## Route Compatibility

Public content details now live under `/page/[slug]`.
The `/cms` index redirects to `/help`, and legacy `/cms/[slug]` links redirect to `/page/[slug]`.

## Data And Seed Expectations

The local seed should provide enough data to test the storefront end to end:

- customer-facing main navigation and footer menus
- multiple categories and products with variants
- CMS content for help, contact, shipping, returns, and legal pages
- shipping methods/rates for checkout intent creation
- promotions/coupons where supported
- loyalty businesses and rewards
- demo member data for account, addresses, orders, invoices, and loyalty

Menu seed, if managed in the backend, should publish:

- Main navigation: Home, Shop, Loyalty, Help, Contact
- Footer: Shop, Account, Support, Legal groups as described above

## Validation

Before considering a Web change complete, run:

```bash
npm run lint
npm run test
npm run build
```

For commerce work, also smoke the anonymous purchase path:

1. Open a seeded product detail page.
2. Add a variant to cart.
3. Review `/cart`.
4. Continue to `/checkout`.
5. Create the checkout intent and place the order.
6. Complete or simulate payment.
7. Verify the confirmation page renders the order number and payment state.

The rendered pages should remain customer-facing and should not show internal diagnostics.
