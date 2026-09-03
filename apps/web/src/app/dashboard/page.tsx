"use client";

import { useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import {
    AlertCircle,
    CalendarDays,
    CheckCircle2,
    Clock3,
    LogOut,
    MessageCircle,
    Plus,
    RefreshCcw,
    Scissors,
    Settings2,
    Trash2,
    TrendingUp,
    UserRound,
    Users,
    XCircle,
} from "lucide-react";
import {
    AppointmentResult,
    ServiceItem,
    StaffItem,
    WorkingHourItem,
    createService,
    createStaff,
    deleteService,
    deleteStaff,
    fetchServices,
    fetchStaff,
    fetchTenantAppointments,
    fetchWorkingHours,
    updateAppointmentStatus,
    updateWorkingHours,
} from "@/lib/api";

type Tab = "appointments" | "services" | "staff" | "hours";
type Filter = "ALL" | 1 | 2 | 3 | 4;

export default function AdminDashboardPage() {
    const router = useRouter();

    const [tenantId, setTenantId] = useState("");
    const [businessOwner, setBusinessOwner] = useState("");
    const [activeTab, setActiveTab] = useState<Tab>("appointments");
    const [loading, setLoading] = useState(true);

    const [appointments, setAppointments] = useState<AppointmentResult[]>([]);
    const [filter, setFilter] = useState<Filter>("ALL");
    const [actionLoadingId, setActionLoadingId] = useState<string | null>(null);

    const [services, setServices] = useState<ServiceItem[]>([]);
    const [staffList, setStaffList] = useState<StaffItem[]>([]);
    const [workingHours, setWorkingHours] = useState<WorkingHourItem[]>([]);
    const [savingHours, setSavingHours] = useState(false);

    const [newServiceName, setNewServiceName] = useState("");
    const [newServiceDesc, setNewServiceDesc] = useState("");
    const [newServiceDuration, setNewServiceDuration] = useState(30);
    const [newServicePrice, setNewServicePrice] = useState(300);

    const [newStaffName, setNewStaffName] = useState("");
    const [newStaffTitle, setNewStaffTitle] = useState("");
    const [newStaffPhone, setNewStaffPhone] = useState("");

    const dayNames = [
        "Pazar",
        "Pazartesi",
        "Salı",
        "Çarşamba",
        "Perşembe",
        "Cuma",
        "Cumartesi",
    ];

    useEffect(() => {
        const token = localStorage.getItem("auth_token");
        const userJson = localStorage.getItem("user_info");

        if (!token || !userJson) {
            router.push("/login");
            return;
        }

        try {
            const user = JSON.parse(userJson);
            setTenantId(user.tenantId);
            setBusinessOwner(user.fullName);
            loadData(user.tenantId);
        } catch {
            localStorage.removeItem("auth_token");
            localStorage.removeItem("user_info");
            router.push("/login");
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    async function loadData(targetTenantId?: string) {
        const tId = targetTenantId || tenantId;
        if (!tId) return;

        try {
            setLoading(true);
            const [appData, srvData, staffData, hoursData] = await Promise.all([
                fetchTenantAppointments(tId),
                fetchServices(tId),
                fetchStaff(tId),
                fetchWorkingHours(tId),
            ]);

            setAppointments(appData);
            setServices(srvData);
            setStaffList(staffData);
            setWorkingHours(hoursData);
        } catch (err) {
            console.error(err);
        } finally {
            setLoading(false);
        }
    }

    function handleLogout() {
        localStorage.removeItem("auth_token");
        localStorage.removeItem("user_info");
        router.push("/login");
    }

    async function handleStatusChange(appointmentId: string, newStatus: number) {
        try {
            setActionLoadingId(appointmentId);
            await updateAppointmentStatus(tenantId, appointmentId, newStatus);
            await loadData();
        } catch {
            alert("Randevu durumu güncellenemedi.");
        } finally {
            setActionLoadingId(null);
        }
    }

    async function handleAddService(e: React.FormEvent) {
        e.preventDefault();

        try {
            await createService(tenantId, {
                name: newServiceName,
                description: newServiceDesc,
                durationInMinutes: Number(newServiceDuration),
                price: Number(newServicePrice),
            });

            setNewServiceName("");
            setNewServiceDesc("");
            setNewServiceDuration(30);
            setNewServicePrice(300);
            await loadData();
        } catch {
            alert("Hizmet eklenemedi.");
        }
    }

    async function handleDeleteService(id: string) {
        if (!confirm("Bu hizmeti silmek istediğinize emin misiniz?")) return;

        try {
            await deleteService(tenantId, id);
            await loadData();
        } catch {
            alert("Hizmet silinemedi.");
        }
    }

    async function handleAddStaff(e: React.FormEvent) {
        e.preventDefault();

        try {
            await createStaff(tenantId, {
                fullName: newStaffName,
                title: newStaffTitle,
                phoneNumber: newStaffPhone,
            });

            setNewStaffName("");
            setNewStaffTitle("");
            setNewStaffPhone("");
            await loadData();
        } catch {
            alert("Personel eklenemedi.");
        }
    }

    async function handleDeleteStaff(id: string) {
        if (!confirm("Bu personeli silmek istediğinize emin misiniz?")) return;

        try {
            await deleteStaff(tenantId, id);
            await loadData();
        } catch {
            alert("Personel silinemedi.");
        }
    }

    async function handleSaveWorkingHours() {
        try {
            setSavingHours(true);
            await updateWorkingHours(tenantId, workingHours);
            alert("Çalışma saatleri başarıyla güncellendi.");
        } catch {
            alert("Çalışma saatleri kaydedilemedi.");
        } finally {
            setSavingHours(false);
        }
    }

    function handleHourChange(
        dayOfWeek: number,
        field: keyof WorkingHourItem,
        value: unknown
    ) {
        setWorkingHours((prev) =>
            prev.map((item) =>
                item.dayOfWeek === dayOfWeek ? { ...item, [field]: value } : item
            )
        );
    }

    const stats = useMemo(() => {
        const pending = appointments.filter((a) => a.status === 1).length;
        const confirmed = appointments.filter((a) => a.status === 2).length;
        const cancelled = appointments.filter(
            (a) => a.status === 3 || a.status === 4
        ).length;
        const income = appointments
            .filter((a) => a.status === 2)
            .reduce((sum, a) => sum + a.price, 0);

        return { pending, confirmed, cancelled, income };
    }, [appointments]);

    const filteredAppointments = useMemo(
        () =>
            appointments.filter((a) =>
                filter === "ALL" ? true : a.status === filter
            ),
        [appointments, filter]
    );

    const navItems: {
        key: Tab;
        label: string;
        icon: typeof CalendarDays;
        count?: number;
    }[] = [
            {
                key: "appointments",
                label: "Randevular",
                icon: CalendarDays,
                count: appointments.length,
            },
            {
                key: "services",
                label: "Hizmetler",
                icon: Scissors,
                count: services.length,
            },
            {
                key: "staff",
                label: "Ekip",
                icon: Users,
                count: staffList.length,
            },
            {
                key: "hours",
                label: "Çalışma saatleri",
                icon: Clock3,
            },
        ];

    const getStatusBadge = (status: number) => {
        const config = {
            1: {
                label: "Bekliyor",
                className: "bg-amber-50 text-amber-700 border-amber-200",
                icon: AlertCircle,
            },
            2: {
                label: "Onaylandı",
                className: "bg-emerald-50 text-emerald-700 border-emerald-200",
                icon: CheckCircle2,
            },
            3: {
                label: "İptal edildi",
                className: "bg-slate-100 text-slate-600 border-slate-200",
                icon: XCircle,
            },
            4: {
                label: "Reddedildi",
                className: "bg-rose-50 text-rose-700 border-rose-200",
                icon: XCircle,
            },
        }[status as 1 | 2 | 3 | 4];

        if (!config) return null;

        const Icon = config.icon;

        return (
            <span
                className={`inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1.5 text-[11px] font-bold ${config.className}`}
            >
                <Icon className="h-3.5 w-3.5" />
                {config.label}
            </span>
        );
    };

    return (
        <div className="min-h-screen bg-[#f7f8f7] text-slate-900">
            {/* Sidebar */}
            <aside className="fixed inset-y-0 left-0 z-40 hidden w-64 border-r border-slate-200 bg-white lg:flex lg:flex-col">
                <div className="flex h-20 items-center gap-3 border-b border-slate-100 px-6">
                    <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-slate-950 text-sm font-black text-white">
                        R<span className="text-emerald-400">.</span>
                    </div>
                    <div>
                        <div className="text-base font-black tracking-[-0.04em]">
                            randevo<span className="text-emerald-500">.</span>
                        </div>
                        <p className="text-[9px] font-semibold uppercase tracking-[0.14em] text-slate-400">
                            Business
                        </p>
                    </div>
                </div>

                <div className="flex-1 px-3 py-6">
                    <p className="px-3 text-[10px] font-black uppercase tracking-[0.18em] text-slate-400">
                        Yönetim
                    </p>

                    <nav className="mt-3 space-y-1">
                        {navItems.map((item) => {
                            const Icon = item.icon;
                            const active = activeTab === item.key;

                            return (
                                <button
                                    key={item.key}
                                    onClick={() => setActiveTab(item.key)}
                                    className={`flex w-full items-center gap-3 rounded-xl px-3 py-3 text-left text-xs font-bold transition ${active
                                            ? "bg-slate-950 text-white shadow-lg shadow-slate-950/10"
                                            : "text-slate-500 hover:bg-slate-50 hover:text-slate-900"
                                        }`}
                                >
                                    <Icon className="h-4 w-4" />
                                    <span className="flex-1">{item.label}</span>
                                    {item.count !== undefined && (
                                        <span
                                            className={`rounded-full px-2 py-0.5 text-[9px] ${active
                                                    ? "bg-white/10 text-white"
                                                    : "bg-slate-100 text-slate-400"
                                                }`}
                                        >
                                            {item.count}
                                        </span>
                                    )}
                                </button>
                            );
                        })}
                    </nav>

                    <p className="mt-8 px-3 text-[10px] font-black uppercase tracking-[0.18em] text-slate-400">
                        Sistem
                    </p>

                    <div className="mt-3 rounded-2xl border border-emerald-100 bg-emerald-50/70 p-4">
                        <div className="flex items-center gap-2">
                            <span className="h-2 w-2 rounded-full bg-emerald-500 shadow-sm shadow-emerald-500/50" />
                            <span className="text-[11px] font-bold text-emerald-800">
                                Sistem aktif
                            </span>
                        </div>
                        <p className="mt-2 text-[10px] leading-4 text-emerald-700/70">
                            Randevo servisleri çalışıyor.
                        </p>
                    </div>
                </div>

                <div className="border-t border-slate-100 p-4">
                    <div className="mb-3 flex items-center gap-3 rounded-xl bg-slate-50 p-3">
                        <div className="flex h-9 w-9 items-center justify-center rounded-full bg-slate-950 text-xs font-black text-emerald-400">
                            {(businessOwner?.[0] || "İ").toUpperCase()}
                        </div>
                        <div className="min-w-0">
                            <p className="truncate text-xs font-bold text-slate-800">
                                {businessOwner || "İşletme Yetkilisi"}
                            </p>
                            <p className="text-[10px] text-slate-400">İşletme sahibi</p>
                        </div>
                    </div>

                    <button
                        onClick={handleLogout}
                        className="flex w-full items-center gap-2 rounded-xl px-3 py-2.5 text-xs font-bold text-slate-500 transition hover:bg-rose-50 hover:text-rose-600"
                    >
                        <LogOut className="h-4 w-4" />
                        Çıkış yap
                    </button>
                </div>
            </aside>

            {/* Main */}
            <div className="lg:pl-64">
                {/* Topbar */}
                <header className="sticky top-0 z-30 border-b border-slate-200/70 bg-[#f7f8f7]/90 backdrop-blur-xl">
                    <div className="mx-auto flex h-16 max-w-7xl items-center justify-between px-5 lg:px-8">
                        <div className="lg:hidden">
                            <div className="text-lg font-black tracking-[-0.04em]">
                                randevo<span className="text-emerald-500">.</span>
                            </div>
                        </div>

                        <div className="hidden lg:block">
                            <p className="text-[10px] font-black uppercase tracking-[0.18em] text-slate-400">
                                İşletme paneli
                            </p>
                            <p className="mt-0.5 text-xs font-bold text-slate-700">
                                Genel yönetim ve randevu operasyonları
                            </p>
                        </div>

                        <div className="flex items-center gap-2">
                            <button
                                onClick={() => loadData()}
                                disabled={loading}
                                title="Verileri yenile"
                                className="flex h-9 w-9 items-center justify-center rounded-xl border border-slate-200 bg-white text-slate-500 shadow-sm transition hover:text-slate-900 disabled:opacity-50"
                            >
                                <RefreshCcw
                                    className={`h-4 w-4 ${loading ? "animate-spin" : ""}`}
                                />
                            </button>

                            <div className="hidden h-9 items-center gap-2 rounded-xl border border-slate-200 bg-white px-3 text-[10px] font-bold text-slate-500 sm:flex">
                                <span className="h-1.5 w-1.5 rounded-full bg-emerald-500" />
                                Çevrimiçi
                            </div>

                            <button
                                onClick={handleLogout}
                                className="flex h-9 items-center gap-2 rounded-xl bg-white px-3 text-[10px] font-bold text-slate-500 shadow-sm ring-1 ring-slate-200 transition hover:text-rose-600"
                            >
                                <LogOut className="h-3.5 w-3.5" />
                                <span className="hidden sm:inline">Çıkış</span>
                            </button>
                        </div>
                    </div>
                </header>

                <main className="mx-auto max-w-7xl px-5 py-7 lg:px-8 lg:py-9">
                    {/* Mobile navigation */}
                    <div className="mb-6 flex gap-2 overflow-x-auto pb-1 lg:hidden">
                        {navItems.map((item) => {
                            const Icon = item.icon;
                            return (
                                <button
                                    key={item.key}
                                    onClick={() => setActiveTab(item.key)}
                                    className={`flex shrink-0 items-center gap-2 rounded-xl px-3.5 py-2.5 text-[11px] font-bold ${activeTab === item.key
                                            ? "bg-slate-950 text-white"
                                            : "border border-slate-200 bg-white text-slate-500"
                                        }`}
                                >
                                    <Icon className="h-3.5 w-3.5" />
                                    {item.label}
                                </button>
                            );
                        })}
                    </div>

                    {/* Page heading */}
                    <div className="mb-7 flex flex-col justify-between gap-4 sm:flex-row sm:items-end">
                        <div>
                            <p className="text-xs font-bold text-emerald-600">
                                Hoş geldiniz 👋
                            </p>
                            <h1 className="mt-1 text-3xl font-black tracking-[-0.045em] text-slate-950">
                                {activeTab === "appointments"
                                    ? "Bugünün randevuları"
                                    : activeTab === "services"
                                        ? "Hizmetler"
                                        : activeTab === "staff"
                                            ? "Ekibiniz"
                                            : "Çalışma saatleri"}
                            </h1>
                            <p className="mt-2 text-xs text-slate-500">
                                {businessOwner || "İşletme Yetkilisi"} · İşletme yönetimi
                            </p>
                        </div>

                        {activeTab === "appointments" && stats.pending > 0 && (
                            <button
                                onClick={() => setFilter(1)}
                                className="inline-flex items-center gap-2 self-start rounded-xl bg-amber-50 px-3.5 py-2.5 text-[11px] font-bold text-amber-700 ring-1 ring-amber-200 sm:self-auto"
                            >
                                <AlertCircle className="h-4 w-4" />
                                {stats.pending} randevu onay bekliyor
                            </button>
                        )}
                    </div>

                    {/* APPOINTMENTS */}
                    {activeTab === "appointments" && (
                        <div className="space-y-6">
                            <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
                                <StatCard
                                    label="Toplam randevu"
                                    value={appointments.length}
                                    icon={CalendarDays}
                                    tone="slate"
                                />
                                <StatCard
                                    label="Bekleyen"
                                    value={stats.pending}
                                    icon={AlertCircle}
                                    tone="amber"
                                />
                                <StatCard
                                    label="Onaylanan"
                                    value={stats.confirmed}
                                    icon={CheckCircle2}
                                    tone="emerald"
                                />
                                <StatCard
                                    label="Onaylı ciro"
                                    value={`${stats.income.toLocaleString("tr-TR")} ₺`}
                                    icon={TrendingUp}
                                    tone="dark"
                                />
                            </div>

                            <section className="overflow-hidden rounded-3xl border border-slate-200 bg-white shadow-sm">
                                <div className="flex flex-col gap-3 border-b border-slate-100 p-4 sm:flex-row sm:items-center sm:justify-between sm:px-5">
                                    <div>
                                        <h2 className="text-sm font-black text-slate-900">
                                            Randevu akışı
                                        </h2>
                                        <p className="mt-1 text-[10px] text-slate-400">
                                            Müşterilerin gönderdiği randevu taleplerini yönet.
                                        </p>
                                    </div>

                                    <div className="flex gap-1.5 overflow-x-auto">
                                        {[
                                            { label: "Tümü", val: "ALL" as const },
                                            { label: "Bekleyen", val: 1 as const },
                                            { label: "Onaylanan", val: 2 as const },
                                            { label: "İptal", val: 3 as const },
                                            { label: "Red", val: 4 as const },
                                        ].map((item) => (
                                            <button
                                                key={item.label}
                                                onClick={() => setFilter(item.val)}
                                                className={`shrink-0 rounded-lg px-2.5 py-2 text-[10px] font-bold transition ${filter === item.val
                                                        ? "bg-slate-950 text-white"
                                                        : "bg-slate-100 text-slate-500 hover:bg-slate-200"
                                                    }`}
                                            >
                                                {item.label}
                                            </button>
                                        ))}
                                    </div>
                                </div>

                                {loading ? (
                                    <div className="space-y-3 p-5">
                                        {[1, 2, 3, 4].map((item) => (
                                            <div
                                                key={item}
                                                className="h-16 animate-pulse rounded-2xl bg-slate-50"
                                            />
                                        ))}
                                    </div>
                                ) : filteredAppointments.length === 0 ? (
                                    <div className="px-6 py-16 text-center">
                                        <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-2xl bg-slate-100">
                                            <CalendarDays className="h-5 w-5 text-slate-400" />
                                        </div>
                                        <p className="mt-4 text-sm font-bold text-slate-600">
                                            Bu filtrede randevu bulunamadı.
                                        </p>
                                        <p className="mt-1 text-xs text-slate-400">
                                            Yeni randevular geldiğinde burada görünecek.
                                        </p>
                                    </div>
                                ) : (
                                    <>
                                        {/* Desktop table */}
                                        <div className="hidden overflow-x-auto md:block">
                                            <table className="w-full text-left">
                                                <thead className="border-b border-slate-100 bg-slate-50/60">
                                                    <tr className="text-[9px] font-black uppercase tracking-[0.12em] text-slate-400">
                                                        <th className="px-5 py-3.5">Müşteri</th>
                                                        <th className="px-5 py-3.5">Hizmet</th>
                                                        <th className="px-5 py-3.5">Tarih / Saat</th>
                                                        <th className="px-5 py-3.5">Tutar</th>
                                                        <th className="px-5 py-3.5">Durum</th>
                                                        <th className="px-5 py-3.5 text-right">İşlem</th>
                                                    </tr>
                                                </thead>
                                                <tbody className="divide-y divide-slate-100">
                                                    {filteredAppointments.map((app) => (
                                                        <tr
                                                            key={app.id}
                                                            className="transition hover:bg-slate-50/60"
                                                        >
                                                            <td className="px-5 py-4">
                                                                <div className="flex items-center gap-3">
                                                                    <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-slate-950 text-xs font-black text-emerald-400">
                                                                        {app.customerFullName?.[0]?.toUpperCase()}
                                                                    </div>
                                                                    <div className="min-w-0">
                                                                        <p className="truncate text-xs font-bold text-slate-800">
                                                                            {app.customerFullName}
                                                                        </p>
                                                                        <p className="mt-0.5 text-[10px] text-slate-400">
                                                                            {app.customerPhoneNumber}
                                                                        </p>
                                                                    </div>
                                                                </div>
                                                            </td>
                                                            <td className="px-5 py-4">
                                                                <p className="text-xs font-bold text-slate-700">
                                                                    {app.serviceName}
                                                                </p>
                                                                <p className="mt-0.5 text-[10px] text-slate-400">
                                                                    {app.staffName}
                                                                </p>
                                                            </td>
                                                            <td className="px-5 py-4">
                                                                <p className="text-xs font-bold text-slate-700">
                                                                    {new Date(
                                                                        app.startTimeUtc
                                                                    ).toLocaleDateString("tr-TR")}
                                                                </p>
                                                                <p className="mt-0.5 text-[10px] text-slate-400">
                                                                    {new Date(
                                                                        app.startTimeUtc
                                                                    ).toLocaleTimeString("tr-TR", {
                                                                        hour: "2-digit",
                                                                        minute: "2-digit",
                                                                        timeZone: "UTC",
                                                                    })}
                                                                </p>
                                                            </td>
                                                            <td className="px-5 py-4 text-xs font-black text-slate-900">
                                                                {app.price.toLocaleString("tr-TR")} ₺
                                                            </td>
                                                            <td className="px-5 py-4">
                                                                {getStatusBadge(app.status)}
                                                            </td>
                                                            <td className="px-5 py-4 text-right">
                                                                {app.status === 1 ? (
                                                                    <div className="flex justify-end gap-2">
                                                                        <button
                                                                            disabled={actionLoadingId === app.id}
                                                                            onClick={() =>
                                                                                handleStatusChange(app.id, 2)
                                                                            }
                                                                            className="rounded-lg bg-emerald-500 px-3 py-2 text-[10px] font-bold text-white transition hover:bg-emerald-600 disabled:opacity-50"
                                                                        >
                                                                            Onayla
                                                                        </button>
                                                                        <button
                                                                            disabled={actionLoadingId === app.id}
                                                                            onClick={() =>
                                                                                handleStatusChange(app.id, 4)
                                                                            }
                                                                            className="rounded-lg bg-rose-50 px-3 py-2 text-[10px] font-bold text-rose-600 transition hover:bg-rose-100 disabled:opacity-50"
                                                                        >
                                                                            Reddet
                                                                        </button>
                                                                    </div>
                                                                ) : (
                                                                    <span className="text-[10px] font-semibold text-slate-300">
                                                                        Tamamlandı
                                                                    </span>
                                                                )}
                                                            </td>
                                                        </tr>
                                                    ))}
                                                </tbody>
                                            </table>
                                        </div>

                                        {/* Mobile cards */}
                                        <div className="space-y-3 p-4 md:hidden">
                                            {filteredAppointments.map((app) => (
                                                <div
                                                    key={app.id}
                                                    className="rounded-2xl border border-slate-100 bg-slate-50/50 p-4"
                                                >
                                                    <div className="flex items-start justify-between gap-3">
                                                        <div className="flex min-w-0 items-center gap-3">
                                                            <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-slate-950 text-xs font-black text-emerald-400">
                                                                {app.customerFullName?.[0]?.toUpperCase()}
                                                            </div>
                                                            <div className="min-w-0">
                                                                <p className="truncate text-xs font-bold">
                                                                    {app.customerFullName}
                                                                </p>
                                                                <p className="mt-1 text-[10px] text-slate-400">
                                                                    {app.customerPhoneNumber}
                                                                </p>
                                                            </div>
                                                        </div>
                                                        {getStatusBadge(app.status)}
                                                    </div>

                                                    <div className="mt-4 grid grid-cols-2 gap-2 rounded-xl bg-white p-3">
                                                        <InfoCell
                                                            label="Hizmet"
                                                            value={app.serviceName}
                                                        />
                                                        <InfoCell label="Personel" value={app.staffName} />
                                                        <InfoCell
                                                            label="Tarih"
                                                            value={new Date(
                                                                app.startTimeUtc
                                                            ).toLocaleDateString("tr-TR")}
                                                        />
                                                        <InfoCell
                                                            label="Saat"
                                                            value={new Date(
                                                                app.startTimeUtc
                                                            ).toLocaleTimeString("tr-TR", {
                                                                hour: "2-digit",
                                                                minute: "2-digit",
                                                                timeZone: "UTC",
                                                            })}
                                                        />
                                                    </div>

                                                    <div className="mt-3 flex items-center justify-between">
                                                        <span className="text-sm font-black">
                                                            {app.price.toLocaleString("tr-TR")} ₺
                                                        </span>

                                                        {app.status === 1 && (
                                                            <div className="flex gap-2">
                                                                <button
                                                                    disabled={actionLoadingId === app.id}
                                                                    onClick={() =>
                                                                        handleStatusChange(app.id, 2)
                                                                    }
                                                                    className="rounded-lg bg-emerald-500 px-3 py-2 text-[10px] font-bold text-white disabled:opacity-50"
                                                                >
                                                                    Onayla
                                                                </button>
                                                                <button
                                                                    disabled={actionLoadingId === app.id}
                                                                    onClick={() =>
                                                                        handleStatusChange(app.id, 4)
                                                                    }
                                                                    className="rounded-lg bg-rose-50 px-3 py-2 text-[10px] font-bold text-rose-600 disabled:opacity-50"
                                                                >
                                                                    Reddet
                                                                </button>
                                                            </div>
                                                        )}
                                                    </div>
                                                </div>
                                            ))}
                                        </div>
                                    </>
                                )}
                            </section>

                            <div className="grid gap-4 lg:grid-cols-2">
                                <div className="rounded-3xl border border-slate-200 bg-slate-950 p-5 text-white">
                                    <div className="flex items-center gap-2">
                                        <MessageCircle className="h-4 w-4 text-emerald-400" />
                                        <p className="text-xs font-bold">Bildirim altyapısı</p>
                                    </div>
                                    <p className="mt-3 text-sm font-black">
                                        WhatsApp bildirimleri hazır.
                                    </p>
                                    <p className="mt-1 text-[10px] leading-5 text-slate-400">
                                        Randevu oluşturma ve durum değişikliklerinde bildirim
                                        servisi tetikleniyor.
                                    </p>
                                    <div className="mt-4 flex items-center gap-2 text-[10px] font-bold text-emerald-400">
                                        <span className="h-1.5 w-1.5 rounded-full bg-emerald-400" />
                                        Notification service aktif
                                    </div>
                                </div>

                                <div className="rounded-3xl border border-slate-200 bg-white p-5">
                                    <div className="flex items-center gap-2">
                                        <Settings2 className="h-4 w-4 text-slate-400" />
                                        <p className="text-xs font-bold">Hızlı özet</p>
                                    </div>
                                    <div className="mt-4 grid grid-cols-2 gap-3">
                                        <MiniMetric label="İptal / Red" value={stats.cancelled} />
                                        <MiniMetric
                                            label="Aktif hizmet"
                                            value={services.length}
                                        />
                                        <MiniMetric label="Ekip üyesi" value={staffList.length} />
                                        <MiniMetric
                                            label="Açık gün"
                                            value={workingHours.filter((x) => !x.isClosed).length}
                                        />
                                    </div>
                                </div>
                            </div>
                        </div>
                    )}

                    {/* SERVICES */}
                    {activeTab === "services" && (
                        <div className="grid gap-5 lg:grid-cols-[340px_1fr]">
                            <section className="rounded-3xl border border-slate-200 bg-white p-5 shadow-sm">
                                <div className="mb-5 flex items-center gap-3">
                                    <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-slate-950 text-emerald-400">
                                        <Plus className="h-4 w-4" />
                                    </div>
                                    <div>
                                        <h2 className="text-sm font-black">Yeni hizmet</h2>
                                        <p className="mt-0.5 text-[10px] text-slate-400">
                                            Müşterilerine sunduğun hizmeti ekle.
                                        </p>
                                    </div>
                                </div>

                                <form onSubmit={handleAddService} className="space-y-4">
                                    <Field
                                        label="Hizmet adı"
                                        value={newServiceName}
                                        onChange={setNewServiceName}
                                        placeholder="Örn. Saç Kesimi"
                                        required
                                    />
                                    <Field
                                        label="Açıklama"
                                        value={newServiceDesc}
                                        onChange={setNewServiceDesc}
                                        placeholder="Kısa bir açıklama"
                                    />

                                    <div className="grid grid-cols-2 gap-3">
                                        <NumberField
                                            label="Süre"
                                            suffix="dk"
                                            value={newServiceDuration}
                                            onChange={setNewServiceDuration}
                                        />
                                        <NumberField
                                            label="Fiyat"
                                            suffix="₺"
                                            value={newServicePrice}
                                            onChange={setNewServicePrice}
                                        />
                                    </div>

                                    <button
                                        type="submit"
                                        className="w-full rounded-xl bg-slate-950 py-3 text-xs font-bold text-white transition hover:bg-slate-800"
                                    >
                                        Hizmeti kaydet
                                    </button>
                                </form>
                            </section>

                            <section className="space-y-3">
                                <div className="flex items-center justify-between px-1">
                                    <div>
                                        <h2 className="text-sm font-black">
                                            Hizmet listeniz{" "}
                                            <span className="text-slate-400">
                                                ({services.length})
                                            </span>
                                        </h2>
                                        <p className="mt-1 text-[10px] text-slate-400">
                                            Müşteri randevu ekranında gösterilecek hizmetler.
                                        </p>
                                    </div>
                                </div>

                                {services.length === 0 ? (
                                    <EmptyState
                                        icon={Scissors}
                                        title="Henüz hizmet eklenmemiş"
                                        text="Soldaki formdan ilk hizmetini oluştur."
                                    />
                                ) : (
                                    services.map((service) => (
                                        <div
                                            key={service.id}
                                            className="group flex items-center gap-4 rounded-2xl border border-slate-200 bg-white p-4 shadow-sm transition hover:border-emerald-200 hover:shadow-md"
                                        >
                                            <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-emerald-50 text-emerald-600">
                                                <Scissors className="h-4 w-4" />
                                            </div>
                                            <div className="min-w-0 flex-1">
                                                <h3 className="truncate text-xs font-black text-slate-800">
                                                    {service.name}
                                                </h3>
                                                <p className="mt-1 truncate text-[10px] text-slate-400">
                                                    {service.durationInMinutes} dk ·{" "}
                                                    {service.description || "Açıklama yok"}
                                                </p>
                                            </div>
                                            <div className="flex items-center gap-3">
                                                <span className="text-sm font-black text-slate-900">
                                                    {service.price.toLocaleString("tr-TR")} ₺
                                                </span>
                                                <button
                                                    onClick={() => handleDeleteService(service.id)}
                                                    className="flex h-8 w-8 items-center justify-center rounded-lg text-slate-300 transition hover:bg-rose-50 hover:text-rose-500"
                                                    title="Hizmeti sil"
                                                >
                                                    <Trash2 className="h-4 w-4" />
                                                </button>
                                            </div>
                                        </div>
                                    ))
                                )}
                            </section>
                        </div>
                    )}

                    {/* STAFF */}
                    {activeTab === "staff" && (
                        <div className="grid gap-5 lg:grid-cols-[340px_1fr]">
                            <section className="rounded-3xl border border-slate-200 bg-white p-5 shadow-sm">
                                <div className="mb-5 flex items-center gap-3">
                                    <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-slate-950 text-emerald-400">
                                        <Plus className="h-4 w-4" />
                                    </div>
                                    <div>
                                        <h2 className="text-sm font-black">Yeni ekip üyesi</h2>
                                        <p className="mt-0.5 text-[10px] text-slate-400">
                                            Personelini randevu sistemine ekle.
                                        </p>
                                    </div>
                                </div>

                                <form onSubmit={handleAddStaff} className="space-y-4">
                                    <Field
                                        label="Ad soyad"
                                        value={newStaffName}
                                        onChange={setNewStaffName}
                                        placeholder="Örn. Ayşe Yılmaz"
                                        required
                                    />
                                    <Field
                                        label="Uzmanlık / unvan"
                                        value={newStaffTitle}
                                        onChange={setNewStaffTitle}
                                        placeholder="Örn. Saç tasarım uzmanı"
                                        required
                                    />
                                    <Field
                                        label="Telefon"
                                        value={newStaffPhone}
                                        onChange={setNewStaffPhone}
                                        placeholder="905551112233"
                                        type="tel"
                                        required
                                    />

                                    <button
                                        type="submit"
                                        className="w-full rounded-xl bg-slate-950 py-3 text-xs font-bold text-white transition hover:bg-slate-800"
                                    >
                                        Personeli kaydet
                                    </button>
                                </form>
                            </section>

                            <section className="space-y-3">
                                <div className="px-1">
                                    <h2 className="text-sm font-black">
                                        Ekibiniz{" "}
                                        <span className="text-slate-400">({staffList.length})</span>
                                    </h2>
                                    <p className="mt-1 text-[10px] text-slate-400">
                                        Müşteriler randevu sırasında ekip üyelerini görebilir.
                                    </p>
                                </div>

                                {staffList.length === 0 ? (
                                    <EmptyState
                                        icon={Users}
                                        title="Henüz ekip üyesi yok"
                                        text="Soldaki formdan ilk personelini ekle."
                                    />
                                ) : (
                                    staffList.map((staff) => (
                                        <div
                                            key={staff.id}
                                            className="flex items-center gap-4 rounded-2xl border border-slate-200 bg-white p-4 shadow-sm transition hover:border-emerald-200 hover:shadow-md"
                                        >
                                            <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-full bg-slate-950 text-sm font-black text-emerald-400">
                                                {staff.fullName?.[0]?.toUpperCase()}
                                            </div>
                                            <div className="min-w-0 flex-1">
                                                <h3 className="truncate text-xs font-black text-slate-800">
                                                    {staff.fullName}
                                                </h3>
                                                <p className="mt-1 truncate text-[10px] text-slate-400">
                                                    {staff.title} · {staff.phoneNumber}
                                                </p>
                                            </div>
                                            <button
                                                onClick={() => handleDeleteStaff(staff.id)}
                                                className="flex h-8 w-8 items-center justify-center rounded-lg text-slate-300 transition hover:bg-rose-50 hover:text-rose-500"
                                                title="Personeli sil"
                                            >
                                                <Trash2 className="h-4 w-4" />
                                            </button>
                                        </div>
                                    ))
                                )}
                            </section>
                        </div>
                    )}

                    {/* HOURS */}
                    {activeTab === "hours" && (
                        <section className="overflow-hidden rounded-3xl border border-slate-200 bg-white shadow-sm">
                            <div className="flex flex-col justify-between gap-4 border-b border-slate-100 p-5 sm:flex-row sm:items-center sm:px-6">
                                <div>
                                    <div className="flex items-center gap-2">
                                        <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-slate-950 text-emerald-400">
                                            <Clock3 className="h-4 w-4" />
                                        </div>
                                        <h2 className="text-sm font-black">
                                            Haftalık çalışma planı
                                        </h2>
                                    </div>
                                    <p className="mt-2 text-[10px] leading-5 text-slate-400">
                                        Kapalı günlerde müşteriler randevu slotu göremez.
                                    </p>
                                </div>

                                <button
                                    onClick={handleSaveWorkingHours}
                                    disabled={savingHours}
                                    className="rounded-xl bg-slate-950 px-4 py-2.5 text-[10px] font-bold text-white transition hover:bg-slate-800 disabled:opacity-50"
                                >
                                    {savingHours ? "Kaydediliyor..." : "Değişiklikleri kaydet"}
                                </button>
                            </div>

                            <div className="divide-y divide-slate-100">
                                {workingHours.map((hour) => (
                                    <div
                                        key={hour.dayOfWeek}
                                        className="flex flex-col gap-4 px-5 py-5 sm:flex-row sm:items-center sm:px-6"
                                    >
                                        <div className="flex w-28 items-center gap-2">
                                            <span
                                                className={`h-2 w-2 rounded-full ${hour.isClosed ? "bg-slate-300" : "bg-emerald-500"
                                                    }`}
                                            />
                                            <span className="text-xs font-bold text-slate-700">
                                                {dayNames[hour.dayOfWeek]}
                                            </span>
                                        </div>

                                        <div className="flex flex-1 flex-wrap items-center gap-3">
                                            <TimeField
                                                label="Açılış"
                                                value={hour.openingTime.slice(0, 5)}
                                                disabled={hour.isClosed}
                                                onChange={(value) =>
                                                    handleHourChange(
                                                        hour.dayOfWeek,
                                                        "openingTime",
                                                        value
                                                    )
                                                }
                                            />
                                            <span className="text-slate-300">—</span>
                                            <TimeField
                                                label="Kapanış"
                                                value={hour.closingTime.slice(0, 5)}
                                                disabled={hour.isClosed}
                                                onChange={(value) =>
                                                    handleHourChange(
                                                        hour.dayOfWeek,
                                                        "closingTime",
                                                        value
                                                    )
                                                }
                                            />
                                        </div>

                                        <label className="flex cursor-pointer items-center gap-2">
                                            <input
                                                type="checkbox"
                                                checked={hour.isClosed}
                                                onChange={(e) =>
                                                    handleHourChange(
                                                        hour.dayOfWeek,
                                                        "isClosed",
                                                        e.target.checked
                                                    )
                                                }
                                                className="h-4 w-4 rounded border-slate-300 text-emerald-500 accent-emerald-500"
                                            />
                                            <span
                                                className={`text-[10px] font-bold ${hour.isClosed ? "text-slate-500" : "text-emerald-600"
                                                    }`}
                                            >
                                                {hour.isClosed ? "Kapalı / Tatil" : "Açık"}
                                            </span>
                                        </label>
                                    </div>
                                ))}
                            </div>
                        </section>
                    )}
                </main>
            </div>
        </div>
    );
}

function StatCard({
    label,
    value,
    icon: Icon,
    tone,
}: {
    label: string;
    value: string | number;
    icon: typeof CalendarDays;
    tone: "slate" | "amber" | "emerald" | "dark";
}) {
    const styles = {
        slate: "bg-white border-slate-200 text-slate-900",
        amber: "bg-amber-50/70 border-amber-100 text-amber-700",
        emerald: "bg-emerald-50/70 border-emerald-100 text-emerald-700",
        dark: "bg-slate-950 border-slate-950 text-white",
    };

    return (
        <div className={`rounded-2xl border p-4 shadow-sm ${styles[tone]}`}>
            <div className="flex items-start justify-between gap-3">
                <p
                    className={`text-[9px] font-black uppercase tracking-[0.12em] ${tone === "dark" ? "text-slate-400" : "text-slate-400"
                        }`}
                >
                    {label}
                </p>
                <Icon
                    className={`h-4 w-4 ${tone === "dark" ? "text-emerald-400" : "opacity-60"
                        }`}
                />
            </div>
            <p className="mt-3 text-2xl font-black tracking-[-0.04em]">{value}</p>
        </div>
    );
}

function InfoCell({ label, value }: { label: string; value: string }) {
    return (
        <div>
            <p className="text-[9px] font-bold uppercase tracking-wider text-slate-300">
                {label}
            </p>
            <p className="mt-1 truncate text-[10px] font-bold text-slate-600">
                {value}
            </p>
        </div>
    );
}

function MiniMetric({
    label,
    value,
}: {
    label: string;
    value: string | number;
}) {
    return (
        <div className="rounded-xl bg-slate-50 p-3">
            <p className="text-[9px] font-bold text-slate-400">{label}</p>
            <p className="mt-1 text-lg font-black">{value}</p>
        </div>
    );
}

function EmptyState({
    icon: Icon,
    title,
    text,
}: {
    icon: typeof Scissors;
    title: string;
    text: string;
}) {
    return (
        <div className="rounded-3xl border border-dashed border-slate-300 bg-white px-6 py-16 text-center">
            <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-2xl bg-slate-100">
                <Icon className="h-5 w-5 text-slate-400" />
            </div>
            <p className="mt-4 text-sm font-bold text-slate-600">{title}</p>
            <p className="mt-1 text-xs text-slate-400">{text}</p>
        </div>
    );
}

function Field({
    label,
    value,
    onChange,
    placeholder,
    required,
    type = "text",
}: {
    label: string;
    value: string;
    onChange: (value: string) => void;
    placeholder: string;
    required?: boolean;
    type?: string;
}) {
    return (
        <label className="block">
            <span className="mb-1.5 block text-[10px] font-black uppercase tracking-wider text-slate-500">
                {label}
            </span>
            <input
                required={required}
                type={type}
                value={value}
                onChange={(e) => onChange(e.target.value)}
                placeholder={placeholder}
                className="w-full rounded-xl border border-slate-200 bg-slate-50 px-3.5 py-3 text-xs font-medium text-slate-800 outline-none transition placeholder:text-slate-300 focus:border-emerald-400 focus:bg-white focus:ring-4 focus:ring-emerald-500/10"
            />
        </label>
    );
}

function NumberField({
    label,
    suffix,
    value,
    onChange,
}: {
    label: string;
    suffix: string;
    value: number;
    onChange: (value: number) => void;
}) {
    return (
        <label className="block">
            <span className="mb-1.5 block text-[10px] font-black uppercase tracking-wider text-slate-500">
                {label}
            </span>
            <div className="relative">
                <input
                    required
                    type="number"
                    min={0}
                    value={value}
                    onChange={(e) => onChange(Number(e.target.value))}
                    className="w-full rounded-xl border border-slate-200 bg-slate-50 px-3.5 py-3 pr-10 text-xs font-bold outline-none transition focus:border-emerald-400 focus:bg-white focus:ring-4 focus:ring-emerald-500/10"
                />
                <span className="absolute right-3 top-1/2 -translate-y-1/2 text-[10px] font-bold text-slate-400">
                    {suffix}
                </span>
            </div>
        </label>
    );
}

function TimeField({
    label,
    value,
    onChange,
    disabled,
}: {
    label: string;
    value: string;
    onChange: (value: string) => void;
    disabled: boolean;
}) {
    return (
        <label className="flex items-center gap-2">
            <span className="text-[9px] font-bold uppercase tracking-wider text-slate-400">
                {label}
            </span>
            <input
                type="time"
                disabled={disabled}
                value={value}
                onChange={(e) => onChange(e.target.value)}
                className="rounded-xl border border-slate-200 bg-slate-50 px-3 py-2 text-xs font-bold text-slate-700 outline-none transition focus:border-emerald-400 focus:bg-white disabled:cursor-not-allowed disabled:opacity-40"
            />
        </label>
    );
}
