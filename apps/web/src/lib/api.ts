const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5099/api";

export interface Tenant {
    id: string;
    name: string;
    slug: string;
    phoneNumber: string;
    isActive: boolean;
}

export interface ServiceItem {
    id: string;
    name: string;
    description: string;
    durationInMinutes: number;
    price: number;
    isActive: boolean;
}

export interface StaffItem {
    id: string;
    fullName: string;
    title: string;
    phoneNumber: string;
    isActive: boolean;
}

export interface TimeSlot {
    startTimeUtc: string;
    endTimeUtc: string;
    isAvailable: boolean;
}

export interface CreateAppointmentPayload {
    serviceId: string;
    staffId: string;
    startTimeUtc: string;
    customerFullName: string;
    customerPhoneNumber: string;
    customerNotes?: string;
}

export interface AppointmentResult {
    id: string;
    tenantId: string;
    serviceName: string;
    staffName: string;
    customerFullName: string;
    customerPhoneNumber: string;
    startTimeUtc: string;
    endTimeUtc: string;
    price: number;
    status: number;
}

// 1. Tenant, Servis ve Personel Listeleme
export async function fetchTenantBySlug(slug: string): Promise<Tenant> {
    const res = await fetch(`${API_BASE_URL}/tenants/by-slug/${slug}`, { cache: "no-store" });
    if (!res.ok) throw new Error("İşletme bulunamadı");
    return res.json();
}

export async function fetchServices(tenantId: string): Promise<ServiceItem[]> {
    const res = await fetch(`${API_BASE_URL}/services`, {
        headers: { "X-Tenant-Id": tenantId },
        cache: "no-store",
    });
    if (!res.ok) throw new Error("Hizmetler alınamadı");
    return res.json();
}

export async function fetchStaff(tenantId: string): Promise<StaffItem[]> {
    const res = await fetch(`${API_BASE_URL}/staff`, {
        headers: { "X-Tenant-Id": tenantId },
        cache: "no-store",
    });
    if (!res.ok) throw new Error("Personel listesi alınamadı");
    return res.json();
}

// 2. Slot Sorgulama ve Randevu Oluşturma
export async function fetchAvailableSlots(
    tenantId: string,
    serviceId: string,
    staffId: string,
    date: string
): Promise<TimeSlot[]> {
    const res = await fetch(`${API_BASE_URL}/appointments/available-slots`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
            "X-Tenant-Id": tenantId,
        },
        body: JSON.stringify({ serviceId, staffId, date }),
    });
    if (!res.ok) throw new Error("Müsait saatler alınamadı");
    return res.json();
}

export async function createAppointment(
    tenantId: string,
    payload: CreateAppointmentPayload
): Promise<AppointmentResult> {
    const res = await fetch(`${API_BASE_URL}/appointments`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
            "X-Tenant-Id": tenantId,
        },
        body: JSON.stringify(payload),
    });
    if (!res.ok) {
        const err = await res.json().catch(() => ({ message: "Randevu oluşturulamadı" }));
        throw new Error(err.message || "Randevu oluşturulamadı");
    }
    return res.json();
}

// 3. Dashboard Randevu Listeleme ve Durum Güncelleme
export async function fetchTenantAppointments(tenantId: string): Promise<AppointmentResult[]> {
    const res = await fetch(`${API_BASE_URL}/appointments`, {
        headers: { "X-Tenant-Id": tenantId },
        cache: "no-store",
    });
    if (!res.ok) throw new Error("Randevular yüklenemedi");
    return res.json();
}

export async function updateAppointmentStatus(
    tenantId: string,
    appointmentId: string,
    status: number
): Promise<AppointmentResult> {
    const res = await fetch(`${API_BASE_URL}/appointments/${appointmentId}/status`, {
        method: "PATCH",
        headers: {
            "Content-Type": "application/json",
            "X-Tenant-Id": tenantId,
        },
        body: JSON.stringify({ status }),
    });
    if (!res.ok) throw new Error("Durum güncellenemedi");
    return res.json();
}

// 4. Hizmet Ekleme / Silme
export async function createService(
    tenantId: string,
    payload: { name: string; description: string; durationInMinutes: number; price: number }
): Promise<ServiceItem> {
    const res = await fetch(`${API_BASE_URL}/services`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
            "X-Tenant-Id": tenantId,
        },
        body: JSON.stringify(payload),
    });
    if (!res.ok) throw new Error("Hizmet eklenemedi");
    return res.json();
}

export async function deleteService(tenantId: string, serviceId: string): Promise<void> {
    const res = await fetch(`${API_BASE_URL}/services/${serviceId}`, {
        method: "DELETE",
        headers: { "X-Tenant-Id": tenantId },
    });
    if (!res.ok) throw new Error("Hizmet silinemedi");
}

// 5. Personel Ekleme / Silme
export async function createStaff(
    tenantId: string,
    payload: { fullName: string; title: string; phoneNumber: string }
): Promise<StaffItem> {
    const res = await fetch(`${API_BASE_URL}/staff`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
            "X-Tenant-Id": tenantId,
        },
        body: JSON.stringify(payload),
    });
    if (!res.ok) throw new Error("Personel eklenemedi");
    return res.json();
}

export async function deleteStaff(tenantId: string, staffId: string): Promise<void> {
    const res = await fetch(`${API_BASE_URL}/staff/${staffId}`, {
        method: "DELETE",
        headers: { "X-Tenant-Id": tenantId },
    });
    if (!res.ok) throw new Error("Personel silinemedi");
}