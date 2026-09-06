"use client";

import { useState } from "react";
import Link from "next/link";
import { CheckCircle2, Clock, Sparkles } from "lucide-react";
import { registerTenant } from "@/lib/api";

export default function RegisterPage() {
    const [businessName, setBusinessName] = useState("");
    const [slug, setSlug] = useState("");
    const [fullName, setFullName] = useState("");
    const [email, setEmail] = useState("");
    const [phoneNumber, setPhoneNumber] = useState("");
    const [password, setPassword] = useState("");
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState("");
    const [isSubmitted, setIsSubmitted] = useState(false);

    async function handleSubmit(e: React.FormEvent) {
        e.preventDefault();
        setError("");
        setLoading(true);

        try {
            await registerTenant({
                businessName,
                slug,
                fullName,
                email,
                phoneNumber,
                password,
            });
            setIsSubmitted(true);
        } catch (err: any) {
            setError(err.message || "Başvuru oluşturulamadı.");
        } finally {
            setLoading(false);
        }
    }

    if (isSubmitted) {
        return (
            <div className="min-h-screen flex items-center justify-center bg-[#f7f8f7] p-6">
                <div className="max-w-md w-full rounded-3xl border border-slate-200 bg-white p-8 text-center shadow-sm">
                    <div className="w-14 h-14 bg-emerald-50 text-emerald-600 rounded-2xl flex items-center justify-center mx-auto mb-4 border border-emerald-100">
                        <CheckCircle2 className="w-8 h-8" />
                    </div>
                    <h2 className="text-xl font-black text-slate-900 tracking-tight">Başvurunuz Alındı!</h2>
                    <p className="mt-2 text-xs leading-5 text-slate-500">
                        <strong className="text-slate-800">{businessName}</strong> işletme başvurunuz sistem yöneticisine iletildi.
                        Hesabınız onaylandığında ve aboneliğiniz tanımlandığında WhatsApp üzerinden bilgilendirileceksiniz.
                    </p>

                    <div className="mt-6 rounded-2xl bg-amber-50 border border-amber-200 p-4 text-left flex items-start gap-3">
                        <Clock className="w-4 h-4 text-amber-600 shrink-0 mt-0.5" />
                        <p className="text-[11px] text-amber-800 leading-4">
                            Güvenlik ve abonelik doğrulaması sebebiyle yönetici onayı verilmeden panele giriş yapılamaz.
                        </p>
                    </div>

                    <Link
                        href="/login"
                        className="mt-6 block w-full rounded-xl bg-slate-950 py-3 text-xs font-bold text-white hover:bg-slate-800 transition"
                    >
                        Giriş Sayfasına Dön
                    </Link>
                </div>
            </div>
        );
    }

    return (
        <div className="min-h-screen flex items-center justify-center bg-[#f7f8f7] p-6">
            <div className="max-w-md w-full rounded-3xl border border-slate-200 bg-white p-8 shadow-sm">
                <div className="flex items-center gap-2 text-emerald-600 font-bold text-xs mb-1">
                    <Sparkles className="w-4 h-4" />
                    İşletme Ön Başvurusu
                </div>
                <h1 className="text-2xl font-black text-slate-950 tracking-tight">İşletmenizi Kaydedin</h1>
                <p className="text-xs text-slate-400 mt-1 mb-6">
                    Başvurunuz onaylandıktan sonra 30 günlük deneme aboneliğiniz başlar.
                </p>

                {error && (
                    <div className="mb-4 rounded-xl bg-rose-50 border border-rose-200 p-3 text-xs font-bold text-rose-600">
                        {error}
                    </div>
                )}

                <form onSubmit={handleSubmit} className="space-y-3.5">
                    <div>
                        <label className="block text-[10px] font-black uppercase text-slate-400 mb-1">İşletme Adı</label>
                        <input
                            required
                            value={businessName}
                            onChange={(e) => {
                                setBusinessName(e.target.value);
                                setSlug(e.target.value.toLowerCase().replace(/[^a-z0-9]/g, ""));
                            }}
                            placeholder="Örn: Gökçe Kuaför"
                            className="w-full rounded-xl border border-slate-200 bg-slate-50 px-3.5 py-2.5 text-xs font-semibold outline-none focus:border-emerald-500 focus:bg-white"
                        />
                    </div>

                    <div>
                        <label className="block text-[10px] font-black uppercase text-slate-400 mb-1">Bağlantı Adresi (Slug)</label>
                        <input
                            required
                            value={slug}
                            onChange={(e) => setSlug(e.target.value)}
                            placeholder="gokce-kuafor"
                            className="w-full rounded-xl border border-slate-200 bg-slate-50 px-3.5 py-2.5 text-xs font-semibold outline-none focus:border-emerald-500 focus:bg-white"
                        />
                    </div>

                    <div>
                        <label className="block text-[10px] font-black uppercase text-slate-400 mb-1">Yetkili Ad Soyad</label>
                        <input
                            required
                            value={fullName}
                            onChange={(e) => setFullName(e.target.value)}
                            placeholder="Yetkili Adı"
                            className="w-full rounded-xl border border-slate-200 bg-slate-50 px-3.5 py-2.5 text-xs font-semibold outline-none focus:border-emerald-500 focus:bg-white"
                        />
                    </div>

                    <div>
                        <label className="block text-[10px] font-black uppercase text-slate-400 mb-1">Telefon (WhatsApp)</label>
                        <input
                            required
                            type="tel"
                            value={phoneNumber}
                            onChange={(e) => setPhoneNumber(e.target.value)}
                            placeholder="905551112233"
                            className="w-full rounded-xl border border-slate-200 bg-slate-50 px-3.5 py-2.5 text-xs font-semibold outline-none focus:border-emerald-500 focus:bg-white"
                        />
                    </div>

                    <div>
                        <label className="block text-[10px] font-black uppercase text-slate-400 mb-1">Giriş E-postası</label>
                        <input
                            required
                            type="email"
                            value={email}
                            onChange={(e) => setEmail(e.target.value)}
                            placeholder="info@isletme.com"
                            className="w-full rounded-xl border border-slate-200 bg-slate-50 px-3.5 py-2.5 text-xs font-semibold outline-none focus:border-emerald-500 focus:bg-white"
                        />
                    </div>

                    <div>
                        <label className="block text-[10px] font-black uppercase text-slate-400 mb-1">Şifre</label>
                        <input
                            required
                            type="password"
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                            placeholder="••••••••"
                            className="w-full rounded-xl border border-slate-200 bg-slate-50 px-3.5 py-2.5 text-xs font-semibold outline-none focus:border-emerald-500 focus:bg-white"
                        />
                    </div>

                    <button
                        type="submit"
                        disabled={loading}
                        className="w-full rounded-xl bg-slate-950 py-3 text-xs font-bold text-white hover:bg-slate-800 transition mt-2 disabled:opacity-50"
                    >
                        {loading ? "Başvuru Gönderiliyor..." : "Ön Başvuruyu Gönder"}
                    </button>
                </form>

                <p className="text-center text-[11px] text-slate-400 mt-4">
                    Zaten onaylı hesabınız var mı?{" "}
                    <Link href="/login" className="font-bold text-slate-800 hover:underline">
                        Giriş Yap
                    </Link>
                </p>
            </div>
        </div>
    );
}