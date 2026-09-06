"use client";

import { useEffect, useState } from "react";
import {
    Building2,
    CheckCircle2,
    Clock,
    Lock,
    PlusCircle,
    RefreshCcw,
    ShieldCheck,
    Unlock,
    UserCheck,
    XCircle
} from "lucide-react";
import {
    AdminTenantItem,
    approveTenant,
    extendTenantSubscription,
    fetchAdminTenants,
    toggleTenantStatus
} from "@/lib/api";

export default function SuperAdminTenantsPage() {
    const [tenants, setTenants] = useState<AdminTenantItem[]>([]);
    const [loading, setLoading] = useState(true);
    const [actionLoadingId, setActionLoadingId] = useState<string | null>(null);

    async function loadTenants() {
        try {
            setLoading(true);
            const data = await fetchAdminTenants();
            setTenants(data);
        } catch (err) {
            console.error(err);
            alert("İşletmeler yüklenemedi.");
        } finally {
            setLoading(false);
        }
    }

    useEffect(() => {
        loadTenants();
    }, []);

    async function handleApprove(id: string) {
        if (!confirm("Bu işletmeyi onaylayıp 30 günlük deneme aboneliği başlatmak istiyor musunuz?")) return;
        try {
            setActionLoadingId(id);
            await approveTenant(id);
            await loadTenants();
        } catch (err: any) {
            alert(err.message);
        } finally {
            setActionLoadingId(null);
        }
    }

    async function handleToggleStatus(id: string, currentStatus: boolean) {
        const actionText = currentStatus ? "dondurmak (erişimini kapatmak)" : "yeniden aktif etmek";
        if (!confirm(`Bu işletmenin sistemini ${actionText} istiyor musunuz?`)) return;
        try {
            setActionLoadingId(id);
            await toggleTenantStatus(id);
            await loadTenants();
        } catch (err: any) {
            alert(err.message);
        } finally {
            setActionLoadingId(null);
        }
    }

    async function handleExtend(id: string, days: number) {
        try {
            setActionLoadingId(id);
            await extendTenantSubscription(id, days);
            await loadTenants();
        } catch (err: any) {
            alert(err.message);
        } finally {
            setActionLoadingId(null);
        }
    }

    return (
        <div className="min-h-screen bg-[#f7f8f7] text-slate-900 p-6 lg:p-12">
            <div className="mx-auto max-w-7xl">
                {/* Header */}
                <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 border-b border-slate-200 pb-6 mb-8">
                    <div>
                        <div className="flex items-center gap-2 text-emerald-600 font-bold text-xs">
                            <ShieldCheck className="w-4 h-4" />
                            Süper Yönetici Paneli
                        </div>
                        <h1 className="text-3xl font-black tracking-tight text-slate-950 mt-1">
                            İşletme Onay & Abonelik Yönetimi
                        </h1>
                        <p className="text-xs text-slate-500 mt-1">
                            Kayıt başvurularını onayla, erişimleri sınırla veya dilediğin işletmenin aboneliğini uzat.
                        </p>
                    </div>
                    <button
                        onClick={loadTenants}
                        disabled={loading}
                        className="inline-flex items-center gap-2 rounded-xl bg-white px-4 py-2.5 text-xs font-bold text-slate-700 shadow-sm border border-slate-200 hover:bg-slate-50"
                    >
                        <RefreshCcw className={`w-3.5 h-3.5 ${loading ? "animate-spin" : ""}`} />
                        Listeyi Yenile
                    </button>
                </div>

                {/* Tablo */}
                <div className="overflow-hidden rounded-3xl border border-slate-200 bg-white shadow-sm">
                    {loading ? (
                        <div className="p-8 space-y-4">
                            {[1, 2, 3].map((i) => (
                                <div key={i} className="h-16 bg-slate-50 animate-pulse rounded-2xl" />
                            ))}
                        </div>
                    ) : tenants.length === 0 ? (
                        <div className="p-16 text-center text-slate-400">
                            <Building2 className="w-10 h-10 mx-auto opacity-40 mb-3" />
                            <p className="text-sm font-bold text-slate-600">Henüz hiçbir işletme başvurusu yok.</p>
                        </div>
                    ) : (
                        <div className="overflow-x-auto">
                            <table className="w-full text-left">
                                <thead className="border-b border-slate-100 bg-slate-50/60 text-[9px] font-black uppercase tracking-[0.12em] text-slate-400">
                                    <tr>
                                        <th className="px-6 py-4">İşletme</th>
                                        <th className="px-6 py-4">Telefon</th>
                                        <th className="px-6 py-4">Onay Durumu</th>
                                        <th className="px-6 py-4">Sistem Erişimi</th>
                                        <th className="px-6 py-4">Abonelik Bitiş</th>
                                        <th className="px-6 py-4 text-right">Yönetim İşlemleri</th>
                                    </tr>
                                </thead>
                                <tbody className="divide-y divide-slate-100">
                                    {tenants.map((t) => {
                                        const isExpired = t.subscriptionExpiresAtUtc
                                            ? new Date(t.subscriptionExpiresAtUtc) < new Date()
                                            : false;

                                        return (
                                            <tr key={t.id} className="hover:bg-slate-50/70 transition">
                                                <td className="px-6 py-4">
                                                    <div className="flex items-center gap-3">
                                                        <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-slate-950 text-xs font-black text-emerald-400">
                                                            {t.name[0]?.toUpperCase()}
                                                        </div>
                                                        <div>
                                                            <p className="text-xs font-black text-slate-800">{t.name}</p>
                                                            <p className="text-[10px] text-slate-400">/{t.slug}</p>
                                                        </div>
                                                    </div>
                                                </td>

                                                <td className="px-6 py-4 text-xs font-semibold text-slate-600">
                                                    {t.phoneNumber || "Belirtilmemiş"}
                                                </td>

                                                {/* Onay Rozeti */}
                                                <td className="px-6 py-4">
                                                    {t.isApproved ? (
                                                        <span className="inline-flex items-center gap-1.5 rounded-full bg-emerald-50 px-2.5 py-1 text-[10px] font-bold text-emerald-700 border border-emerald-200">
                                                            <CheckCircle2 className="w-3 h-3" /> Onaylandı
                                                        </span>
                                                    ) : (
                                                        <span className="inline-flex items-center gap-1.5 rounded-full bg-amber-50 px-2.5 py-1 text-[10px] font-bold text-amber-700 border border-amber-200 animate-pulse">
                                                            <Clock className="w-3 h-3" /> Başvuru Bekliyor
                                                        </span>
                                                    )}
                                                </td>

                                                {/* Erişim Rozeti */}
                                                <td className="px-6 py-4">
                                                    {t.isActive ? (
                                                        <span className="inline-flex items-center gap-1.5 text-xs font-bold text-emerald-600">
                                                            <Unlock className="w-3.5 h-3.5" /> Açık
                                                        </span>
                                                    ) : (
                                                        <span className="inline-flex items-center gap-1.5 text-xs font-bold text-rose-600">
                                                            <Lock className="w-3.5 h-3.5" /> Donduruldu
                                                        </span>
                                                    )}
                                                </td>

                                                {/* Abonelik Tarihi */}
                                                <td className="px-6 py-4">
                                                    {t.subscriptionExpiresAtUtc ? (
                                                        <div>
                                                            <p className={`text-xs font-bold ${isExpired ? "text-rose-600" : "text-slate-700"}`}>
                                                                {new Date(t.subscriptionExpiresAtUtc).toLocaleDateString("tr-TR")}
                                                            </p>
                                                            <p className="text-[9px] text-slate-400">
                                                                {isExpired ? "Süresi Bitti" : "Aktif Dönem"}
                                                            </p>
                                                        </div>
                                                    ) : (
                                                        <span className="text-xs text-slate-400">—</span>
                                                    )}
                                                </td>

                                                {/* Aksiyon Butonları */}
                                                <td className="px-6 py-4 text-right">
                                                    <div className="flex items-center justify-end gap-2">
                                                        {/* Henüz Onaylanmadıysa Onayla Butonu */}
                                                        {!t.isApproved && (
                                                            <button
                                                                disabled={actionLoadingId === t.id}
                                                                onClick={() => handleApprove(t.id)}
                                                                className="rounded-xl bg-emerald-500 px-3 py-2 text-[10px] font-bold text-white shadow-sm hover:bg-emerald-600 transition disabled:opacity-50"
                                                            >
                                                                Onayla (+30 Gün)
                                                            </button>
                                                        )}

                                                        {/* Dondur / Aç Butonu */}
                                                        {t.isApproved && (
                                                            <button
                                                                disabled={actionLoadingId === t.id}
                                                                onClick={() => handleToggleStatus(t.id, t.isActive)}
                                                                className={`rounded-xl px-3 py-2 text-[10px] font-bold transition disabled:opacity-50 ${t.isActive
                                                                        ? "bg-rose-50 text-rose-600 hover:bg-rose-100 border border-rose-200"
                                                                        : "bg-emerald-50 text-emerald-700 hover:bg-emerald-100 border border-emerald-200"
                                                                    }`}
                                                            >
                                                                {t.isActive ? "Erişimi Kes" : "Erişimi Aç"}
                                                            </button>
                                                        )}

                                                        {/* +30 Gün Abonelik Ekle Butonu */}
                                                        {t.isApproved && (
                                                            <button
                                                                disabled={actionLoadingId === t.id}
                                                                onClick={() => handleExtend(t.id, 30)}
                                                                className="rounded-xl bg-slate-950 px-3 py-2 text-[10px] font-bold text-white hover:bg-slate-800 transition disabled:opacity-50"
                                                            >
                                                                +30 Gün Ekle
                                                            </button>
                                                        )}
                                                    </div>
                                                </td>
                                            </tr>
                                        );
                                    })}
                                </tbody>
                            </table>
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
}