"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { login, registerTenant } from "@/lib/api";
import { Lock, Mail, Store, User, Phone, ArrowRight, Sparkles } from "lucide-react";

export default function AuthPage() {
    const router = useRouter();
    const [isRegister, setIsRegister] = useState(false);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    // Ortak Alanlar
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");

    // Kayıt Alanları
    const [businessName, setBusinessName] = useState("");
    const [slug, setSlug] = useState("");
    const [fullName, setFullName] = useState("");
    const [phoneNumber, setPhoneNumber] = useState("");

    function handleBusinessNameChange(name: string) {
        setBusinessName(name);
        // Otomatik URL slug üret (Örn: Burak Berber -> burakberber)
        const generatedSlug = name
            .toLowerCase()
            .replace(/ğ/g, "g")
            .replace(/ü/g, "u")
            .replace(/ş/g, "s")
            .replace(/ı/g, "i")
            .replace(/ö/g, "o")
            .replace(/ç/g, "c")
            .replace(/[^a-z0-9]/g, "");
        setSlug(generatedSlug);
    }

    async function handleSubmit(e: React.FormEvent) {
        e.preventDefault();
        try {
            setLoading(true);
            setError(null);

            let res;
            if (isRegister) {
                res = await registerTenant({
                    businessName,
                    slug,
                    fullName,
                    email,
                    password,
                    phoneNumber,
                });
            } else {
                res = await login({ email, password });
            }

            localStorage.setItem("auth_token", res.token);
            localStorage.setItem("user_info", JSON.stringify(res));

            router.push("/dashboard");
        } catch (err: any) {
            setError(err.message || "İşlem sırasında bir hata oluştu.");
        } finally {
            setLoading(false);
        }
    }

    return (
        <div className="min-h-screen bg-slate-50 flex items-center justify-center p-4">
            <div className="max-w-md w-full bg-white rounded-3xl p-8 border border-slate-200/80 shadow-xl space-y-6">
                <div className="text-center space-y-1">
                    <div className="w-12 h-12 rounded-2xl bg-slate-900 text-emerald-400 flex items-center justify-center mx-auto mb-3 font-bold">
                        {isRegister ? <Sparkles className="w-6 h-6" /> : <Lock className="w-6 h-6" />}
                    </div>
                    <h1 className="text-2xl font-bold text-slate-900 tracking-tight">
                        {isRegister ? "İşletmeni Oluştur" : "İşletme Girişi"}
                    </h1>
                    <p className="text-xs text-slate-500">
                        {isRegister
                            ? "Akıllı randevu ve WhatsApp otomasyonunu başlatın."
                            : "Randevu ve yönetim paneline erişmek için oturum açın."}
                    </p>
                </div>

                {error && (
                    <div className="p-4 rounded-xl bg-rose-50 border border-rose-100 text-rose-600 text-xs font-medium">
                        ⚠️ {error}
                    </div>
                )}

                <form onSubmit={handleSubmit} className="space-y-3.5">
                    {isRegister && (
                        <>
                            <div>
                                <label className="block text-xs font-semibold text-slate-600 mb-1">İşletme Adı</label>
                                <div className="relative">
                                    <Store className="w-4 h-4 text-slate-400 absolute left-3.5 top-3.5" />
                                    <input
                                        required
                                        type="text"
                                        placeholder="Örn: Burak Duygun Kuaför"
                                        value={businessName}
                                        onChange={(e) => handleBusinessNameChange(e.target.value)}
                                        className="w-full pl-10 pr-4 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-emerald-500"
                                    />
                                </div>
                            </div>

                            <div>
                                <label className="block text-xs font-semibold text-slate-600 mb-1">
                                    Randevu Linkiniz (Slug)
                                </label>
                                <div className="flex items-center">
                                    <span className="text-xs text-slate-400 bg-slate-100 border border-r-0 border-slate-200 py-2.5 px-3 rounded-l-xl">
                                        /
                                    </span>
                                    <input
                                        required
                                        type="text"
                                        placeholder="burakduygun"
                                        value={slug}
                                        onChange={(e) => setSlug(e.target.value)}
                                        className="w-full pr-4 py-2.5 bg-slate-50 border border-slate-200 rounded-r-xl text-sm focus:outline-none focus:ring-2 focus:ring-emerald-500"
                                    />
                                </div>
                            </div>

                            <div>
                                <label className="block text-xs font-semibold text-slate-600 mb-1">Yetkili Ad Soyad</label>
                                <div className="relative">
                                    <User className="w-4 h-4 text-slate-400 absolute left-3.5 top-3.5" />
                                    <input
                                        required
                                        type="text"
                                        placeholder="Örn: Burak Duygun"
                                        value={fullName}
                                        onChange={(e) => setFullName(e.target.value)}
                                        className="w-full pl-10 pr-4 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-emerald-500"
                                    />
                                </div>
                            </div>

                            <div>
                                <label className="block text-xs font-semibold text-slate-600 mb-1">İşletme WhatsApp Numarası</label>
                                <div className="relative">
                                    <Phone className="w-4 h-4 text-slate-400 absolute left-3.5 top-3.5" />
                                    <input
                                        required
                                        type="tel"
                                        placeholder="905551112233"
                                        value={phoneNumber}
                                        onChange={(e) => setPhoneNumber(e.target.value)}
                                        className="w-full pl-10 pr-4 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-emerald-500"
                                    />
                                </div>
                            </div>
                        </>
                    )}

                    <div>
                        <label className="block text-xs font-semibold text-slate-600 mb-1">E-Posta</label>
                        <div className="relative">
                            <Mail className="w-4 h-4 text-slate-400 absolute left-3.5 top-3.5" />
                            <input
                                required
                                type="email"
                                placeholder="ornek@isletme.com"
                                value={email}
                                onChange={(e) => setEmail(e.target.value)}
                                className="w-full pl-10 pr-4 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-emerald-500"
                            />
                        </div>
                    </div>

                    <div>
                        <label className="block text-xs font-semibold text-slate-600 mb-1">Şifre</label>
                        <div className="relative">
                            <Lock className="w-4 h-4 text-slate-400 absolute left-3.5 top-3.5" />
                            <input
                                required
                                type="password"
                                placeholder="••••••••"
                                value={password}
                                onChange={(e) => setPassword(e.target.value)}
                                className="w-full pl-10 pr-4 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-emerald-500"
                            />
                        </div>
                    </div>

                    <button
                        type="submit"
                        disabled={loading}
                        className="w-full py-3.5 bg-slate-900 hover:bg-slate-800 text-white rounded-xl text-sm font-semibold transition-all flex items-center justify-center gap-2 disabled:opacity-50 mt-2"
                    >
                        {loading ? "İşleniyor..." : isRegister ? "Hemen Kaydol & Başla" : "Panele Giriş Yap"}
                        <ArrowRight className="w-4 h-4" />
                    </button>
                </form>

                {/* Geçiş Butonu */}
                <div className="text-center pt-2">
                    <button
                        type="button"
                        onClick={() => {
                            setIsRegister(!isRegister);
                            setError(null);
                        }}
                        className="text-xs text-slate-500 hover:text-slate-900 transition-colors"
                    >
                        {isRegister
                            ? "Zaten bir hesabınız var mı? Giriş Yapın"
                            : "Yeni bir işletme misiniz? Ücretsiz Kayıt Olun"}
                    </button>
                </div>
            </div>
        </div>
    );
}