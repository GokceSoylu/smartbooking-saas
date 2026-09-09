import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import "./globals.css";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  metadataBase: new URL("https://www.randevoapp.net"),
  title: {
    default: "Randevo | İşletmen için akıllı randevu sistemi",
    template: "%s | Randevo",
  },
  description:
    "Randevo ile kuaför ve güzellik salonunuzun online randevularını, personelini, hizmetlerini ve çalışma saatlerini tek pencereden yönetin.",
  keywords: [
    "randevu sistemi",
    "online randevu",
    "kuaför randevu sistemi",
    "güzellik salonu randevu sistemi",
    "online randevu sistemi",
    "kuaför randevu programı",
    "güzellik salonu randevu programı",
    "Randevo",
  ],
  icons: {
    icon: "/logo.svg", // public klasörünün içindeki dosya adı
  },
  other: {
    "instagram": "https://www.instagram.com/randevo.app/",
  },
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html
      lang="en"
      className={`${geistSans.variable} ${geistMono.variable} h-full antialiased`}
    >
      <body className="min-h-full flex flex-col">{children}</body>
    </html>
  );
}