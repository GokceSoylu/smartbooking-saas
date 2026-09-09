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
    "Randevo ile kuaför ve güzellik salonunuzun online randevularını, personelini, hizmetlerini ve çalışma saatlerini tek panelden yönetin.",

  keywords: [
    "randevu sistemi",
    "online randevu",
    "kuaför randevu sistemi",
    "güzellik salonu randevu sistemi",
    "online randevu sistemi",
    "randevu programı",
    "kuaför randevu programı",
    "güzellik salonu randevu programı",
    "Randevo",
  ],

  applicationName: "Randevo",

  authors: [
    {
      name: "Randevo",
      url: "https://www.randevoapp.net",
    },
  ],

  creator: "Randevo",
  publisher: "Randevo",

  icons: {
    icon: "/logo.svg",
  },

  robots: {
    index: true,
    follow: true,
  },

  openGraph: {
    type: "website",
    locale: "tr_TR",
    url: "https://www.randevoapp.net",
    siteName: "Randevo",
    title: "Randevo | İşletmen için akıllı randevu sistemi",
    description:
      "Müşterilerin 7/24 online randevu alsın. Sen işletmeni, personelini ve randevularını tek panelden yönet.",
  },

  twitter: {
    card: "summary_large_image",
    title: "Randevo | İşletmen için akıllı randevu sistemi",
    description:
      "Kuaför ve güzellik salonları için online randevu ve işletme yönetim sistemi.",
  },
};

interface LayoutProps {
  children: React.ReactNode;
}

export default function RootLayout({ children }: LayoutProps) {
  return (
    <html
      lang="tr"
      className={`${geistSans.variable} ${geistMono.variable} h-full antialiased`}
    >
      <body className="min-h-full flex flex-col">{children}</body>
    </html>
  );
}