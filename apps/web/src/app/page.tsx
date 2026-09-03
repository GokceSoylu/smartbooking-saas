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
      `${tenant.name} ${tenant.slug}`.toLocaleLowerCase("tr-TR").includes(value)
    );
  }, [search, tenants]);

  return (
    <main className="min-h-screen overflow-hidden bg-[#f7f8f7] text-slate-900">
      {/* Ambient background */}
      <div className="pointer-events-none fixed inset-0 -z-10">
        <div className="absolute -left-32 top-24 h-72 w-72 rounded-full bg-emerald-200/25 blur-3xl" />
        <div className="absolute right-0 top-0 h-96 w-96 rounded-full bg-teal-100/30 blur-3xl" />
      </div>

      {/* Header */}
      <header className="sticky top-0 z-50 border-b border-slate-200/70 bg-white/85 backdrop-blur-xl">
        <div className="mx-auto flex h-18 max-w-7xl items-center justify-between px-5 lg:px-8">
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

          <Link
            href="/login"
            className="inline-flex items-center gap-2 rounded-xl bg-slate-950 px-4 py-2.5 text-xs font-bold text-white shadow-sm transition hover:-translate-y-0.5 hover:bg-slate-800"
          >
            İşletme Paneli
            <ArrowRight className="h-3.5 w-3.5" />
          </Link>
        </div>
      </header>

      {/* Hero */}
      <section className="mx-auto max-w-7xl px-5 pb-16 pt-14 lg:px-8 lg:pb-20 lg:pt-20">
        <div className="grid items-center gap-14 lg:grid-cols-[1.05fr_.95fr]">
          <div className="max-w-2xl">
            <div className="mb-6 inline-flex items-center gap-2 rounded-full border border-emerald-200 bg-white px-3 py-1.5 text-[11px] font-bold text-emerald-700 shadow-sm">
              <Sparkles className="h-3.5 w-3.5" />
              Randevu yönetimi, sadeleştirildi.
            </div>

            <h1 className="text-5xl font-black leading-[0.98] tracking-[-0.055em] text-slate-950 sm:text-6xl lg:text-7xl">
              Randevunu al.
              <br />
              <span className="text-emerald-500">Zamanını yaşa.</span>
            </h1>

            <p className="mt-6 max-w-xl text-base leading-7 text-slate-500 sm:text-lg">
              Salonunu seç, hizmetini belirle ve uygun saatini birkaç saniyede
              ayır. Üyelik karmaşası olmadan, işletmenin onayıyla randevun hazır.
            </p>

            <div className="mt-8 flex flex-wrap gap-3">
              <a
                href="#businesses"
                className="inline-flex items-center gap-2 rounded-2xl bg-slate-950 px-5 py-3.5 text-sm font-bold text-white shadow-xl shadow-slate-950/10 transition hover:-translate-y-0.5 hover:bg-slate-800"
              >
                İşletmeleri keşfet
                <ArrowRight className="h-4 w-4" />
              </a>
              <Link
                href="/login"
                className="inline-flex items-center gap-2 rounded-2xl border border-slate-200 bg-white px-5 py-3.5 text-sm font-bold text-slate-700 shadow-sm transition hover:border-slate-300 hover:bg-slate-50"
              >
                İşletmeni yönet
              </Link>
            </div>

            <div className="mt-8 flex flex-wrap gap-x-6 gap-y-3 text-xs font-semibold text-slate-500">
              <span className="flex items-center gap-2">
                <CheckCircle2 className="h-4 w-4 text-emerald-500" />
                Hızlı randevu
              </span>
              <span className="flex items-center gap-2">
                <MessageCircle className="h-4 w-4 text-emerald-500" />
                WhatsApp bildirimleri
              </span>
              <span className="flex items-center gap-2">
                <ShieldCheck className="h-4 w-4 text-emerald-500" />
                Güvenli işletme paneli
              </span>
            </div>
          </div>

          {/* Product preview */}
          <div className="relative">
            <div className="absolute -inset-5 rounded-[2.5rem] bg-emerald-200/25 blur-2xl" />
            <div className="relative overflow-hidden rounded-[2rem] border border-slate-200 bg-white shadow-2xl shadow-slate-900/10">
              <div className="flex items-center justify-between border-b border-slate-100 px-5 py-4">
                <div>
                  <p className="text-[10px] font-bold uppercase tracking-[0.16em] text-slate-400">
                    Bugünün planı
                  </p>
                  <p className="mt-1 text-sm font-bold text-slate-900">
                    Bella Studio
                  </p>
                </div>
                <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-emerald-50 text-emerald-600">
                  <CalendarDays className="h-4 w-4" />
                </div>
              </div>

              <div className="space-y-2.5 p-4">
                {[
                  ["10:00", "Ayşe Yılmaz", "Saç Kesimi", "Onaylandı"],
                  ["11:30", "Mehmet Kaya", "Sakal & Bakım", "Bekliyor"],
                  ["13:00", "Elif Demir", "Saç Boyama", "Onaylandı"],
                  ["15:30", "Zeynep Arslan", "Cilt Bakımı", "Onaylandı"],
                ].map(([time, name, service, status], index) => (
                  <div
                    key={name}
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

              <div className="grid grid-cols-3 gap-px border-t border-slate-100 bg-slate-100">
                <div className="bg-white p-4">
                  <p className="text-[10px] font-semibold text-slate-400">
                    Randevu
                  </p>
                  <p className="mt-1 text-lg font-black">24</p>
                </div>
                <div className="bg-white p-4">
                  <p className="text-[10px] font-semibold text-slate-400">
                    Onaylı
                  </p>
                  <p className="mt-1 text-lg font-black text-emerald-600">18</p>
                </div>
                <div className="bg-white p-4">
                  <p className="text-[10px] font-semibold text-slate-400">
                    Bekleyen
                  </p>
                  <p className="mt-1 text-lg font-black text-amber-500">3</p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Value props */}
      <section className="border-y border-slate-200/70 bg-white">
        <div className="mx-auto grid max-w-7xl grid-cols-1 divide-y divide-slate-100 px-5 sm:grid-cols-3 sm:divide-x sm:divide-y-0 lg:px-8">
          {[
            {
              icon: CalendarDays,
              title: "Akıllı zamanlama",
              text: "Çalışma saatleri ve personel uygunluğu otomatik kontrol edilir.",
            },
            {
              icon: Users,
              title: "Tek panelden yönet",
              text: "Randevu, ekip, hizmet ve çalışma saatlerini tek yerde tut.",
            },
            {
              icon: MessageCircle,
              title: "Müşteriyi haberdar et",
              text: "Randevu durumlarını WhatsApp bildirimleriyle takip et.",
            },
          ].map((item) => {
            const Icon = item.icon;
            return (
              <div key={item.title} className="flex gap-4 px-5 py-7 sm:px-8">
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

      {/* Businesses */}
      <section id="businesses" className="mx-auto max-w-7xl px-5 py-16 lg:px-8 lg:py-20">
        <div className="flex flex-col justify-between gap-5 sm:flex-row sm:items-end">
          <div>
            <p className="text-[11px] font-black uppercase tracking-[0.18em] text-emerald-600">
              Randevo ağı
            </p>
            <h2 className="mt-2 text-3xl font-black tracking-[-0.04em]">
              Randevu alabileceğin işletmeler
            </h2>
            <p className="mt-2 text-sm text-slate-500">
              İşletmeni seç ve uygun saatleri görüntüle.
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
                      randevo.app/{tenant.slug}
                    </p>
                  </div>

                  <div className="mt-5 flex items-center gap-2 text-[11px] font-bold text-slate-500">
                    <span className="rounded-full bg-emerald-50 px-2.5 py-1 text-emerald-700">
                      Online randevu
                    </span>
                    <span className="text-slate-300">•</span>
                    <span>Hemen görüntüle</span>
                  </div>
                </Link>
              ))}
            </div>
          )}
        </div>
      </section>

      {/* Footer */}
      <footer className="border-t border-slate-200 bg-slate-950 text-white">
        <div className="mx-auto flex max-w-7xl flex-col gap-3 px-5 py-8 sm:flex-row sm:items-center sm:justify-between lg:px-8">
          <div className="flex items-center gap-2">
            <div className="flex h-8 w-8 items-center justify-center rounded-xl bg-white text-xs font-black text-slate-950">
              R
            </div>
            <span className="text-sm font-black tracking-[-0.03em]">
              randevo<span className="text-emerald-400">.</span>
            </span>
          </div>
          <p className="text-[11px] text-slate-500">
            Randevu yönetimini daha kolay hale getiriyoruz.
          </p>
        </div>
      </footer>
    </main>
  );
}
