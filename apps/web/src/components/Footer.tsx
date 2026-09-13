import Link from "next/link";

export function Footer() {
    return (
        <footer className="border-t border-slate-200/80 bg-white py-8 text-xs text-slate-500">
            <div className="mx-auto flex max-w-5xl flex-col items-center justify-between gap-4 px-4 sm:flex-row sm:px-6">
                <p>© {new Date().getFullYear()} RandevoApp. Tüm hakları saklıdır.</p>
                <div className="flex items-center gap-6 font-semibold">
                    <Link href="/kvkk" className="hover:text-emerald-600 transition">
                        KVKK Aydınlatma Metni
                    </Link>
                    <Link href="/gizlilik" className="hover:text-emerald-600 transition">
                        Gizlilik Politikası
                    </Link>
                    <a href="mailto:destek@randevoapp.net" className="hover:text-emerald-600 transition">
                        İletişim
                    </a>
                </div>
            </div>
        </footer>
    );
}