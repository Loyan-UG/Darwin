import "server-only";
import QRCode from "qrcode";

export async function createQrCodeDataUrl(value: string) {
  return QRCode.toDataURL(value, {
    errorCorrectionLevel: "M",
    margin: 1,
    width: 320,
    color: {
      dark: "#1f3422",
      light: "#fffdf8",
    },
  });
}
