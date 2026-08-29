"use client";

import { useEffect, useState } from "react";
import {
    fetchTenantAppointments,
    updateAppointmentStatus,
    fetchServices,
    fetchStaff,
    createService,
    deleteService,
    createStaff,
    deleteStaff,
    fetchWorkingHours,
    updateWorkingHours,
    AppointmentResult,
    ServiceItem,
    StaffItem,
    WorkingHourItem,
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
    Scissors,
    Users,
    Plus,
    Trash2,
} from "lucide-react";

const DEMO_TENANT_ID = "00926e45-6412-49a6-acc4-05632aa9a9df";

export default function AdminDashboardPage() {
    const [activeTab, setActiveTab] = useState<"appointments" | "services" | "staff" | "hours">("appointments");
    const [loading, setLoading] = useState(true);

    // Randevular
    const [appointments, setAppointments] = useState<AppointmentResult[]>([]);
    const [filter, setFilter] = useState<"ALL" | 1 | 2 | 3 | 4>("ALL");
    const [actionLoadingId, setActionLoadingId] = useState<string | null>(null);

    // Hizmetler & Personel
    const [services, setServices] = useState<ServiceItem[]>([]);
    const [staffList, setStaffList] = useState<StaffItem[]>([]);

    // Çalışma Saatleri & Tatiller
    const [workingHours, setWorkingHours] = useState<WorkingHourItem[]>([]);
    const [savingHours, setSavingHours] = useState(false);
    const dayNames = ["Pazar", "Pazartesi", "Salı", "Çarşamba", "Perşembe", "Cuma", "Cumartesi"];

    // Yeni Hizmet Form State
    const [newServiceName, setNewServiceName] = useState("");
    const [newServiceDesc, setNewServiceDesc] = useState("");
    const [newServiceDuration, setNewServiceDuration] = useState(30);
    const [newServicePrice, setNewServicePrice] = useState(300);

    // Yeni Personel Form State
    const [newStaffName, setNewStaffName] = useState("");
    const [newStaffTitle, setNewStaffTitle] = useState("");
    const [newStaffPhone, setNewStaffPhone] = useState("");

    async function loadData() {
        try {
            setLoading(true);
            const [appData, srvData, staffData, hoursData] = await Promise.all([
                fetchTenantAppointments(DEMO_TENANT_ID),
                fetchServices(DEMO_TENANT_ID),
                fetchStaff(DEMO_TENANT_ID),
                fetchWorkingHours(DEMO_TENANT_ID),
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

    useEffect(() => {
        loadData();
    }, []);

    async function handleStatusChange(appointmentId: string, newStatus: number) {
        try {
            setActionLoadingId(appointmentId);
            await updateAppointmentStatus(DEMO_TENANT_ID, appointmentId, newStatus);
            await loadData();
        } catch {
            alert("Durum güncellenemedi.");
        } finally {
            setActionLoadingId(null);
        }
    }

    async function handleAddService(e: React.FormEvent) {
        e.preventDefault();
        try {
            await createService(DEMO_TENANT_ID, {
                name: newServiceName,
                description: newServiceDesc,
                durationInMinutes: Number(newServiceDuration),
                price: Number(newServicePrice),
            });
            setNewServiceName("");
            setNewServiceDesc("");
            await loadData();
        } catch {
            alert("Hizmet eklenemedi.");
        }
    }

    async function handleDeleteService(id: string) {
        if (!confirm("Bu hizmeti kaldırmak istediğinize emin misiniz?")) return;
        try {
            await deleteService(DEMO_TENANT_ID, id);
            await loadData();
        } catch {
            alert("Hizmet silinemedi.");
        }
    }

    async function handleAddStaff(e: React.FormEvent) {
        e.preventDefault();
        try {
            await createStaff(DEMO_TENANT_ID, {
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
        if (!confirm("Bu personeli kaldırmak istediğinize emin misiniz?")) return;
        try {
            await deleteStaff(DEMO_TENANT_ID, id);
            await loadData();
        } catch {
            alert("Personel silinemedi.");
        }
    }

    async function handleSaveWorkingHours() {
        try {
            setSavingHours(true);
            await updateWorkingHours(DEMO_TENANT_ID, workingHours);
            alert("Çalışma saatleri başarıyla güncellendi!");
        } catch {
            alert("Çalışma saatleri kaydedilemedi.");
        } finally {
            setSavingHours(false);
        }
    }

    function handleHourChange(dayOfWeek: number, field: keyof WorkingHourItem, value: any) {
        setWorkingHours((prev) =>
            prev.map((item) => (item.dayOfWeek === dayOfWeek ? { ...item, [field]: value } : item))
        );
    }

    const totalIncome = appointments
        .filter((a) => a.status === 2)
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
                {/* Üst Bar */}
                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 bg-white p-6 rounded-3xl border border-slate-200/80 shadow-sm">
                    <div>
                        <h1 className="text-2xl font-bold text-slate-900 tracking-tight">Yönetim Paneli</h1>
                        <p className="text-sm text-slate-500 mt-1">Burak Duygun Kuaför • İşletme Yönetimi</p>
                    </div>
                    <div className="flex items-center gap-2">
                        <button
                            onClick={loadData}
                            disabled={loading}
                            className="inline-flex items-center gap-2 px-4 py-2.5 bg-slate-100 hover:bg-slate-200 text-slate-700 text-sm font-semibold rounded-2xl transition-all disabled:opacity-50"
                        >
                            <RefreshCcw className={`w-4 h-4 ${loading ? "animate-spin" : ""}`} />
                            Yenile
                        </button>
                    </div>
                </div>

                {/* Sekmeler (Tabs) */}
                <div className="flex items-center gap-2 border-b border-slate-200 pb-2 overflow-x-auto">
                    <button
                        onClick={() => setActiveTab("appointments")}
                        className={`flex items-center gap-2 px-5 py-2.5 rounded-2xl text-sm font-semibold transition-all whitespace-nowrap ${activeTab === "appointments" ? "bg-slate-900 text-white shadow-sm" : "bg-white text-slate-600 hover:bg-slate-100"
                            }`}
                    >
                        <Calendar className="w-4 h-4" /> Randevular
                    </button>
                    <button
                        onClick={() => setActiveTab("services")}
                        className={`flex items-center gap-2 px-5 py-2.5 rounded-2xl text-sm font-semibold transition-all whitespace-nowrap ${activeTab === "services" ? "bg-slate-900 text-white shadow-sm" : "bg-white text-slate-600 hover:bg-slate-100"
                            }`}
                    >
                        <Scissors className="w-4 h-4" /> Hizmetler ({services.length})
                    </button>
                    <button
                        onClick={() => setActiveTab("staff")}
                        className={`flex items-center gap-2 px-5 py-2.5 rounded-2xl text-sm font-semibold transition-all whitespace-nowrap ${activeTab === "staff" ? "bg-slate-900 text-white shadow-sm" : "bg-white text-slate-600 hover:bg-slate-100"
                            }`}
                    >
                        <Users className="w-4 h-4" /> Ekip ({staffList.length})
                    </button>
                    <button
                        onClick={() => setActiveTab("hours")}
                        className={`flex items-center gap-2 px-5 py-2.5 rounded-2xl text-sm font-semibold transition-all whitespace-nowrap ${activeTab === "hours" ? "bg-slate-900 text-white shadow-sm" : "bg-white text-slate-600 hover:bg-slate-100"
                            }`}
                    >
                        <Clock className="w-4 h-4" /> Çalışma Saatleri & Tatiller
                    </button>
                </div>

                {/* TAB 1: RANDEVULAR */}
                {activeTab === "appointments" && (
                    <div className="space-y-6">
                        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
                            <div className="bg-white p-6 rounded-3xl border border-slate-200/80 shadow-sm flex items-center justify-between">
                                <div>
                                    <p className="text-xs font-semibold uppercase tracking-wider text-slate-400">Bekleyen Onaylar</p>
                                    <h3 className="text-3xl font-extrabold text-amber-500 mt-1">{pendingCount}</h3>
                                </div>
                                <div className="w-12 h-12 rounded-2xl bg-amber-50 flex items-center justify-center text-amber-500">
                                    <AlertCircle className="w-6 h-6" />
                                </div>
                            </div>

                            <div className="bg-white p-6 rounded-3xl border border-slate-200/80 shadow-sm flex items-center justify-between">
                                <div>
                                    <p className="text-xs font-semibold uppercase tracking-wider text-slate-400">Onaylanan Randevular</p>
                                    <h3 className="text-3xl font-extrabold text-emerald-600 mt-1">{confirmedCount}</h3>
                                </div>
                                <div className="w-12 h-12 rounded-2xl bg-emerald-50 flex items-center justify-center text-emerald-600">
                                    <CheckCircle2 className="w-6 h-6" />
                                </div>
                            </div>

                            <div className="bg-white p-6 rounded-3xl border border-slate-200/80 shadow-sm flex items-center justify-between">
                                <div>
                                    <p className="text-xs font-semibold uppercase tracking-wider text-slate-400">Tahmini Ciro (Onaylı)</p>
                                    <h3 className="text-3xl font-extrabold text-slate-900 mt-1">{totalIncome.toLocaleString("tr-TR")} ₺</h3>
                                </div>
                                <div className="w-12 h-12 rounded-2xl bg-slate-100 flex items-center justify-center text-slate-700">
                                    <TrendingUp className="w-6 h-6" />
                                </div>
                            </div>
                        </div>

                        <div className="bg-white rounded-3xl border border-slate-200/80 shadow-sm overflow-hidden">
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
                                        className={`px-4 py-2 rounded-xl text-xs font-semibold transition-all ${filter === f.val ? "bg-slate-900 text-white" : "bg-slate-100 text-slate-600 hover:bg-slate-200"
                                            }`}
                                    >
                                        {f.label}
                                    </button>
                                ))}
                            </div>

                            {loading ? (
                                <div className="py-16 text-center text-slate-400 text-sm">Randevular yükleniyor...</div>
                            ) : filteredAppointments.length === 0 ? (
                                <div className="py-16 text-center text-slate-400 text-sm">Kayıtlı randevu bulunamadı.</div>
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
                                                    <td className="py-4 px-6 font-bold text-slate-800">{app.price} ₺</td>
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
                )}

                {/* TAB 2: HİZMET YÖNETİMİ */}
                {activeTab === "services" && (
                    <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                        <div className="bg-white p-6 rounded-3xl border border-slate-200/80 shadow-sm h-fit">
                            <h2 className="text-lg font-bold text-slate-900 mb-4 flex items-center gap-2">
                                <Plus className="w-5 h-5 text-emerald-500" /> Yeni Hizmet Ekle
                            </h2>
                            <form onSubmit={handleAddService} className="space-y-4">
                                <div>
                                    <label className="block text-xs font-semibold text-slate-600 mb-1">Hizmet Adı</label>
                                    <input
                                        required
                                        type="text"
                                        placeholder="Örn: Sakal Tıraşı & Buhar"
                                        value={newServiceName}
                                        onChange={(e) => setNewServiceName(e.target.value)}
                                        className="w-full p-3 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-emerald-500"
                                    />
                                </div>
                                <div>
                                    <label className="block text-xs font-semibold text-slate-600 mb-1">Açıklama</label>
                                    <input
                                        type="text"
                                        placeholder="Örn: Sıcak havlu ve cilt bakımı dahil"
                                        value={newServiceDesc}
                                        onChange={(e) => setNewServiceDesc(e.target.value)}
                                        className="w-full p-3 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-emerald-500"
                                    />
                                </div>
                                <div className="grid grid-cols-2 gap-3">
                                    <div>
                                        <label className="block text-xs font-semibold text-slate-600 mb-1">Süre (Dk)</label>
                                        <input
                                            required
                                            type="number"
                                            step={5}
                                            value={newServiceDuration}
                                            onChange={(e) => setNewServiceDuration(Number(e.target.value))}
                                            className="w-full p-3 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-emerald-500"
                                        />
                                    </div>
                                    <div>
                                        <label className="block text-xs font-semibold text-slate-600 mb-1">Fiyat (₺)</label>
                                        <input
                                            required
                                            type="number"
                                            value={newServicePrice}
                                            onChange={(e) => setNewServicePrice(Number(e.target.value))}
                                            className="w-full p-3 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-emerald-500"
                                        />
                                    </div>
                                </div>
                                <button
                                    type="submit"
                                    className="w-full py-3 bg-slate-900 hover:bg-slate-800 text-white rounded-xl text-sm font-semibold transition-all"
                                >
                                    Hizmeti Kaydet
                                </button>
                            </form>
                        </div>

                        <div className="md:col-span-2 space-y-3">
                            {services.map((s) => (
                                <div
                                    key={s.id}
                                    className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-sm flex items-center justify-between hover:border-slate-300 transition-all"
                                >
                                    <div>
                                        <h3 className="font-semibold text-slate-800">{s.name}</h3>
                                        <p className="text-xs text-slate-500 mt-0.5">
                                            {s.durationInMinutes} Dakika • {s.description || "Açıklama yok"}
                                        </p>
                                    </div>
                                    <div className="flex items-center gap-4">
                                        <span className="font-bold text-slate-900 text-base">{s.price} ₺</span>
                                        <button
                                            onClick={() => handleDeleteService(s.id)}
                                            className="p-2 text-slate-400 hover:text-rose-500 rounded-lg hover:bg-rose-50 transition-colors"
                                            title="Sil"
                                        >
                                            <Trash2 className="w-4 h-4" />
                                        </button>
                                    </div>
                                </div>
                            ))}
                        </div>
                    </div>
                )}

                {/* TAB 3: EKİP YÖNETİMİ */}
                {activeTab === "staff" && (
                    <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                        <div className="bg-white p-6 rounded-3xl border border-slate-200/80 shadow-sm h-fit">
                            <h2 className="text-lg font-bold text-slate-900 mb-4 flex items-center gap-2">
                                <Plus className="w-5 h-5 text-emerald-500" /> Yeni Personel Ekle
                            </h2>
                            <form onSubmit={handleAddStaff} className="space-y-4">
                                <div>
                                    <label className="block text-xs font-semibold text-slate-600 mb-1">Ad Soyad</label>
                                    <input
                                        required
                                        type="text"
                                        placeholder="Örn: Caner Yıldız"
                                        value={newStaffName}
                                        onChange={(e) => setNewStaffName(e.target.value)}
                                        className="w-full p-3 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-emerald-500"
                                    />
                                </div>
                                <div>
                                    <label className="block text-xs font-semibold text-slate-600 mb-1">Unvan / Uzmanlık</label>
                                    <input
                                        required
                                        type="text"
                                        placeholder="Örn: Renklendirme Uzmanı"
                                        value={newStaffTitle}
                                        onChange={(e) => setNewStaffTitle(e.target.value)}
                                        className="w-full p-3 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-emerald-500"
                                    />
                                </div>
                                <div>
                                    <label className="block text-xs font-semibold text-slate-600 mb-1">Telefon Numarası</label>
                                    <input
                                        required
                                        type="tel"
                                        placeholder="905551112233"
                                        value={newStaffPhone}
                                        onChange={(e) => setNewStaffPhone(e.target.value)}
                                        className="w-full p-3 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-emerald-500"
                                    />
                                </div>
                                <button
                                    type="submit"
                                    className="w-full py-3 bg-slate-900 hover:bg-slate-800 text-white rounded-xl text-sm font-semibold transition-all"
                                >
                                    Personeli Kaydet
                                </button>
                            </form>
                        </div>

                        <div className="md:col-span-2 space-y-3">
                            {staffList.map((st) => (
                                <div
                                    key={st.id}
                                    className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-sm flex items-center justify-between hover:border-slate-300 transition-all"
                                >
                                    <div className="flex items-center gap-3">
                                        <div className="w-10 h-10 rounded-full bg-slate-100 flex items-center justify-center font-bold text-slate-700">
                                            {st.fullName[0]}
                                        </div>
                                        <div>
                                            <h3 className="font-semibold text-slate-800">{st.fullName}</h3>
                                            <p className="text-xs text-slate-500 mt-0.5">{st.title} • {st.phoneNumber}</p>
                                        </div>
                                    </div>
                                    <button
                                        onClick={() => handleDeleteStaff(st.id)}
                                        className="p-2 text-slate-400 hover:text-rose-500 rounded-lg hover:bg-rose-50 transition-colors"
                                        title="Sil"
                                    >
                                        <Trash2 className="w-4 h-4" />
                                    </button>
                                </div>
                            ))}
                        </div>
                    </div>
                )}

                {/* TAB 4: ÇALIŞMA SAATLERİ & TATİLLER */}
                {activeTab === "hours" && (
                    <div className="bg-white p-6 md:p-8 rounded-3xl border border-slate-200/80 shadow-sm space-y-6">
                        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
                            <div>
                                <h2 className="text-lg font-bold text-slate-900">Haftalık Çalışma & Tatil Planı</h2>
                                <p className="text-xs text-slate-500 mt-0.5">
                                    Kapalı işaretlediğiniz günlerde müşteriler sistem üzerinden randevu slotu seçemez.
                                </p>
                            </div>
                            <button
                                onClick={handleSaveWorkingHours}
                                disabled={savingHours}
                                className="px-5 py-2.5 bg-emerald-500 hover:bg-emerald-600 text-white text-xs font-semibold rounded-2xl transition-all shadow-md shadow-emerald-500/20 disabled:opacity-50"
                            >
                                {savingHours ? "Kaydediliyor..." : "Değişiklikleri Kaydet"}
                            </button>
                        </div>

                        <div className="divide-y divide-slate-100">
                            {workingHours.map((wh) => (
                                <div
                                    key={wh.dayOfWeek}
                                    className="py-4 flex flex-col sm:flex-row sm:items-center justify-between gap-4"
                                >
                                    <div className="w-36">
                                        <span className="font-semibold text-slate-800 text-sm">
                                            {dayNames[wh.dayOfWeek]}
                                        </span>
                                    </div>

                                    <div className="flex items-center gap-4 flex-1">
                                        <div className="flex items-center gap-2">
                                            <label className="text-xs text-slate-400 font-medium">Açılış:</label>
                                            <input
                                                type="time"
                                                disabled={wh.isClosed}
                                                value={wh.openingTime.slice(0, 5)}
                                                onChange={(e) => handleHourChange(wh.dayOfWeek, "openingTime", e.target.value)}
                                                className="px-3 py-1.5 bg-slate-50 border border-slate-200 rounded-xl text-xs font-medium disabled:opacity-40"
                                            />
                                        </div>

                                        <span className="text-slate-300">-</span>

                                        <div className="flex items-center gap-2">
                                            <label className="text-xs text-slate-400 font-medium">Kapanış:</label>
                                            <input
                                                type="time"
                                                disabled={wh.isClosed}
                                                value={wh.closingTime.slice(0, 5)}
                                                onChange={(e) => handleHourChange(wh.dayOfWeek, "closingTime", e.target.value)}
                                                className="px-3 py-1.5 bg-slate-50 border border-slate-200 rounded-xl text-xs font-medium disabled:opacity-40"
                                            />
                                        </div>
                                    </div>

                                    <div className="flex items-center gap-2">
                                        <label className="relative inline-flex items-center cursor-pointer">
                                            <input
                                                type="checkbox"
                                                checked={wh.isClosed}
                                                onChange={(e) => handleHourChange(wh.dayOfWeek, "isClosed", e.target.checked)}
                                                className="sr-only peer"
                                            />
                                            <div className="w-9 h-5 bg-slate-200 peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-slate-300 after:border after:rounded-full after:h-4 after:w-4 after:transition-all peer-checked:bg-rose-500"></div>
                                            <span className={`ml-2 text-xs font-semibold ${wh.isClosed ? "text-rose-600" : "text-slate-500"}`}>
                                                {wh.isClosed ? "Kapalı / Tatil" : "Açık"}
                                            </span>
                                        </label>
                                    </div>
                                </div>
                            ))}
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
}