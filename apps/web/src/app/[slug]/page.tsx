import { notFound } from "next/navigation";
import { fetchTenantBySlug } from "@/lib/api";
import { BookingWidget } from "@/components/BookingWidget";

interface PageProps {
    params: Promise<{ slug: string }>;
}

export default async function TenantBookingPage({ params }: PageProps) {
    const { slug } = await params;

    let tenant;

    try {
        tenant = await fetchTenantBySlug(slug);
    } catch {
        notFound();
    }

    if (!tenant) {
        notFound();
    }

    return (
        <main className="min-h-screen bg-[#f7f8f6] px-4 py-6 sm:px-6 sm:py-10">
            <div className="mx-auto w-full max-w-5xl">
                <BookingWidget tenant={tenant} />
            </div>
        </main>
    );
}