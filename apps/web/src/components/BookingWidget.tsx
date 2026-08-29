"use client";

import { useState, useEffect } from "react";
import {
    Tenant,
    ServiceItem,
    StaffItem,
    TimeSlot,
    fetchServices,
    fetchStaff,
    fetchAvailableSlots,
    createAppointment,
    AppointmentResult,
} from "@/lib/api";
import { Scissors, User, Calendar, Clock, CheckCircle2, ChevronRight, ArrowLeft } from "lucide-react";

interface Props {
    tenant: Tenant;
}

export default function BookingWidget({ tenant }: Props) {
    const [step, setStep] = useState<1 | 2 | 3 | 4 | 5>(1);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    // Veri Listeleri
    const [services, setServices] = useState<ServiceItem[]>([]);
    const [staffList, setStaffList] = useState<StaffItem[]>([]);
    const [slots, setSlots] = useState<TimeSlot[]>([]);

    // Seçim Durumları
    const [selectedService, setSelectedService] = useState<ServiceItem | null>(null);
    const [selectedStaff, setSelectedStaff] = useState<StaffItem | null>(null);
    const [selectedDate, setSelectedDate] = useState<string>("2026-08-30");
    const [selectedSlot, setSelectedSlot] = useState<TimeSlot | null>(null);

    // Müşteri Form Bilgileri
    const [customerName, setCustomerName] = useState("");
    const [customerPhone, setCustomerPhone] = useState("");
    const [customerNotes, setCustomerNotes] = useState("");

    const [createdAppointment, setCreatedAppointment] = useState<AppointmentResult | null>(null);

    // İlk yüklemede hizmetleri ve personeli çek
    useEffect(() => {
        async function init() {
            try {
                setLoading(true);
                const [srvs, stf] = await Promise.all([
                    fetchServices(tenant.id),
                    fetchStaff(tenant.id),
                ]);
                setServices(srvs);
                setStaffList(stf);
            } catch (err: any) {
                setError(err.message);
            } finally {
                setLoading(false);
            }
        }
        init();
    }, [tenant.id]);

    // Personel veya tarih değiştiğinde slotları güncelle
    useEffect(() => {
        if (selectedService && selectedStaff && selectedDate) {
            loadSlots();
        }
    }, [selectedService, selectedStaff, selectedDate]);

    async function loadSlots() {
        if (!selectedService || !selectedStaff) return;
        try {
            setLoading(true);
            setError(null);
            const res = await fetchAvailableSlots(
                tenant.id,
                selectedService.id,
                selectedStaff.id,
                `${selectedDate}T00:00:00Z`
            );
            setSlots(res);
            setSelectedSlot(null);
        } catch (err: any) {
            setError(err.message);
        } finally {
            setLoading(false);
        }
    }

    async function handleSubmit(e: React.FormEvent) {
        e.preventDefault();
        if (!selectedService || !selectedStaff || !selectedSlot) return;

        try {
            setLoading(true);
            setError(null);
            const result = await createAppointment(tenant.id, {
                serviceId: selectedService.id,
                staffId: selectedStaff.id,
                startTimeUtc: selectedSlot.startTimeUtc,
                customerFullName: customerName,
                customerPhoneNumber: customerPhone,
                customerNotes: customerNotes,
            });
            setCreatedAppointment(result);
            setStep(5); // Başarı adımı
        } catch (err: any) {
            setError(err.message);
        } finally {
            setLoading(false);
        }
    }

    return (
        <div className="max-w-xl mx-auto bg-white rounded-3xl shadow-xl border border-slate-100 overflow-hidden">
            {/* Üst Başlık */}
            <div className="bg-slate-900 text-white p-6">
                <div className="flex items-center justify-between">
                    <div>
                        <span className="text-xs font-semibold tracking-wider uppercase text-emerald-400">
                            Online Randevu
                        </span>
                        <h1 className="text-2xl font-bold mt-0.5">{tenant.name}</h1>
                    </div>
                    <div className="h-10 w-10 rounded-full bg-slate-800 flex items-center justify-center text-emerald-400 font-bold border border-slate-700">
                        {step}/4
                    </div>
                </div>

                {/* Adım Çizgisi */}
                {step < 5 && (
                    <div className="grid grid-cols-4 gap-2 mt-6">
                        {[1, 2, 3, 4].map((i) => (
                            <div
                                key={i}
                                className={`h-1.5 rounded-full transition-all duration-300 ${i <= step ? "bg-emerald-400" : "bg-slate-800"
                                    }`}
                            />
                        ))}
                    </div>
                )}
            </div>

            <div className="p-6">
                {error && (
                    <div className="mb-6 p-4 rounded-xl bg-red-50 text-red-600 text-sm border border-red-100 flex items-center gap-2">
                        ⚠️ {error}
                    </div>
                )}

                {/* ADIM 1: HİZMET SEÇİMİ */}
                {step === 1 && (
                    <div className="space-y-4">
                        <h2 className="text-lg font-semibold text-slate-800 flex items-center gap-2">
                            <Scissors className="w-5 h-5 text-emerald-500" /> Hizmet Seçin
                        </h2>
                        <div className="grid gap-3">
                            {services.map((s) => (
                                <button
                                    key={s.id}
                                    onClick={() => {
                                        setSelectedService(s);
                                        setStep(2);
                                    }}
                                    className={`p-4 rounded-2xl border text-left flex items-center justify-between transition-all ${selectedService?.id === s.id
                                            ? "border-emerald-500 bg-emerald-50/40 ring-2 ring-emerald-500/20"
                                            : "border-slate-200 hover:border-slate-300 hover:bg-slate-50/60"
                                        }`}
                                >
                                    <div>
                                        <p className="font-semibold text-slate-800">{s.name}</p>
                                        <p className="text-xs text-slate-500 mt-0.5">
                                            {s.durationInMinutes} Dakika • {s.description}
                                        </p>
                                    </div>
                                    <div className="text-right">
                                        <span className="font-bold text-slate-900">{s.price} ₺</span>
                                        <ChevronRight className="w-4 h-4 text-slate-400 ml-auto mt-1" />
                                    </div>
                                </button>
                            ))}
                        </div>
                    </div>
                )}

                {/* ADIM 2: PERSONEL SEÇİMİ */}
                {step === 2 && (
                    <div className="space-y-4">
                        <button
                            onClick={() => setStep(1)}
                            className="text-xs font-medium text-slate-500 hover:text-slate-800 flex items-center gap-1 mb-2"
                        >
                            <ArrowLeft className="w-3.5 h-3.5" /> Hizmet Değiştir
                        </button>
                        <h2 className="text-lg font-semibold text-slate-800 flex items-center gap-2">
                            <User className="w-5 h-5 text-emerald-500" /> Personel Tercihi
                        </h2>
                        <div className="grid gap-3">
                            {staffList.map((st) => (
                                <button
                                    key={st.id}
                                    onClick={() => {
                                        setSelectedStaff(st);
                                        setStep(3);
                                    }}
                                    className={`p-4 rounded-2xl border text-left flex items-center justify-between transition-all ${selectedStaff?.id === st.id
                                            ? "border-emerald-500 bg-emerald-50/40 ring-2 ring-emerald-500/20"
                                            : "border-slate-200 hover:border-slate-300 hover:bg-slate-50/60"
                                        }`}
                                >
                                    <div className="flex items-center gap-3">
                                        <div className="w-10 h-10 rounded-full bg-slate-100 flex items-center justify-center font-bold text-slate-700">
                                            {st.fullName[0]}
                                        </div>
                                        <div>
                                            <p className="font-semibold text-slate-800">{st.fullName}</p>
                                            <p className="text-xs text-slate-500">{st.title}</p>
                                        </div>
                                    </div>
                                    <ChevronRight className="w-4 h-4 text-slate-400" />
                                </button>
                            ))}
                        </div>
                    </div>
                )}

                {/* ADIM 3: TARİH & SAAT SLOTU */}
                {step === 3 && (
                    <div className="space-y-4">
                        <button
                            onClick={() => setStep(2)}
                            className="text-xs font-medium text-slate-500 hover:text-slate-800 flex items-center gap-1 mb-2"
                        >
                            <ArrowLeft className="w-3.5 h-3.5" /> Personel Değiştir
                        </button>
                        <h2 className="text-lg font-semibold text-slate-800 flex items-center gap-2">
                            <Calendar className="w-5 h-5 text-emerald-500" /> Tarih ve Saat Seçimi
                        </h2>

                        {/* Tarih Girişi */}
                        <div>
                            <input
                                type="date"
                                value={selectedDate}
                                onChange={(e) => setSelectedDate(e.target.value)}
                                className="w-full p-3 bg-slate-50 border border-slate-200 rounded-xl text-sm font-medium text-slate-700 focus:outline-none focus:ring-2 focus:ring-emerald-500"
                            />
                        </div>

                        {/* Slotlar */}
                        <div className="pt-2">
                            <p className="text-xs font-semibold text-slate-400 uppercase tracking-wider mb-2 flex items-center gap-1.5">
                                <Clock className="w-3.5 h-3.5" /> Müsait Saat Aralıkları
                            </p>
                            {loading ? (
                                <div className="py-8 text-center text-sm text-slate-400">Saatler yükleniyor...</div>
                            ) : slots.length === 0 ? (
                                <div className="py-8 text-center text-sm text-slate-400">Bu tarihe uygun slot bulunamadı.</div>
                            ) : (
                                <div className="grid grid-cols-3 gap-2">
                                    {slots.map((sl, idx) => {
                                        const startLabel = new Date(sl.startTimeUtc).toLocaleTimeString("tr-TR", {
                                            hour: "2-digit",
                                            minute: "2-digit",
                                            timeZone: "UTC",
                                        });
                                        return (
                                            <button
                                                key={idx}
                                                disabled={!sl.isAvailable}
                                                onClick={() => setSelectedSlot(sl)}
                                                className={`py-2.5 px-3 rounded-xl text-xs font-medium transition-all ${!sl.isAvailable
                                                        ? "bg-slate-100 text-slate-400 cursor-not-allowed line-through"
                                                        : selectedSlot === sl
                                                            ? "bg-emerald-500 text-white shadow-md shadow-emerald-500/20 font-bold"
                                                            : "bg-white border border-slate-200 text-slate-700 hover:border-emerald-500"
                                                    }`}
                                            >
                                                {startLabel}
                                            </button>
                                        );
                                    })}
                                </div>
                            )}
                        </div>

                        {selectedSlot && (
                            <button
                                onClick={() => setStep(4)}
                                className="w-full mt-4 py-3 bg-slate-900 hover:bg-slate-800 text-white rounded-xl text-sm font-semibold transition-all flex items-center justify-center gap-2"
                            >
                                Bilgileri Doldur <ChevronRight className="w-4 h-4" />
                            </button>
                        )}
                    </div>
                )}

                {/* ADIM 4: MÜŞTERİ BİLGİ FORMU */}
                {step === 4 && (
                    <div className="space-y-4">
                        <button
                            onClick={() => setStep(3)}
                            className="text-xs font-medium text-slate-500 hover:text-slate-800 flex items-center gap-1 mb-2"
                        >
                            <ArrowLeft className="w-3.5 h-3.5" /> Saat Seçimine Dön
                        </button>
                        <h2 className="text-lg font-semibold text-slate-800">Bilgilerinizi Girin</h2>

                        <form onSubmit={handleSubmit} className="space-y-3">
                            <div>
                                <label className="block text-xs font-medium text-slate-600 mb-1">Ad Soyad</label>
                                <input
                                    required
                                    type="text"
                                    placeholder="Örn: Gökçe Soylu"
                                    value={customerName}
                                    onChange={(e) => setCustomerName(e.target.value)}
                                    className="w-full p-3 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-emerald-500"
                                />
                            </div>

                            <div>
                                <label className="block text-xs font-medium text-slate-600 mb-1">Telefon Numarası</label>
                                <input
                                    required
                                    type="tel"
                                    placeholder="905551112233"
                                    value={customerPhone}
                                    onChange={(e) => setCustomerPhone(e.target.value)}
                                    className="w-full p-3 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-emerald-500"
                                />
                            </div>

                            <div>
                                <label className="block text-xs font-medium text-slate-600 mb-1">Özel Not (Opsiyonel)</label>
                                <textarea
                                    rows={2}
                                    placeholder="İstediğiniz özel bir stil var mı?"
                                    value={customerNotes}
                                    onChange={(e) => setCustomerNotes(e.target.value)}
                                    className="w-full p-3 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-emerald-500"
                                />
                            </div>

                            {/* Randevu Özeti */}
                            <div className="p-3 bg-slate-50 rounded-xl border border-slate-100 text-xs text-slate-600 space-y-1 mt-2">
                                <p><strong>Hizmet:</strong> {selectedService?.name} ({selectedService?.price} ₺)</p>
                                <p><strong>Personel:</strong> {selectedStaff?.fullName}</p>
                                <p>
                                    <strong>Zaman:</strong> {selectedDate} •{" "}
                                    {selectedSlot &&
                                        new Date(selectedSlot.startTimeUtc).toLocaleTimeString("tr-TR", {
                                            hour: "2-digit",
                                            minute: "2-digit",
                                            timeZone: "UTC",
                                        })}
                                </p>
                            </div>

                            <button
                                type="submit"
                                disabled={loading}
                                className="w-full py-3.5 bg-emerald-500 hover:bg-emerald-600 text-white rounded-xl text-sm font-semibold shadow-lg shadow-emerald-500/25 transition-all mt-4 disabled:opacity-50"
                            >
                                {loading ? "Talebiniz İletiliyor..." : "Randevu Talebini Gönder"}
                            </button>
                        </form>
                    </div>
                )}

                {/* ADIM 5: ONAY VE BİLGİLENDİRME KARTI */}
                {step === 5 && createdAppointment && (
                    <div className="text-center py-6 space-y-4">
                        <div className="w-16 h-16 bg-emerald-100 text-emerald-600 rounded-full flex items-center justify-center mx-auto">
                            <CheckCircle2 className="w-10 h-10" />
                        </div>
                        <h2 className="text-xl font-bold text-slate-800">Randevu Talebiniz Alındı!</h2>
                        <p className="text-sm text-slate-500 max-w-sm mx-auto">
                            İşletme talebinizi onayladığında <strong>WhatsApp</strong> üzerinden size anında bilgilendirme iletilecektir.
                        </p>

                        <div className="p-4 bg-slate-50 rounded-2xl border border-slate-100 text-left text-xs text-slate-700 space-y-1.5">
                            <p><strong>İşletme:</strong> {tenant.name}</p>
                            <p><strong>Hizmet:</strong> {createdAppointment.serviceName}</p>
                            <p><strong>Personel:</strong> {createdAppointment.staffName}</p>
                            <p><strong>Tutar:</strong> {createdAppointment.price} ₺</p>
                        </div>

                        <button
                            onClick={() => {
                                setStep(1);
                                setSelectedService(null);
                                setSelectedStaff(null);
                                setSelectedSlot(null);
                                setCustomerName("");
                                setCustomerPhone("");
                                setCustomerNotes("");
                                setCreatedAppointment(null);
                            }}
                            className="text-xs font-semibold text-emerald-600 hover:text-emerald-700"
                        >
                            Yeni Bir Randevu Al
                        </button>
                    </div>
                )}
            </div>
        </div>
    );
}