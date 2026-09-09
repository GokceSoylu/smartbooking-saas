// URL'in sonunda /api olup olmadığını garantiye alan ve canlı fallback içeren güvenli URL tanımı:
const RAW_URL =
    process.env.NEXT_PUBLIC_API_URL ||
    process.env.API_URL ||
    "https://randevo-api-v1yu.onrender.com";

const CLEAN_URL = RAW_URL.replace(/\/+$/, "");
const API_BASE_URL = CLEAN_URL.endsWith("/api") ? CLEAN_URL : `${CLEAN_URL}/api`;

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
    customerWantsWhatsAppNotification?: boolean;
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

export interface WorkingHourItem {
    id?: string;
    dayOfWeek: number; // 0: Pazar, 1: Pazartesi, ...
    openingTime: string;
    closingTime: string;
    isClosed: boolean;
}

export interface LoginResponse {
    token: string;
    fullName: string;
    email: string;
    tenantId: string;
    role?: string;
}

export interface AdminTenantItem {
    id: string;
    name: string;
    slug: string;
    phoneNumber: string;
    isApproved: boolean;
    isActive: boolean;
    subscriptionExpiresAtUtc: string | null;
    createdAtUtc: string;
}

// 1. Tenant, Servis ve Personel Listeleme
export async function fetchTenantBySlug(slug: string): Promise<Tenant> {
    const url = `${API_BASE_URL}/tenants/by-slug/${slug}`;
    try {
        const res = await fetch(url, { cache: "no-store" });
        if (!res.ok) {
            const errBody = await res.text().catch(() => "");
            console.error(`fetchTenantBySlug Hatası [${res.status}]: ${errBody} (URL: ${url})`);
            throw new Error(`İşletme bulunamadı: ${res.status}`);
        }
        return res.json();
    } catch (err) {
        console.error("fetchTenantBySlug Network / Fetch Hatası:", err);
        throw err;
    }
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

// 6. Çalışma Saatleri
export async function fetchWorkingHours(tenantId: string): Promise<WorkingHourItem[]> {
    const res = await fetch(`${API_BASE_URL}/workinghours`, {
        headers: { "X-Tenant-Id": tenantId },
        cache: "no-store",
    });
    if (!res.ok) throw new Error("Çalışma saatleri alınamadı");
    return res.json();
}

export async function updateWorkingHours(
    tenantId: string,
    items: WorkingHourItem[]
): Promise<void> {
    const res = await fetch(`${API_BASE_URL}/workinghours`, {
        method: "PUT",
        headers: {
            "Content-Type": "application/json",
            "X-Tenant-Id": tenantId,
        },
        body: JSON.stringify(items),
    });
    if (!res.ok) throw new Error("Çalışma saatleri güncellenemedi");
}

// 7. Auth İşlemleri
export async function login(payload: { email: string; password: string }): Promise<LoginResponse> {
    const res = await fetch(`${API_BASE_URL}/auth/login`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload),
    });
    if (!res.ok) {
        const err = await res.json().catch(() => ({ message: "Giriş başarısız" }));
        throw new Error(err.message || "Giriş yapılamadı");
    }
    return res.json();
}

export async function registerTenant(payload: {
    businessName: string;
    slug: string;
    fullName: string;
    email: string;
    password: string;
    phoneNumber: string;
}): Promise<LoginResponse> {
    const res = await fetch(`${API_BASE_URL}/auth/register`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload),
    });
    if (!res.ok) {
        const err = await res.json().catch(() => ({ message: "Kayıt oluşturulamadı" }));
        throw new Error(err.message || "Kayıt işlemi başarısız");
    }
    return res.json();
}

export async function fetchAllTenants(): Promise<Tenant[]> {
    const res = await fetch(`${API_BASE_URL}/tenants/all`, { cache: "no-store" });
    if (!res.ok) throw new Error("İşletmeler alınamadı");
    return res.json();
}

export async function updateTenantNotificationSettings(
    tenantId: string,
    notifyOwner: boolean
): Promise<void> {
    const res = await fetch(`${API_BASE_URL}/tenants/notification-settings`, {
        method: "PATCH",
        headers: {
            "Content-Type": "application/json",
            "X-Tenant-Id": tenantId,
        },
        body: JSON.stringify({ notifyOwnerOnNewAppointment: notifyOwner }),
    });
    if (!res.ok) throw new Error("Ayar güncellenemedi");
}

// 8. Superadmin Tenant Yönetimi
export async function fetchAdminTenants(): Promise<AdminTenantItem[]> {
    const url = `${API_BASE_URL}/admin/tenants`;
    try {
        const res = await fetch(url, {
            cache: "no-store",
            headers: { "Content-Type": "application/json" },
        });

        if (!res.ok) {
            const errText = await res.text();
            console.error("API Hatası Detayı:", res.status, errText);
            throw new Error(`Hata (${res.status}): ${errText}`);
        }

        return res.json();
    } catch (error) {
        console.error("Bağlantı Hatası:", error);
        throw error;
    }
}

export async function approveTenant(id: string): Promise<{ message: string }> {
    const res = await fetch(`${API_BASE_URL}/admin/tenants/${id}/approve`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({}),
    });

    if (!res.ok) {
        const errorText = await res.text();
        throw new Error(errorText || "İşletme onaylanamadı.");
    }

    return res.json();
}

export async function toggleTenantStatus(id: string): Promise<{ message: string; isActive: boolean }> {
    const res = await fetch(`${API_BASE_URL}/admin/tenants/${id}/toggle-status`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({}),
    });

    if (!res.ok) {
        const errorText = await res.text();
        throw new Error(errorText || "İşletme durumu güncellenemedi.");
    }

    return res.json();
}

export async function extendTenantSubscription(id: string, days: number): Promise<{ message: string }> {
    const res = await fetch(`${API_BASE_URL}/admin/tenants/${id}/extend-subscription?days=${days}`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({}),
    });

    if (!res.ok) {
        const errorText = await res.text();
        throw new Error(errorText || "Abonelik uzatılamadı.");
    }

    return res.json();
}