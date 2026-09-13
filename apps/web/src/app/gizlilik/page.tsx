import Link from "next/link";
import { ArrowLeft, Lock } from "lucide-react";

export const metadata = {
    title: "Gizlilik Politikası | RandevoApp",
    description: "RandevoApp Gizlilik ve Veri Güvenliği Esasları",
};

export default function PrivacyPage() {
    return (
        <main className="min-h-screen bg-[#f8fafc] px-4 py-12 sm:px-6 lg:px-8">
            <div className="mx-auto max-w-3xl rounded-[32px] border border-slate-200/80 bg-white p-6 sm:p-12 shadow-sm">
                <Link
                    href="/"
                    className="inline-flex items-center gap-2 text-xs font-bold text-slate-400 transition hover:text-slate-800 mb-8"
                >
                    <ArrowLeft className="h-4 w-4" /> Ana Sayfaya Dön
                </Link>

                <div className="flex items-center gap-3 border-b border-slate-100 pb-6 mb-8">
                    <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-slate-900 text-emerald-400">
                        <Lock className="h-6 w-6" />
                    </div>
                    <div>
                        <h1 className="text-2xl font-black text-slate-900">Gizlilik Politikası</h1>
                        <p className="text-xs text-slate-400 mt-0.5">Son Güncelleme: Eylül 2026</p>
                    </div>
                </div>

                <div className="space-y-6 text-sm leading-relaxed text-slate-600">
                    <p>
                        RandevoApp olarak kullanıcılarımızın ve iş ortaklarımızın veri güvenliğine en üst düzeyde önem veriyoruz. Bu politika, platformumuz kullanılırken verilerin nasıl korunduğunu açıklamaktadır.
                    </p>
                    <h2 className="text-base font-bold text-slate-900">Veri Güvenliği Önlemleri</h2>
                    <p>
                        Tüm iletişim SSL/TLS protokolleri ile şifrelenmektedir. Veritabanı katmanında çok kiracılı (multi-tenant) izolasyon mimarisi uygulanmakta olup, bir işletmenin verilerine diğer bir işletmenin erişmesi engellenmiştir.
                    </p>
                    <h2 className="text-base font-bold text-slate-900">Üçüncü Taraf Entegrasyonlar</h2>
                    <p>
                        WhatsApp bildirimleri için doğrudan Meta Business Cloud API entegrasyonu kullanılır. Veriler reklam veya pazarlama amacıyla üçüncü şahıslara kesinlikle satılmaz veya kiralanmaz.
                    </p>
                </div>
            </div>
        </main>
    );
}