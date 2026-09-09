"use client";

import { useEffect, useMemo, useState } from "react";
import Link from "next/link";
import {
  ArrowRight,
  CalendarDays,
  CheckCircle2,
  ChevronRight,
  Clock3,
  Search,
  ShieldCheck,
  Sparkles,
  Store,
  Users,
  MessageCircle,
  Instagram,
  Zap,
} from "lucide-react";
import { fetchAllTenants, Tenant } from "@/lib/api";

export default function HomePage() {
  const [tenants, setTenants] = useState<Tenant[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");

  useEffect(() => {
    fetchAllTenants()
      .then(setTenants)
      .catch(console.error)
      .finally(() => setLoading(false));
  }, []);

  const filteredTenants = useMemo(() => {
    const value = search.trim().toLocaleLowerCase("tr-TR");

    if (!value) return tenants;

    return tenants.filter((tenant) =>
      `${tenant.name} ${tenant.slug}`
        .toLocaleLowerCase("tr-TR")
        .includes(value)
    );
  }, [search, tenants]);

  return (
    <main className="min-h-screen overflow-hidden bg-[#f7f8f7] text-slate-900">
      {/* Background */}
      <div className="pointer-events-none fixed inset-0 -z-10 overflow-hidden">
        <div className="absolute -left-40 top-20 h-96 w-96 rounded-full bg-emerald-200/25 blur-3xl" />
        <div className="absolute right-0 top-0 h-[30rem] w-[30rem] rounded-full bg-teal-100/30 blur-3xl" />
        <div className="absolute left-1/2 top-[35rem] h-72 w-72 -translate-x-1/2 rounded-full bg-emerald-100/20 blur-3xl" />
      </div>

      {/* Header */}
      <header className="sticky top-0 z-50 border-b border-slate-200/70 bg-white/85 backdrop-blur-xl">
        <div className="mx-auto flex h-16 max-w-7xl items-center justify-between px-5 lg:px-8">
          <Link href="/" className="group flex items-center gap-3">
            <div className="relative flex h-10 w-10 items-center justify-center overflow-hidden rounded-2xl bg-slate-950 text-white shadow-lg shadow-slate-950/10">
              <span className="text-sm font-black tracking-tight">R</span>
              <span className="absolute bottom-1.5 right-1.5 h-1.5 w-1.5 rounded-full bg-emerald-400" />
            </div>

            <div>
              <div className="text-lg font-black tracking-[-0.04em]">
                randevo<span className="text-emerald-500">.</span>
              </div>

              <div className="hidden text-[10px] font-medium text-slate-400 sm:block">
                İşletmen için akıllı randevu
              </div>
            </div>
          </Link>

          <div className="flex items-center gap-3">
            <a
              href="#businesses"
              className="hidden text-xs font-bold text-slate-500 transition hover:text-slate-900 sm:block"
            >
              İşletmeleri keşfet
            </a>

            <Link
              href="/login"
              className="inline-flex items-center gap-2 rounded-xl bg-slate-950 px-4 py-2.5 text-xs font-bold text-white shadow-sm transition hover:-translate-y-0.5 hover:bg-slate-800"
            >
              Ücretsiz başla
              <ArrowRight className="h-3.5 w-3.5" />
            </Link>
          </div>
        </div>
      </header>

      {/* Hero */}
      <section className="mx-auto max-w-7xl px-5 pb-20 pt-14 lg:px-8 lg:pb-28 lg:pt-24">
        <div className="grid items-center gap-14 lg:grid-cols-[1.02fr_.98fr]">
          {/* Left */}
          <div className="max-w-2xl">
            <div className="mb-6 inline-flex items-center gap-2 rounded-full border border-emerald-200 bg-white px-3.5 py-2 text-[11px] font-bold text-emerald-700 shadow-sm">
              <Sparkles className="h-3.5 w-3.5" />
              Kuaför & güzellik salonları için
            </div>

            <h1 className="text-5xl font-black leading-[0.96] tracking-[-0.06em] text-slate-950 sm:text-6xl lg:text-7xl">
              Boş saatlerini
              <br />
              <span className="text-emerald-500">doldur.</span>
            </h1>

            <p className="mt-7 max-w-xl text-base leading-7 text-slate-500 sm:text-lg">
              Müşterilerin 7/24 online randevu alsın. Sen randevularını,
              personelini ve hizmetlerini tek panelden yönet.
            </p>

            {/* Main CTA */}
            <div className="mt-8 flex flex-col gap-3 sm:flex-row">
              <Link
                href="/login"
                className="inline-flex items-center justify-center gap-2 rounded-2xl bg-slate-950 px-6 py-4 text-sm font-bold text-white shadow-xl shadow-slate-950/15 transition hover:-translate-y-0.5 hover:bg-slate-800"
              >
                Ücretsiz başla
                <ArrowRight className="h-4 w-4" />
              </Link>

              <a
                href="#how-it-works"
                className="inline-flex items-center justify-center gap-2 rounded-2xl border border-slate-200 bg-white px-6 py-4 text-sm font-bold text-slate-700 shadow-sm transition hover:border-slate-300 hover:bg-slate-50"
              >
                Nasıl çalışır?
              </a>
            </div>

            <div className="mt-7 flex flex-wrap gap-x-6 gap-y-3 text-xs font-semibold text-slate-500">
              <span className="flex items-center gap-2">
                <CheckCircle2 className="h-4 w-4 text-emerald-500" />
                Online randevu
              </span>

              <span className="flex items-center gap-2">
                <CheckCircle2 className="h-4 w-4 text-emerald-500" />
                Personel yönetimi
              </span>

              <span className="flex items-center gap-2">
                <CheckCircle2 className="h-4 w-4 text-emerald-500" />
                WhatsApp bildirimleri
              </span>
            </div>

            {/* Problem statement */}
            <div className="mt-10 max-w-xl rounded-2xl border border-slate-200/80 bg-white/80 p-5 shadow-sm">
              <p className="text-xs font-black uppercase tracking-[0.16em] text-slate-400">
                Tanıdık geliyor mu?
              </p>

              <div className="mt-3 grid gap-2 sm:grid-cols-2">
                <div className="flex items-center gap-2 text-sm text-slate-600">
                  <MessageCircle className="h-4 w-4 text-emerald-500" />
                  “Yarın boş saatiniz var mı?”
                </div>

                <div className="flex items-center gap-2 text-sm text-slate-600">
                  <MessageCircle className="h-4 w-4 text-emerald-500" />
                  “Ayşe Hanım müsait mi?”
                </div>

                <div className="flex items-center gap-2 text-sm text-slate-600">
                  <MessageCircle className="h-4 w-4 text-emerald-500" />
                  “Cumartesi saat kaçta boş?”
                </div>

                <div className="flex items-center gap-2 text-sm text-slate-600">
                  <MessageCircle className="h-4 w-4 text-emerald-500" />
                  “Randevumu değiştirebilir miyim?”
                </div>
              </div>

              <p className="mt-4 text-xs font-bold text-slate-900">
                Randevo ile müşterin uygun zamanı kendisi seçsin.
              </p>
            </div>
          </div>

          {/* Product Preview */}
          <div className="relative">
            <div className="absolute -inset-6 rounded-[3rem] bg-emerald-200/30 blur-3xl" />

            <div className="relative overflow-hidden rounded-[2rem] border border-slate-200 bg-white shadow-2xl shadow-slate-900/10">
              {/* Dashboard header */}
              <div className="flex items-center justify-between border-b border-slate-100 px-5 py-4">
                <div>
                  <p className="text-[10px] font-bold uppercase tracking-[0.16em] text-slate-400">
                    İşletme paneli
                  </p>

                  <p className="mt-1 text-sm font-bold text-slate-900">
                    Bella Studio
                  </p>
                </div>

                <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-emerald-50 text-emerald-600">
                  <CalendarDays className="h-4 w-4" />
                </div>
              </div>

              {/* Dashboard stats */}
              <div className="grid grid-cols-3 gap-px border-b border-slate-100 bg-slate-100">
                <div className="bg-white p-4">
                  <p className="text-[10px] font-semibold text-slate-400">
                    Bugün
                  </p>
                  <p className="mt-1 text-xl font-black">24</p>
                  <p className="mt-1 text-[9px] font-bold text-emerald-600">
                    randevu
                  </p>
                </div>

                <div className="bg-white p-4">
                  <p className="text-[10px] font-semibold text-slate-400">
                    Onaylı
                  </p>
                  <p className="mt-1 text-xl font-black text-emerald-600">
                    18
                  </p>
                  <p className="mt-1 text-[9px] font-bold text-slate-400">
                    randevu
                  </p>
                </div>

                <div className="bg-white p-4">
                  <p className="text-[10px] font-semibold text-slate-400">
                    Bekleyen
                  </p>
                  <p className="mt-1 text-xl font-black text-amber-500">3</p>
                  <p className="mt-1 text-[9px] font-bold text-slate-400">
                    randevu
                  </p>
                </div>
              </div>

              {/* Appointments */}
              <div className="space-y-2.5 p-4">
                {[
                  ["10:00", "Ayşe Yılmaz", "Saç Kesimi", "Onaylandı"],
                  ["11:30", "Mehmet Kaya", "Sakal & Bakım", "Bekliyor"],
                  ["13:00", "Elif Demir", "Saç Boyama", "Onaylandı"],
                  ["15:30", "Zeynep Arslan", "Cilt Bakımı", "Onaylandı"],
                ].map(([time, name, service, status], index) => (
                  <div
                    key={`${name}-${time}`}
                    className="flex items-center gap-3 rounded-2xl border border-slate-100 bg-slate-50/70 p-3"
                  >
                    <div className="w-11 shrink-0 text-center text-xs font-black text-slate-500">
                      {time}
                    </div>

                    <div className="h-9 w-px bg-slate-200" />

                    <div className="min-w-0 flex-1">
                      <p className="truncate text-xs font-bold text-slate-800">
                        {name}
                      </p>

                      <p className="mt-0.5 truncate text-[11px] text-slate-400">
                        {service}
                      </p>
                    </div>

                    <span
                      className={`hidden rounded-full px-2 py-1 text-[9px] font-bold sm:block ${status === "Bekliyor"
                          ? "bg-amber-50 text-amber-600"
                          : "bg-emerald-50 text-emerald-600"
                        }`}
                    >
                      {status}
                    </span>

                    {index === 0 && (
                      <Clock3 className="h-4 w-4 text-slate-300" />
                    )}
                  </div>
                ))}
              </div>

              {/* Dashboard bottom */}
              <div className="border-t border-slate-100 bg-slate-50/50 p-4">
                <div className="flex items-center gap-3 rounded-2xl border border-emerald-100 bg-emerald-50/60 p-4">
                  <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-emerald-500 text-white">
                    <Zap className="h-4 w-4" />
                  </div>

                  <div>
                    <p className="text-xs font-black text-slate-900">
                      Yeni online randevu
                    </p>

                    <p className="mt-0.5 text-[11px] text-slate-500">
                      Müşterin kendi randevusunu oluşturdu.
                    </p>
                  </div>
                </div>
              </div>
            </div>

            {/* Floating badge */}
            <div className="absolute -bottom-5 -left-5 hidden rounded-2xl border border-slate-200 bg-white p-4 shadow-xl sm:block">
              <div className="flex items-center gap-3">
                <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-emerald-50 text-emerald-600">
                  <CheckCircle2 className="h-5 w-5" />
                </div>

                <div>
                  <p className="text-[10px] font-bold text-slate-400">
                    DURUM
                  </p>
                  <p className="text-xs font-black text-slate-900">
                    Sistem aktif
                  </p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Value Props */}
      <section className="border-y border-slate-200/70 bg-white">
        <div className="mx-auto grid max-w-7xl grid-cols-1 divide-y divide-slate-100 px-5 sm:grid-cols-3 sm:divide-x sm:divide-y-0 lg:px-8">
          {[
            {
              icon: CalendarDays,
              title: "Online randevu",
              text: "Müşterilerin uygun saatleri görüp randevusunu kendi oluştursun.",
            },
            {
              icon: Users,
              title: "Ekibini yönet",
              text: "Personellerini, çalışma saatlerini ve hizmetlerini tek panelden yönet.",
            },
            {
              icon: MessageCircle,
              title: "Müşteriyi haberdar et",
              text: "Randevu durumlarını ve bildirimlerini daha kolay takip et.",
            },
          ].map((item) => {
            const Icon = item.icon;

            return (
              <div key={item.title} className="flex gap-4 px-5 py-8 sm:px-8">
                <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-slate-950 text-emerald-400">
                  <Icon className="h-4 w-4" />
                </div>

                <div>
                  <h2 className="text-sm font-bold">{item.title}</h2>

                  <p className="mt-1 text-xs leading-5 text-slate-500">
                    {item.text}
                  </p>
                </div>
              </div>
            );
          })}
        </div>
      </section>

      {/* How it works */}
      <section
        id="how-it-works"
        className="mx-auto max-w-7xl px-5 py-20 lg:px-8 lg:py-28"
      >
        <div className="mx-auto max-w-2xl text-center">
          <p className="text-[11px] font-black uppercase tracking-[0.18em] text-emerald-600">
            Nasıl çalışır?
          </p>

          <h2 className="mt-3 text-3xl font-black tracking-[-0.04em] sm:text-4xl">
            Randevu yönetimini
            <br />
            üç adımda kolaylaştır.
          </h2>

          <p className="mt-4 text-sm leading-6 text-slate-500">
            İşletmeni oluştur, randevu linkini paylaş ve müşterilerinin
            randevularını kendilerinin oluşturmasına izin ver.
          </p>
        </div>

        <div className="mt-12 grid gap-5 md:grid-cols-3">
          {[
            {
              number: "01",
              icon: Store,
              title: "İşletmeni oluştur",
              text: "İşletmeni, hizmetlerini, çalışanlarını ve çalışma saatlerini birkaç adımda tanımla.",
            },
            {
              number: "02",
              icon: Instagram,
              title: "Linkini paylaş",
              text: "Randevo linkini Instagram bio'na, WhatsApp'a veya müşterilerine gönder.",
            },
            {
              number: "03",
              icon: CalendarDays,
              title: "Randevularını yönet",
              text: "Müşterilerin uygun saatleri seçsin. Sen tüm randevuları tek panelden yönet.",
            },
          ].map((item) => {
            const Icon = item.icon;

            return (
              <div
                key={item.number}
                className="relative rounded-3xl border border-slate-200 bg-white p-6 shadow-sm transition hover:-translate-y-1 hover:shadow-xl"
              >
                <div className="flex items-center justify-between">
                  <div className="flex h-11 w-11 items-center justify-center rounded-2xl bg-slate-950 text-emerald-400">
                    <Icon className="h-5 w-5" />
                  </div>

                  <span className="text-xs font-black tracking-widest text-slate-300">
                    {item.number}
                  </span>
                </div>

                <h3 className="mt-6 text-base font-black">{item.title}</h3>

                <p className="mt-2 text-sm leading-6 text-slate-500">
                  {item.text}
                </p>
              </div>
            );
          })}
        </div>
      </section>

      {/* CTA */}
      <section className="px-5 pb-20 lg:px-8 lg:pb-28">
        <div className="mx-auto max-w-7xl overflow-hidden rounded-[2rem] bg-slate-950 px-6 py-14 text-white sm:px-10 lg:px-16 lg:py-16">
          <div className="grid items-center gap-10 lg:grid-cols-[1fr_auto]">
            <div>
              <p className="text-[11px] font-black uppercase tracking-[0.18em] text-emerald-400">
                Randevo ile başla
              </p>

              <h2 className="mt-3 max-w-2xl text-3xl font-black tracking-[-0.04em] sm:text-4xl">
                Randevu trafiğini
                <br />
                tek yerde yönet.
              </h2>

              <p className="mt-4 max-w-xl text-sm leading-6 text-slate-400">
                İşletmeni oluştur, randevu linkini paylaş ve müşterilerinin
                sana ulaşmasını kolaylaştır.
              </p>
            </div>

            <Link
              href="/login"
              className="inline-flex items-center justify-center gap-2 rounded-2xl bg-white px-6 py-4 text-sm font-black text-slate-950 transition hover:-translate-y-0.5 hover:bg-slate-100"
            >
              Ücretsiz başla
              <ArrowRight className="h-4 w-4" />
            </Link>
          </div>
        </div>
      </section>

      {/* Businesses */}
      <section
        id="businesses"
        className="mx-auto max-w-7xl px-5 pb-20 lg:px-8 lg:pb-28"
      >
        <div className="flex flex-col justify-between gap-5 sm:flex-row sm:items-end">
          <div>
            <p className="text-[11px] font-black uppercase tracking-[0.18em] text-emerald-600">
              Randevo ağı
            </p>

            <h2 className="mt-2 text-3xl font-black tracking-[-0.04em]">
              Randevo kullanan işletmeler
            </h2>

            <p className="mt-2 text-sm text-slate-500">
              Randevo üzerinden online randevu alabileceğin işletmeleri keşfet.
            </p>
          </div>

          <div className="relative w-full sm:w-64">
            <Search className="absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />

            <input
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="İşletme ara..."
              className="w-full rounded-xl border border-slate-200 bg-white py-3 pl-10 pr-4 text-xs font-medium outline-none transition focus:border-emerald-400 focus:ring-4 focus:ring-emerald-500/10"
            />
          </div>
        </div>

        <div className="mt-8">
          {loading ? (
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
              {[1, 2, 3].map((item) => (
                <div
                  key={item}
                  className="h-52 animate-pulse rounded-3xl border border-slate-200 bg-white"
                />
              ))}
            </div>
          ) : filteredTenants.length === 0 ? (
            <div className="rounded-3xl border border-dashed border-slate-300 bg-white px-6 py-16 text-center">
              <Store className="mx-auto h-8 w-8 text-slate-300" />

              <p className="mt-3 text-sm font-bold text-slate-600">
                {search
                  ? "Aramanla eşleşen işletme bulunamadı."
                  : "Henüz kayıtlı bir işletme bulunmuyor."}
              </p>
            </div>
          ) : (
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
              {filteredTenants.map((tenant) => (
                <Link
                  key={tenant.id}
                  href={`/${tenant.slug}`}
                  className="group relative overflow-hidden rounded-3xl border border-slate-200 bg-white p-5 shadow-sm transition duration-300 hover:-translate-y-1 hover:border-emerald-200 hover:shadow-xl hover:shadow-slate-900/5"
                >
                  <div className="flex items-start justify-between">
                    <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-slate-950 text-lg font-black text-emerald-400">
                      {tenant.name[0]?.toUpperCase()}
                    </div>

                    <div className="flex h-8 w-8 items-center justify-center rounded-full bg-slate-50 text-slate-300 transition group-hover:bg-emerald-50 group-hover:text-emerald-600">
                      <ChevronRight className="h-4 w-4" />
                    </div>
                  </div>

                  <div className="mt-7">
                    <h3 className="text-lg font-black tracking-[-0.025em]">
                      {tenant.name}
                    </h3>

                    <p className="mt-1 text-xs text-slate-400">
                      randevoapp.net/{tenant.slug}
                    </p>
                  </div>

                  <div className="mt-5 flex items-center gap-2 text-[11px] font-bold text-slate-500">
                    <span className="rounded-full bg-emerald-50 px-2.5 py-1 text-emerald-700">
                      Online randevu
                    </span>

                    <span className="text-slate-300">•</span>

                    <span>Randevu al</span>
                  </div>
                </Link>
              ))}
            </div>
          )}
        </div>
      </section>

      {/* Footer */}
      <footer className="border-t border-slate-200 bg-slate-950 text-white">
        <div className="mx-auto flex max-w-7xl flex-col gap-5 px-5 py-9 sm:flex-row sm:items-center sm:justify-between lg:px-8">
          <div className="flex items-center gap-3">
            <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-white text-xs font-black text-slate-950">
              R
            </div>

            <div>
              <div className="text-sm font-black tracking-[-0.03em]">
                randevo<span className="text-emerald-400">.</span>
              </div>

              <p className="mt-0.5 text-[10px] text-slate-500">
                İşletmen için akıllı randevu
              </p>
            </div>
          </div>

          <div className="flex flex-col gap-2 sm:items-end">
            <p className="text-[11px] text-slate-500">
              Randevu yönetimini daha kolay hale getiriyoruz.
            </p>

            <a
              href="https://gokcedev.com"
              target="_blank"
              rel="noopener noreferrer"
              className="text-[11px] font-semibold text-slate-400 transition hover:text-white"
            >
              Designed & Built by{" "}
              <span className="text-emerald-400">gokcedev</span>
            </a>
          </div>
        </div>
      </footer>
    </main>
  );
}