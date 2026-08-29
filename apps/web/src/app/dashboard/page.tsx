"use client";

import { useEffect, useState } from "react";
import {
    fetchTenantAppointments,
    updateAppointmentStatus,
    AppointmentResult,
} from "@/lib/api";
import {
    Calendar,
    Clock,
    User,
    Phone,
    CheckCircle2,
    XCircle,
    AlertCircle,
    TrendingUp,
    RefreshCcw,
} from "lucide-react";

// Varsayılan işletme ID'miz (Burak Duygun Kuaför)
const DEMO_TENANT_ID = "00926e45-6412-49a6-acc4-05632aa9a9df";

export default function AdminDashboardPage() {
    const [appointments, setAppointments] = useState<AppointmentResult[]>([]);
    const [loading, setLoading] = useState(true);
    const [filter, setFilter] = useState<"ALL" | 1 | 2 | 3 | 4>("ALL");
    const [actionLoadingId, setActionLoadingId] = useState<string | null>(null);

    async function loadData() {
        try {
            setLoading(true);
            const data = await fetchTenantAppointments(DEMO_TENANT_ID);
            setAppointments(data);
        } catch (err) {
            console.error(err);
        } finally {
            setLoading(false);
        }
    }

    useEffect(() => {
        loadData();
    }, []);

    async function handleStatusChange(appointmentId: string, newStatus: number) {
        try {
            setActionLoadingId(appointmentId);
            await updateAppointmentStatus(DEMO_TENANT_ID, appointmentId, newStatus);
            await loadData();
        } catch (err) {
            alert("Durum güncellenirken bir hata oluştu.");
        } finally {
            setActionLoadingId(null);
        }
    }

    // İstatistikler
    const totalIncome = appointments
        .filter((a) => a.status === 2) // Confirmed
        .reduce((sum, a) => sum + a.price, 0);

    const pendingCount = appointments.filter((a) => a.status === 1).length;
    const confirmedCount = appointments.filter((a) => a.status === 2).length;

    const filteredAppointments = appointments.filter((a) => {
        if (filter === "ALL") return true;
        return a.status === filter;
    });

    const getStatusBadge = (status: number) => {
        switch (status) {
            case 1:
                return (
                    <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-semibold bg-amber-50 text-amber-600 border border-amber-200">
                        <AlertCircle className="w-3.5 h-3.5" /> Bekliyor
                    </span>
                );
            case 2:
                return (
                    <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-semibold bg-emerald-50 text-emerald-600 border border-emerald-200">
                        <CheckCircle2 className="w-3.5 h-3.5" /> Onaylandı
                    </span>
                );
            case 3:
                return (
                    <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-semibold bg-rose-50 text-rose-600 border border-rose-200">
                        <XCircle className="w-3.5 h-3.5" /> İptal Edildi
                    </span>
                );
            case 4:
                return (
                    <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-semibold bg-red-50 text-red-600 border border-red-200">
                        <XCircle className="w-3.5 h-3.5" /> Reddedildi
                    </span>
                );
            default:
                return null;
        }
    };

    return (
        <div className="min-h-screen bg-slate-50 text-slate-800 p-4 md:p-8">
            <div className="max-w-6xl mx-auto space-y-6">
                {/* Başlık ve Yenile */}
                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 bg-white p-6 rounded-3xl border border-slate-200/80 shadow-sm">
                    <div>
                        <h1 className="text-2xl font-bold text-slate-900 tracking-tight">
                            Yönetim Paneli
                        </h1>
                        <p className="text-sm text-slate-500 mt-1">
                            Burak Duygun Kuaför • Canlı Randevu Akışı & WhatsApp Yönetimi
                        </p>
                    </div>
                    <button
                        onClick={loadData}
                        disabled={loading}
                        className="inline-flex items-center gap-2 px-4 py-2.5 bg-slate-100 hover:bg-slate-200 text-slate-700 text-sm font-semibold rounded-2xl transition-all disabled:opacity-50"
                    >
                        <RefreshCcw className={`w-4 h-4 ${loading ? "animate-spin" : ""}`} />
                        Yenile
                    </button>
                </div>

                {/* Metrik Kartları */}
                <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
                    <div className="bg-white p-6 rounded-3xl border border-slate-200/80 shadow-sm flex items-center justify-between">
                        <div>
                            <p className="text-xs font-semibold uppercase tracking-wider text-slate-400">
                                Bekleyen Onaylar
                            </p>
                            <h3 className="text-3xl font-extrabold text-amber-500 mt-1">
                                {pendingCount}
                            </h3>
                        </div>
                        <div className="w-12 h-12 rounded-2xl bg-amber-50 flex items-center justify-center text-amber-500">
                            <AlertCircle className="w-6 h-6" />
                        </div>
                    </div>

                    <div className="bg-white p-6 rounded-3xl border border-slate-200/80 shadow-sm flex items-center justify-between">
                        <div>
                            <p className="text-xs font-semibold uppercase tracking-wider text-slate-400">
                                Onaylanan Randevular
                            </p>
                            <h3 className="text-3xl font-extrabold text-emerald-600 mt-1">
                                {confirmedCount}
                            </h3>
                        </div>
                        <div className="w-12 h-12 rounded-2xl bg-emerald-50 flex items-center justify-center text-emerald-600">
                            <CheckCircle2 className="w-6 h-6" />
                        </div>
                    </div>

                    <div className="bg-white p-6 rounded-3xl border border-slate-200/80 shadow-sm flex items-center justify-between">
                        <div>
                            <p className="text-xs font-semibold uppercase tracking-wider text-slate-400">
                                Tahmini Ciro (Onaylı)
                            </p>
                            <h3 className="text-3xl font-extrabold text-slate-900 mt-1">
                                {totalIncome.toLocaleString("tr-TR")} ₺
                            </h3>
                        </div>
                        <div className="w-12 h-12 rounded-2xl bg-slate-100 flex items-center justify-center text-slate-700">
                            <TrendingUp className="w-6 h-6" />
                        </div>
                    </div>
                </div>

                {/* Randevu Tablosu ve Filtreler */}
                <div className="bg-white rounded-3xl border border-slate-200/80 shadow-sm overflow-hidden">
                    {/* Filtre Butonları */}
                    <div className="p-4 border-b border-slate-100 flex items-center gap-2 overflow-x-auto">
                        {[
                            { label: "Tümü", val: "ALL" },
                            { label: "Bekleyenler", val: 1 },
                            { label: "Onaylananlar", val: 2 },
                            { label: "İptal / Red", val: 4 },
                        ].map((f) => (
                            <button
                                key={f.label}
                                onClick={() => setFilter(f.val as any)}
                                className={`px-4 py-2 rounded-xl text-xs font-semibold transition-all ${filter === f.val
                                        ? "bg-slate-900 text-white"
                                        : "bg-slate-100 text-slate-600 hover:bg-slate-200"
                                    }`}
                            >
                                {f.label}
                            </button>
                        ))}
                    </div>

                    {/* Tablo İçeriği */}
                    {loading ? (
                        <div className="py-16 text-center text-slate-400 text-sm">
                            Randevular listeleniyor...
                        </div>
                    ) : filteredAppointments.length === 0 ? (
                        <div className="py-16 text-center text-slate-400 text-sm">
                            Kayıtlı randevu bulunamadı.
                        </div>
                    ) : (
                        <div className="overflow-x-auto">
                            <table className="w-full text-left text-sm">
                                <thead className="bg-slate-50/70 text-slate-400 text-xs uppercase tracking-wider">
                                    <tr>
                                        <th className="py-4 px-6 font-semibold">Müşteri</th>
                                        <th className="py-4 px-6 font-semibold">Hizmet & Personel</th>
                                        <th className="py-4 px-6 font-semibold">Tarih / Saat</th>
                                        <th className="py-4 px-6 font-semibold">Tutar</th>
                                        <th className="py-4 px-6 font-semibold">Durum</th>
                                        <th className="py-4 px-6 font-semibold text-right">İşlemler</th>
                                    </tr>
                                </thead>
                                <tbody className="divide-y divide-slate-100">
                                    {filteredAppointments.map((app) => (
                                        <tr key={app.id} className="hover:bg-slate-50/50 transition-colors">
                                            <td className="py-4 px-6">
                                                <div className="font-semibold text-slate-800 flex items-center gap-2">
                                                    <User className="w-4 h-4 text-slate-400" />
                                                    {app.customerFullName}
                                                </div>
                                                <div className="text-xs text-slate-400 flex items-center gap-1 mt-0.5">
                                                    <Phone className="w-3 h-3" />
                                                    {app.customerPhoneNumber}
                                                </div>
                                            </td>
                                            <td className="py-4 px-6">
                                                <div className="font-medium text-slate-700">{app.serviceName}</div>
                                                <div className="text-xs text-slate-400">{app.staffName}</div>
                                            </td>
                                            <td className="py-4 px-6">
                                                <div className="flex items-center gap-1.5 text-slate-700 font-medium text-xs">
                                                    <Calendar className="w-3.5 h-3.5 text-slate-400" />
                                                    {new Date(app.startTimeUtc).toLocaleDateString("tr-TR")}
                                                </div>
                                                <div className="flex items-center gap-1.5 text-slate-500 text-xs mt-0.5">
                                                    <Clock className="w-3.5 h-3.5 text-slate-400" />
                                                    {new Date(app.startTimeUtc).toLocaleTimeString("tr-TR", {
                                                        hour: "2-digit",
                                                        minute: "2-digit",
                                                        timeZone: "UTC",
                                                    })}
                                                </div>
                                            </td>
                                            <td className="py-4 px-6 font-bold text-slate-800">
                                                {app.price} ₺
                                            </td>
                                            <td className="py-4 px-6">{getStatusBadge(app.status)}</td>
                                            <td className="py-4 px-6 text-right">
                                                {app.status === 1 ? (
                                                    <div className="flex items-center justify-end gap-2">
                                                        <button
                                                            disabled={actionLoadingId === app.id}
                                                            onClick={() => handleStatusChange(app.id, 2)}
                                                            className="px-3 py-1.5 bg-emerald-500 hover:bg-emerald-600 text-white rounded-xl text-xs font-semibold transition-all disabled:opacity-50"
                                                        >
                                                            Onayla
                                                        </button>
                                                        <button
                                                            disabled={actionLoadingId === app.id}
                                                            onClick={() => handleStatusChange(app.id, 4)}
                                                            className="px-3 py-1.5 bg-rose-50 hover:bg-rose-100 text-rose-600 rounded-xl text-xs font-semibold transition-all disabled:opacity-50"
                                                        >
                                                            Reddet
                                                        </button>
                                                    </div>
                                                ) : (
                                                    <span className="text-xs text-slate-400">İşlem Tamam</span>
                                                )}
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
}