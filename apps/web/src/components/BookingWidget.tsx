"use client";

import { useEffect, useMemo, useState } from "react";
import Link from "next/link";
import {
    Tenant, ServiceItem, StaffItem, TimeSlot, fetchServices, fetchStaff,
    fetchAvailableSlots, createAppointment, AppointmentResult,
} from "@/lib/api";
import {
    ArrowLeft, ArrowRight, CalendarDays, Check, CheckCircle2, ChevronRight,
    Clock3, Home, MessageCircle, Scissors, UserRound,
} from "lucide-react";

interface Props { tenant: Tenant; }

const steps = [
    { number: 1, label: "Hizmet" },
    { number: 2, label: "Uzman" },
    { number: 3, label: "Tarih & Saat" },
    { number: 4, label: "Bilgiler" },
];

function formatDate(date: string) {
    if (!date) return "";
    return new Intl.DateTimeFormat("tr-TR", {
        weekday: "long", day: "numeric", month: "long",
    }).format(new Date(`${date}T12:00:00`));
}

function formatTime(utc: string) {
    return new Date(utc).toLocaleTimeString("tr-TR", {
        hour: "2-digit", minute: "2-digit", timeZone: "UTC",
    });
}

export function BookingWidget({ tenant }: Props) {
    const [step, setStep] = useState(1);
    const [services, setServices] = useState<ServiceItem[]>([]);
    const [staffList, setStaffList] = useState<StaffItem[]>([]);
    const [slots, setSlots] = useState<TimeSlot[]>([]);
    const [selectedService, setSelectedService] = useState<ServiceItem | null>(null);
    const [selectedStaff, setSelectedStaff] = useState<StaffItem | null>(null);
    const [selectedDate, setSelectedDate] = useState(new Date().toISOString().split("T")[0]);
    const [selectedSlot, setSelectedSlot] = useState<TimeSlot | null>(null);
    const [customerName, setCustomerName] = useState("");
    const [customerPhone, setCustomerPhone] = useState("");
    const [customerNotes, setCustomerNotes] = useState("");
    const [wantsWhatsApp, setWantsWhatsApp] = useState(true);
    const [loading, setLoading] = useState(false);
    const [submitting, setSubmitting] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [confirmedAppointment, setConfirmedAppointment] = useState<AppointmentResult | null>(null);

    useEffect(() => {
        async function initData() {
            try {
                setLoading(true); setError(null);
                const [srv, stf] = await Promise.all([fetchServices(tenant.id), fetchStaff(tenant.id)]);
                setServices(srv); setStaffList(stf);
            } catch (err: any) {
                setError(err.message || "Bilgiler yüklenemedi.");
            } finally { setLoading(false); }
        }
        initData();
    }, [tenant.id]);

    useEffect(() => {
        if (!selectedService || !selectedStaff || !selectedDate) return;
        async function loadSlots() {
            try {
                setLoading(true); setError(null);
                const available = await fetchAvailableSlots(
                    tenant.id, selectedService!.id, selectedStaff!.id, selectedDate
                );
                setSlots(available); setSelectedSlot(null);
            } catch { setError("Müsait saatler alınamadı."); setSlots([]); }
            finally { setLoading(false); }
        }
        loadSlots();
    }, [selectedService, selectedStaff, selectedDate, tenant.id]);

    async function handleBooking(e: React.FormEvent) {
        e.preventDefault();
        if (!selectedService || !selectedStaff || !selectedSlot) return;
        try {
            setSubmitting(true); setError(null);
            const result = await createAppointment(tenant.id, {
                serviceId: selectedService.id,
                staffId: selectedStaff.id,
                startTimeUtc: selectedSlot.startTimeUtc,
                customerFullName: customerName,
                customerPhoneNumber: customerPhone,
                customerNotes: customerNotes || undefined,
                customerWantsWhatsAppNotification: wantsWhatsApp,
            });
            setConfirmedAppointment(result); setStep(5);
        } catch (err: any) { setError(err.message || "Randevu alınamadı."); }
        finally { setSubmitting(false); }
    }

    const progress = step === 5 ? 100 : (step / 4) * 100;
    const summary = useMemo(() => ({
        service: selectedService?.name,
        staff: selectedStaff?.fullName,
        date: selectedDate ? formatDate(selectedDate) : "",
        time: selectedSlot ? formatTime(selectedSlot.startTimeUtc) : "",
        price: selectedService?.price,
    }), [selectedService, selectedStaff, selectedDate, selectedSlot]);

    const resetBooking = () => {
        setStep(1); setSelectedService(null); setSelectedStaff(null); setSelectedSlot(null);
        setConfirmedAppointment(null); setCustomerName(""); setCustomerPhone("");
        setCustomerNotes(""); setError(null);
    };

    return (
        <section className="overflow-hidden rounded-[32px] border border-slate-200/80 bg-white shadow-[0_24px_80px_-30px_rgba(15,23,42,0.28)]">
            <header className="relative overflow-hidden border-b border-slate-200/80 px-5 py-6 sm:px-8">
                <div className="absolute inset-0 bg-[radial-gradient(circle_at_90%_0%,rgba(16,185,129,0.12),transparent_35%)]" />
                <div className="relative flex items-start justify-between gap-4">
                    <div className="flex min-w-0 items-center gap-3">
                        <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-2xl bg-slate-950 text-emerald-400 shadow-lg shadow-slate-900/10"><Scissors className="h-5 w-5" /></div>
                        <div className="min-w-0"><p className="text-[11px] font-bold uppercase tracking-[0.18em] text-emerald-600">Randevu</p><h1 className="truncate text-xl font-black tracking-tight text-slate-950 sm:text-2xl">{tenant.name}</h1></div>
                    </div>
                    {step < 5 && <div className="hidden rounded-full border border-slate-200 bg-slate-50 px-3 py-2 text-xs font-bold text-slate-500 sm:block">{step} / 4</div>}
                </div>
                {step < 5 && <div className="relative mt-6"><div className="h-1.5 overflow-hidden rounded-full bg-slate-100"><div className="h-full rounded-full bg-emerald-500 transition-all duration-500" style={{ width: `${progress}%` }} /></div><div className="mt-3 grid grid-cols-4 gap-2">{steps.map(item => <div key={item.number} className="flex items-center gap-2"><span className={`flex h-6 w-6 items-center justify-center rounded-full text-[10px] font-black ${step >= item.number ? "bg-slate-950 text-white" : "bg-slate-100 text-slate-400"}`}>{step > item.number ? <Check className="h-3 w-3" /> : item.number}</span><span className={`hidden text-[11px] font-semibold sm:block ${step >= item.number ? "text-slate-700" : "text-slate-400"}`}>{item.label}</span></div>)}</div></div>}
            </header>

            <div className="grid lg:grid-cols-[1fr_280px]">
                <div className="p-5 sm:p-8">
                    {error && <div className="mb-6 flex items-start gap-3 rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700"><span className="mt-0.5">!</span><span>{error}</span></div>}

                    {step === 1 && <div className="space-y-6"><SectionTitle eyebrow="01 — Hizmet" title="Ne yaptırmak istersin?" description="Sana uygun hizmeti seç, sonraki adıma geçelim." />{loading && services.length === 0 ? <SkeletonList /> : services.length === 0 ? <EmptyState text="Bu işletmede henüz hizmet tanımlanmamış." /> : <div className="space-y-3">{services.map(srv => <button key={srv.id} type="button" onClick={() => { setSelectedService(srv); setStep(2); }} className="group flex w-full items-center justify-between rounded-2xl border border-slate-200 bg-white p-4 text-left transition-all hover:-translate-y-0.5 hover:border-emerald-300 hover:shadow-lg hover:shadow-slate-900/5"><div className="flex items-center gap-4"><div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-2xl bg-slate-100 text-slate-700 transition-colors group-hover:bg-emerald-50 group-hover:text-emerald-600"><Scissors className="h-5 w-5" /></div><div><h3 className="font-bold text-slate-900">{srv.name}</h3><p className="mt-1 text-xs text-slate-400">{srv.durationInMinutes} dk{srv.description ? ` • ${srv.description}` : ""}</p></div></div><div className="flex items-center gap-3"><span className="font-black text-slate-950">{srv.price} ₺</span><ChevronRight className="h-4 w-4 text-slate-300 transition-transform group-hover:translate-x-1 group-hover:text-emerald-500" /></div></button>)}</div>}</div>}

                    {step === 2 && <div className="space-y-6"><BackButton onClick={() => setStep(1)} label="Hizmet seçimine dön" /><SectionTitle eyebrow="02 — Uzman" title="Kiminle çalışmak istersin?" description="Bir uzman seç ve uygun zamanları görüntüle." />{staffList.length === 0 ? <EmptyState text="Bu işletmede henüz personel tanımlanmamış." /> : <div className="grid gap-3 sm:grid-cols-2">{staffList.map(st => <button key={st.id} type="button" onClick={() => { setSelectedStaff(st); setStep(3); }} className="group flex items-center gap-4 rounded-2xl border border-slate-200 p-4 text-left transition-all hover:-translate-y-0.5 hover:border-emerald-300 hover:shadow-lg hover:shadow-slate-900/5"><div className="flex h-14 w-14 shrink-0 items-center justify-center rounded-2xl bg-slate-950 text-lg font-black text-emerald-400">{st.fullName?.[0]?.toUpperCase() ?? "U"}</div><div className="min-w-0 flex-1"><h3 className="truncate font-bold text-slate-900">{st.fullName}</h3><p className="mt-1 text-xs text-slate-400">{st.title || "Uzman"}</p></div><ChevronRight className="h-4 w-4 text-slate-300 group-hover:text-emerald-500" /></button>)}</div>}</div>}

                    {step === 3 && <div className="space-y-6"><BackButton onClick={() => setStep(2)} label="Uzman seçimine dön" /><SectionTitle eyebrow="03 — Tarih & Saat" title="Sana uygun zamanı seç" /><div className="rounded-2xl border border-slate-200 bg-slate-50/70 p-4"><label className="mb-2 flex items-center gap-2 text-xs font-bold text-slate-600"><CalendarDays className="h-4 w-4 text-emerald-600" />Randevu tarihi</label><input type="date" value={selectedDate} min={new Date().toISOString().split("T")[0]} onChange={e => setSelectedDate(e.target.value)} className="w-full rounded-xl border border-slate-200 bg-white px-4 py-3 text-sm font-semibold text-slate-800 outline-none transition focus:border-emerald-400 focus:ring-4 focus:ring-emerald-500/10" /><p className="mt-2 text-xs text-slate-400">{formatDate(selectedDate)}</p></div><div><div className="mb-3 flex items-center justify-between"><label className="text-xs font-bold uppercase tracking-wider text-slate-500">Müsait saatler</label>{loading && <span className="text-xs font-medium text-emerald-600">Kontrol ediliyor...</span>}</div>{loading ? <div className="grid grid-cols-3 gap-2 sm:grid-cols-4">{Array.from({ length: 12 }).map((_, i) => <div key={i} className="h-11 animate-pulse rounded-xl bg-slate-100" />)}</div> : slots.length === 0 ? <EmptyState text="Bu tarihte uygun saat bulunmuyor. Başka bir tarih deneyebilirsin." /> : <div className="grid max-h-64 grid-cols-3 gap-2 overflow-y-auto pr-1 sm:grid-cols-4">{slots.map((slot, idx) => { const selected = selectedSlot === slot; return <button key={idx} type="button" disabled={!slot.isAvailable} onClick={() => setSelectedSlot(slot)} className={`rounded-xl border px-3 py-3 text-sm font-bold transition-all ${!slot.isAvailable ? "cursor-not-allowed border-slate-100 bg-slate-50 text-slate-300 line-through" : selected ? "border-slate-950 bg-slate-950 text-white shadow-lg shadow-slate-950/10" : "border-slate-200 bg-white text-slate-700 hover:border-emerald-300 hover:bg-emerald-50/50"}`}>{formatTime(slot.startTimeUtc)}</button> })}</div>}</div><button type="button" disabled={!selectedSlot} onClick={() => setStep(4)} className="flex w-full items-center justify-center gap-2 rounded-2xl bg-slate-950 py-4 text-sm font-bold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-30">Devam Et <ArrowRight className="h-4 w-4" /></button></div>}

                    {step === 4 && <div className="space-y-6"><BackButton onClick={() => setStep(3)} label="Saat seçimine dön" /><SectionTitle eyebrow="04 — Bilgiler" title="Son bir adım" description="Randevunu oluşturmak için iletişim bilgilerini bırak." /><div className="rounded-2xl border border-slate-200 bg-slate-50 p-4"><div className="mb-3 flex items-center gap-2 text-xs font-bold text-slate-500"><Clock3 className="h-4 w-4 text-emerald-600" />Randevu özeti</div><div className="grid gap-3 sm:grid-cols-2"><SummaryItem label="Hizmet" value={summary.service || "-"} /><SummaryItem label="Uzman" value={summary.staff || "-"} /><SummaryItem label="Tarih" value={summary.date || "-"} /><SummaryItem label="Saat" value={summary.time || "-"} /></div><div className="mt-4 flex items-center justify-between border-t border-slate-200 pt-4"><span className="text-sm font-semibold text-slate-500">Toplam</span><span className="text-xl font-black text-slate-950">{summary.price} ₺</span></div></div><form onSubmit={handleBooking} className="space-y-4"><Input label="Ad Soyad" placeholder="Adınız ve soyadınız" value={customerName} onChange={setCustomerName} icon={<UserRound className="h-4 w-4" />} required /><Input label="Telefon" type="tel" placeholder="905551112233" value={customerPhone} onChange={setCustomerPhone} icon={<span className="text-xs font-black">+90</span>} required /><div><label className="mb-2 block text-xs font-bold text-slate-600">Not <span className="font-medium text-slate-400">(opsiyonel)</span></label><textarea rows={3} placeholder="Varsa işletmeye iletmek istediğin bir not..." value={customerNotes} onChange={e => setCustomerNotes(e.target.value)} className="w-full resize-none rounded-xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm outline-none transition placeholder:text-slate-400 focus:border-emerald-400 focus:bg-white focus:ring-4 focus:ring-emerald-500/10" /></div><label className="flex cursor-pointer items-start gap-3 rounded-2xl border border-emerald-100 bg-emerald-50/60 p-4"><input type="checkbox" checked={wantsWhatsApp} onChange={e => setWantsWhatsApp(e.target.checked)} className="mt-0.5 h-4 w-4 rounded border-slate-300 text-emerald-600 focus:ring-emerald-500" /><span><span className="flex items-center gap-1.5 text-sm font-bold text-slate-800"><MessageCircle className="h-4 w-4 text-emerald-600" />WhatsApp bildirimleri</span><span className="mt-1 block text-xs leading-relaxed text-slate-500">Randevu durumu ve hatırlatmalar WhatsApp üzerinden iletilsin.</span></span></label><button type="submit" disabled={submitting} className="flex w-full items-center justify-center gap-2 rounded-2xl bg-emerald-500 py-4 text-sm font-black text-white shadow-lg shadow-emerald-500/20 transition hover:bg-emerald-600 disabled:cursor-not-allowed disabled:opacity-50">{submitting ? "Randevun oluşturuluyor..." : "Randevuyu Oluştur"}{!submitting && <ArrowRight className="h-4 w-4" />}</button></form></div>}

                    {step === 5 && confirmedAppointment && <div className="py-8 text-center sm:py-12"><div className="mx-auto flex h-20 w-20 items-center justify-center rounded-[28px] bg-emerald-50 text-emerald-500"><CheckCircle2 className="h-10 w-10" /></div><p className="mt-7 text-xs font-bold uppercase tracking-[0.18em] text-emerald-600">Randevu oluşturuldu</p><h2 className="mt-2 text-3xl font-black tracking-tight text-slate-950">Talebin işletmeye iletildi.</h2><p className="mx-auto mt-3 max-w-md text-sm leading-6 text-slate-500">İşletme randevunu onayladığında {wantsWhatsApp ? "WhatsApp üzerinden bilgilendirileceksin." : "randevu durumunu sistem üzerinden takip edebilirsin."}</p><div className="mx-auto mt-8 max-w-md rounded-3xl border border-slate-200 bg-slate-50 p-5 text-left"><div className="mb-4 flex items-center justify-between"><span className="text-xs font-bold uppercase tracking-wider text-slate-400">Randevu özeti</span><span className="rounded-full bg-amber-50 px-2.5 py-1 text-[10px] font-black text-amber-700">ONAY BEKLİYOR</span></div><div className="space-y-3 text-sm"><SummaryItem label="İşletme" value={tenant.name} /><SummaryItem label="Hizmet" value={confirmedAppointment.serviceName} /><SummaryItem label="Personel" value={confirmedAppointment.staffName} /><SummaryItem label="Tutar" value={`${confirmedAppointment.price} ₺`} /></div></div><div className="mt-7 flex flex-col justify-center gap-3 sm:flex-row"><button type="button" onClick={resetBooking} className="rounded-xl border border-slate-200 px-5 py-3 text-sm font-bold text-slate-700 transition hover:bg-slate-50">Yeni Randevu</button><Link href="/" className="inline-flex items-center justify-center gap-2 rounded-xl bg-slate-950 px-5 py-3 text-sm font-bold text-white transition hover:bg-slate-800"><Home className="h-4 w-4" />Ana Sayfa</Link></div></div>}
                </div>

                <aside className="hidden border-l border-slate-200 bg-slate-50/70 p-6 lg:block"><div className="sticky top-6"><p className="text-[10px] font-black uppercase tracking-[0.2em] text-slate-400">Randevun</p><div className="mt-4 rounded-3xl border border-slate-200 bg-white p-5 shadow-sm"><div className="flex h-11 w-11 items-center justify-center rounded-2xl bg-emerald-50 text-emerald-600"><CalendarDays className="h-5 w-5" /></div><div className="mt-5 space-y-4"><MiniSummary label="Hizmet" value={summary.service || "Henüz seçilmedi"} /><MiniSummary label="Uzman" value={summary.staff || "Henüz seçilmedi"} /><MiniSummary label="Tarih" value={summary.date || "Henüz seçilmedi"} /><MiniSummary label="Saat" value={summary.time || "Henüz seçilmedi"} /></div>{summary.price !== undefined && <div className="mt-5 flex items-center justify-between border-t border-slate-100 pt-4"><span className="text-xs font-semibold text-slate-500">Tutar</span><span className="font-black text-slate-950">{summary.price} ₺</span></div>}</div><div className="mt-5 rounded-3xl bg-slate-950 p-5 text-white"><div className="flex h-9 w-9 items-center justify-center rounded-xl bg-white/10"><MessageCircle className="h-4 w-4 text-emerald-400" /></div><h3 className="mt-4 text-sm font-bold">WhatsApp ile bilgilendirme</h3><p className="mt-2 text-xs leading-5 text-slate-400">Randevu durumundaki değişiklikler ve hatırlatmalar seçimin doğrultusunda WhatsApp üzerinden iletilebilir.</p></div></div></aside>
            </div>
        </section>
    );
}

function SectionTitle({ eyebrow, title, description }: { eyebrow: string; title: string; description?: string }) {
    return <div><p className="text-xs font-bold uppercase tracking-[0.16em] text-emerald-600">{eyebrow}</p><h2 className="mt-1 text-2xl font-black tracking-tight text-slate-950">{title}</h2>{description && <p className="mt-2 text-sm text-slate-500">{description}</p>}</div>;
}

function BackButton({ onClick, label }: { onClick: () => void; label: string }) {
    return <button type="button" onClick={onClick} className="inline-flex items-center gap-2 text-xs font-bold text-slate-400 transition hover:text-slate-800"><ArrowLeft className="h-4 w-4" />{label}</button>;
}

function SummaryItem({ label, value }: { label: string; value: string }) {
    return <div className="min-w-0"><p className="text-[10px] font-bold uppercase tracking-wider text-slate-400">{label}</p><p className="mt-1 truncate text-sm font-bold text-slate-800">{value}</p></div>;
}

function MiniSummary({ label, value }: { label: string; value: string }) {
    return <div><p className="text-[10px] font-bold uppercase tracking-wider text-slate-400">{label}</p><p className="mt-1 truncate text-xs font-bold text-slate-700">{value}</p></div>;
}

function Input({ label, placeholder, value, onChange, icon, type = "text", required = false }: { label: string; placeholder: string; value: string; onChange: (value: string) => void; icon: React.ReactNode; type?: string; required?: boolean }) {
    return <div><label className="mb-2 block text-xs font-bold text-slate-600">{label}</label><div className="relative"><div className="pointer-events-none absolute left-4 top-1/2 flex -translate-y-1/2 items-center text-slate-400">{icon}</div><input required={required} type={type} placeholder={placeholder} value={value} onChange={e => onChange(e.target.value)} className="w-full rounded-xl border border-slate-200 bg-slate-50 py-3.5 pl-11 pr-4 text-sm outline-none transition placeholder:text-slate-400 focus:border-emerald-400 focus:bg-white focus:ring-4 focus:ring-emerald-500/10" /></div></div>;
}

function EmptyState({ text }: { text: string }) {
    return <div className="rounded-2xl border border-dashed border-slate-200 bg-slate-50 px-5 py-12 text-center"><CalendarDays className="mx-auto h-6 w-6 text-slate-300" /><p className="mt-3 text-sm font-medium text-slate-500">{text}</p></div>;
}

function SkeletonList() {
    return <div className="space-y-3">{[1, 2, 3].map(item => <div key={item} className="h-20 animate-pulse rounded-2xl bg-slate-100" />)}</div>;
}
