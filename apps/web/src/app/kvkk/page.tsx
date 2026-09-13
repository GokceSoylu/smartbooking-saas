import Link from "next/link";
import { ArrowLeft, ShieldCheck } from "lucide-react";

export const metadata = {
    title: "KVKK Aydınlatma Metni | RandevoApp",
    description: "Kişisel Verilerin Korunması Kanunu Kapsamında Aydınlatma Metni",
};

export default function KvkkPage() {
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
                    <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-emerald-50 text-emerald-600">
                        <ShieldCheck className="h-6 w-6" />
                    </div>
                    <div>
                        <h1 className="text-2xl font-black text-slate-900">KVKK Aydınlatma Metni</h1>
                        <p className="text-xs text-slate-400 mt-0.5">Yürürlük Tarihi: Eylül 2026 | Sürüm: v1.0</p>
                    </div>
                </div>

                <div className="space-y-6 text-sm leading-relaxed text-slate-600">
                    <section>
                        <h2 className="text-base font-bold text-slate-900 mb-2">1. Veri Sorumlusu ve Veri İşleyen Sıfatı</h2>
                        <p>
                            6698 sayılı Kişisel Verilerin Korunması Kanunu (&quot;KVKK&quot;) uyarınca; platformumuz üzerinden randevu aldığınız ilgili işletme (salon, klinik vb.) <strong>&quot;Veri Sorumlusu&quot;</strong>, randevu yazılım altyapısını ve teknik servis sağlayıcılığını üstlenen RandevoApp ise <strong>&quot;Veri İşleyen&quot;</strong> sıfatına sahiptir.
                        </p>
                    </section>

                    <section>
                        <h2 className="text-base font-bold text-slate-900 mb-2">2. İşlenen Kişisel Verileriniz ve Toplama Yöntemi</h2>
                        <p>Randevu sürecinizin yürütülmesi amacıyla aşağıdaki kişisel verileriniz dijital form aracılığıyla otomatik yollarla toplanmaktadır:</p>
                        <ul className="list-disc list-inside mt-2 space-y-1 pl-2">
                            <li><strong>Kimlik Bilgisi:</strong> Ad, soyad.</li>
                            <li><strong>İletişim Bilgisi:</strong> Telefon numarası.</li>
                            <li><strong>İşlem Güvenliği Bilgisi:</strong> Randevu saati, onay/red kayıtları, onay anındaki IP adresi ve zaman damgası.</li>
                            <li><strong>Müşteri İşlem Notu:</strong> Randevu formunda kendi isteğinizle belirttiğiniz opsiyonel notlar.</li>
                        </ul>
                    </section>

                    <section>
                        <h2 className="text-base font-bold text-slate-900 mb-2">3. Veri İşlemenin Hukuki Sebepleri ve Amaçları</h2>
                        <p>
                            Kişisel verileriniz, KVKK Madde 5/2-c bendi uyarınca <em>&quot;Bir sözleşmenin kurulması veya ifasıyla doğrudan doğruya ilgili olması&quot;</em> ve 5/2-f bendi uyarınca <em>&quot;Veri sorumlusunun meşru menfaatleri&quot;</em> hukuki sebeplerine dayalı olarak; randevu rezervasyonunun gerçekleştirilmesi, takvim çakışmalarının önlenmesi ve hizmetin ifası amaçlarıyla işlenmektedir.
                        </p>
                    </section>

                    <section>
                        <h2 className="text-base font-bold text-slate-900 mb-2">4. Yurt Dışına Veri Aktarımı (WhatsApp Bildirimleri)</h2>
                        <p>
                            WhatsApp üzerinden randevu durumu ve hatırlatma bildirimi almayı tercih etmeniz halinde; kimlik, iletişim ve randevu saati verileriniz, sunucuları yurt dışında bulunan Meta Platforms Inc. (WhatsApp Business Cloud API) altyapısına <strong>KVKK Madde 9</strong> uyarınca verdiğiniz <strong>Açık Rıza</strong> doğrultusunda aktarılmaktadır. Bu izni vermediğiniz takdirde randevunuz oluşturulur ancak bildirim gönderilmez.
                        </p>
                    </section>

                    <section>
                        <h2 className="text-base font-bold text-slate-900 mb-2">5. Veri Saklama Süresi ve Güvenlik</h2>
                        <p>
                            Randevu kayıtları, hizmet tamamlandıktan sonra olası hukuki uyuşmazlıklarda zamanaşımı süreleri gözetilerek yasal mevzuata uygun süre boyunca güvenli veritabanlarımızda şifrelenmiş olarak muhafaza edilmektedir.
                        </p>
                    </section>

                    <section>
                        <h2 className="text-base font-bold text-slate-900 mb-2">6. İlgili Kişi Olarak Haklarınız (Madde 11)</h2>
                        <p>
                            KVKK Madde 11 kapsamında; verilerinizin işlenip işlenmediğini öğrenme, silinmesini veya anonimleştirilmesini talep etme hakkınız bulunmaktadır. Taleplerinizi doğrudan hizmet aldığınız işletmeye veya yazılı olarak altyapı sağlayıcımıza (<strong>destek@randevoapp.net</strong>) iletebilirsiniz.
                        </p>
                    </section>
                </div>
            </div>
        </main>
    );
}