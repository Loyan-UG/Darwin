import { addToCartAction } from "@/features/cart/actions";

type AddToCartFormProps = {
  variantId: string;
  productName: string;
  productHref: string;
  productImageUrl?: string | null;
  productImageAlt?: string | null;
  productSku?: string | null;
  returnPath: string;
  quantity?: number;
};

export function AddToCartForm({
  variantId,
  productName,
  productHref,
  productImageUrl,
  productImageAlt,
  productSku,
  returnPath,
  quantity = 1,
}: AddToCartFormProps) {
  return (
    <form action={addToCartAction} className="contents">
      <input type="hidden" name="variantId" value={variantId} />
      <input type="hidden" name="quantity" value={quantity} />
      <input type="hidden" name="returnPath" value={returnPath} />
      <input type="hidden" name="productName" value={productName} />
      <input type="hidden" name="productHref" value={productHref} />
      <input type="hidden" name="productImageUrl" value={productImageUrl ?? ""} />
      <input type="hidden" name="productImageAlt" value={productImageAlt ?? ""} />
      <input type="hidden" name="productSku" value={productSku ?? ""} />
      <button
        type="submit"
        className="inline-flex items-center justify-center rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
      >
        Add to cart
      </button>
    </form>
  );
}
